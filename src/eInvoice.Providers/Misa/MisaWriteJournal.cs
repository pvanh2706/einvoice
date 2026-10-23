using eInvoice.Providers.Common;

namespace eInvoice.Providers.Misa;

// Preserve the existing DI type, journal paths and idempotency hashes for MISA.
public sealed class MisaWriteJournal(string directory, TimeProvider clock) : WriteOperationJournal(directory, clock, "MISA");
