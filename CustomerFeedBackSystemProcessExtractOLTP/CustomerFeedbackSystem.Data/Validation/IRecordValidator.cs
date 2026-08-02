using CustomerFeedbackSystem.Data.Common;

namespace CustomerFeedbackSystem.Data.Validation;
public interface IRecordValidator<TDto>
{
    Result<TDto> Validate(TDto record, int rowNumber, string sourceFile);
}
