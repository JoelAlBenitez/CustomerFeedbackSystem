using CustomerFeedbackSystem.Load.Common;

namespace CustomerFeedbackSystem.Load.Validation;
public interface IRecordValidator<TDto>
{
    Result<TDto> Validate(TDto record, int rowNumber, string sourceFile);
}
