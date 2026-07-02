using CustomerFeedbackSystem.Load.Common;
namespace CustomerFeedbackSystem.Load.Sources;
public interface IDataSourceReader<TDto>
{
    IAsyncEnumerable<Result<TDto>> ReadAsync(CancellationToken cancellationToken = default);
}
