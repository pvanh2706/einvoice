using eInvoice.Core.Contracts;

namespace eInvoice.Core.Configuration;

public sealed record ResolvedSite(SiteConfiguration Site, IReadOnlyList<ExtraFieldMapping> ExtraFieldMappings);
