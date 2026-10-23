$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    dotnet restore eInvoice.sln --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
    dotnet build eInvoice.sln --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    dotnet test eInvoice.sln --configuration Release --no-build --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
} finally {
    Pop-Location
}
