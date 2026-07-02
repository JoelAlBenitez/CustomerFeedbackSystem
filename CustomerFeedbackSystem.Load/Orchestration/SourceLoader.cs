using CustomerFeedbackSystem.Load.Common.Errors;
using CustomerFeedbackSystem.Load.Reporting;
using CustomerFeedbackSystem.Load.Sources;
using CustomerFeedbackSystem.Load.Validation;
namespace CustomerFeedbackSystem.Load.Orchestration;
internal static class SourceLoader
{
    public static async Task<List<(TDto Record, int RowNumber)>> ReadAndValidateAsync<TDto>(
        IDataSourceReader<TDto> reader,
        IRecordValidator<TDto> validator,
        SourceLoadStats stats,
        string sourceName,
        CancellationToken cancellationToken)
    {
        var valid = new List<(TDto Record, int RowNumber)>();
        var rowNumber = 1;

        await foreach (var readResult in reader.ReadAsync(cancellationToken))
        {
            if (readResult.IsFailure && readResult.Errors[0] is SourceUnavailableError)
            {
                stats.RecordRejected(readResult.Errors[0]);
                break;
            }

            rowNumber++;
            stats.RecordRead();

            if (readResult.IsFailure)
            {
                foreach (var error in readResult.Errors)
                {
                    stats.RecordRejected(error);
                }

                continue;
            }

            var validated = validator.Validate(readResult.Value, rowNumber, sourceName);
            if (validated.IsFailure)
            {
                foreach (var error in validated.Errors)
                {
                    stats.RecordRejected(error);
                }

                continue;
            }

            valid.Add((validated.Value, rowNumber));
        }

        return valid;
    }
}
