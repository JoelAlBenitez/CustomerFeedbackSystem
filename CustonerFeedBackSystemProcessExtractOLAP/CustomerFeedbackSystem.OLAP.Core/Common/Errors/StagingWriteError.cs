namespace CustomerFeedbackSystem.OLAP.Core.Common.Errors;

public sealed record StagingWriteError(string TableName, string Reason)
    : Error("STAGING_WRITE", $"staging table '{TableName}' write failed: {Reason}");
