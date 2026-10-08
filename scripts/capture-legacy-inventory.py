"""Capture reference metadata from the pinned legacy commit; never read appsettings/secrets."""
from pathlib import Path
import argparse
import hashlib
import json
import re
import subprocess

BASELINE = "66a9c742d0f6fb692d339076e6cd82780e3476be"
ROOT = Path(__file__).resolve().parents[1]


def git(source, *args):
    return subprocess.check_output(["git", "-C", str(source), *args]).decode("utf-8")


def strip_comments(text):
    return re.sub(r"/\*.*?\*/|//[^\n]*", "", text, flags=re.S)


def brace_body(text, opening):
    depth = 0
    for index in range(opening, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[opening + 1:index]
    raise ValueError("Unbalanced method/switch body")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, default=ROOT.parent / "25.ezInvoice")
    args = parser.parse_args()
    source = args.source.resolve()
    if git(source, "rev-parse", "HEAD").strip() != BASELINE:
        raise SystemExit("Legacy HEAD changed. Review baseline deliberately before recapturing.")
    reference = ROOT / "docs" / "reference"
    reference.mkdir(parents=True, exist_ok=True)
    metadata = {"baselineCommit": BASELINE, "documentationCommit": "f77fa2e", "files": []}

    def read(relative):
        raw = subprocess.check_output(["git", "-C", str(source), "show", f"{BASELINE}:{relative}"])
        metadata["files"].append({"path": relative, "sha256": hashlib.sha256(raw).hexdigest()})
        return raw.decode("utf-8-sig")

    enums = read("src/ezInvoice.Application/Services/ThirdPartyServices/EnumDefine.cs")
    actions_block = re.search(r"public enum INVOICE_ACTION\s*\{", enums)
    partners_block = re.search(r"public enum PARTNER\s*\{", enums)
    actions = [{"name": name, "value": int(value)} for name, value in re.findall(
        r"^\s*(\w+)\s*=\s*(-?\d+)", brace_body(enums, actions_block.end()-1), re.M)]
    partners = [{"name": name, "value": int(value)} for name, value in re.findall(
        r"^\s*(\w+)\s*=\s*(-?\d+)", brace_body(enums, partners_block.end()-1), re.M)]

    controller = strip_comments(read("src/ezInvoice.Services.Api/Controllers/ApiForPms/PmsController.cs"))
    endpoints = []
    for match in re.finditer(r'\[Http(Get|Post)\("([^"\n]+)"\)\]', controller):
        tail = controller[match.end():]
        signature = re.search(r"public\s+async\s+Task<IActionResult>\s+(\w+)\s*\((.*?)\)\s*\{", tail, re.S)
        if signature is None:
            raise ValueError(f"Cannot read endpoint signature: {match.group(2)}")
        parameters = re.sub(r"\[[^\]]*\]", "", signature.group(2)).strip()
        endpoints.append({"method": match.group(1).upper(), "path": match.group(2),
                          "legacyMethod": signature.group(1), "legacyParameters": parameters})
    if len(endpoints) != 33 or len(actions) != 32 or len(partners) != 9:
        raise ValueError("Unexpected endpoint/action/provider count; inspect source before proceeding")

    services = {"VNPT": "Vnpt", "MINVOICE": "Minvoice", "SUN": "Sun", "VIETTEL": "Viettel",
                "VNPT_V5": "VnptV5", "MISA": "Misa", "CYBER_BILL": "Cyberbill", "FAST": "Fast",
                "EASY_INVOICE": "EasyInvoice"}
    for partner in partners:
        relative = f"src/ezInvoice.Application/Services/ThirdPartyServices/{services[partner['name']]}AppService.cs"
        service = strip_comments(read(relative))
        switch = re.search(r"(?:switch\s*\(model\.Action\)|model\.Action\s+switch)\s*\{", service)
        if switch is None:
            # Minvoice dispatches request.Action; use the initial dispatch switch, not nested mapping switches.
            switch = re.search(r"switch\s*\(request\.Action\)\s*\{", service)
        if switch is None:
            raise ValueError(f"Cannot find dispatch switch in {relative}")
        body = brace_body(service, switch.end()-1)
        supported = re.findall(r"case\s+\(int\)INVOICE_ACTION\.(\w+)\s*:", body)
        for arm in re.findall(r"((?:\(int\)INVOICE_ACTION\.\w+(?:\s+or\s+)?)+)\s*=>", body):
            supported.extend(re.findall(r"INVOICE_ACTION\.(\w+)", arm))
        partner["supportedActions"] = sorted(set(supported))
        partner["implementationStatus"] = "not-implemented"
        partner["source"] = relative

    (reference / "legacy-contract-inventory.json").write_text(
        json.dumps({"baselineCommit": BASELINE, "actions": actions, "providers": partners,
                    "endpoints": endpoints}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    legacy_docs = reference / "legacy-docs"
    legacy_docs.mkdir(exist_ok=True)
    for file in sorted((source / "docs" / "rewrite").glob("*.md")):
        raw = file.read_bytes()
        (legacy_docs / file.name).write_bytes(raw)
        metadata["files"].append({"path": f"docs/rewrite/{file.name}",
                                  "origin": "legacy working tree; untracked at capture",
                                  "sha256": hashlib.sha256(raw).hexdigest()})
    # Only these standalone helpers are captured as executable historical references.
    for relative in ["src/ezInvoice.Application/ViewModels/PmsViewModels/PmsInvoiceModels.cs",
                     "src/ezInvoice.Application/ViewModels/ExtraDataMapViewModel.cs",
                     "src/ezInvoice.Application/Services/CompanyAppService.cs",
                     "src/ezInvoice.Infra.ThirdParty/ezInvoice.Infra.ThirdParty.Misa/MisaRepository.cs",
                     "src/ezInvoice.Infra.ThirdParty/ezInvoice.Infra.ThirdParty.Misa/Models/EndpointApi.cs",
                     "src/ezInvoice.Infra.ThirdParty/ezInvoice.Infra.ThirdParty.Misa/Models/MisaResponseModel.cs"]:
        read(relative)  # Provenance hashes only; no whole legacy project or runtime configuration is copied.
    snapshots = ROOT / "tools" / "LegacyFixtureCapture" / "Reference"
    snapshots.mkdir(parents=True, exist_ok=True)
    for relative in ["src/ezInvoice.Application/Helper/MoneyToText.cs",
                     "src/ezInvoice.Application/Helper/XmlHelper.cs",
                     "src/ezInvoice.Infra.ThirdParty/ezInvoice.Infra.ThirdParty.SunEInvoice/Services/NumberReader.cs"]:
        captured = "\n".join(line.rstrip() for line in read(relative).splitlines()).rstrip() + "\n"
        (snapshots / Path(relative).name).write_text(captured, encoding="utf-8", newline="\n")
    (reference / "source-manifest.json").write_text(
        json.dumps(metadata, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Captured {len(endpoints)} endpoints, {len(actions)} actions, {len(partners)} providers; no network calls.")


if __name__ == "__main__":
    main()
