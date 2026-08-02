using CustomerFeedbackSystem.Data.Persistence;
using CustomerFeedbackSystem.Data.Reporting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace CustomerFeedbackSystem.Data.Orchestration;

public sealed class EtlRunContext
{
    public required SqlConnection Connection { get; init; }

    public required SqlTransaction Transaction { get; init; }

    public required IdentityAllocator IdentityAllocator { get; init; }

    public required ISqlBulkLoader BulkLoader { get; init; }

    public required LoadReport Report { get; init; }

    public required ILogger Logger { get; init; }

    public CancellationToken CancellationToken { get; init; }
}
