using CustomerFeedbackSystem.Data.Common;
namespace CustomerFeedbackSystem.Data.Sources;
public interface IDataSourceReader<TDto>
{
    IAsyncEnumerable<Result<TDto>> ReadAsync(CancellationToken cancellationToken = default);
}
