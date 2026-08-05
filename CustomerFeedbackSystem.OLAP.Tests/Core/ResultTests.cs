using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using FluentAssertions;

namespace CustomerFeedbackSystem.OLAP.Tests.Core;


public sealed class ResultTests
{
    private static SourceUnavailableError AnyError() => new("fake", "because");

    [Fact]
    public void Success_WithNoErrors_IsSuccessful()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Failure_WithOneError_IsFailed()
    {
        var result = Result.Failure(AnyError());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle();
    }

    [Fact]
    public void Failure_WithEmptyErrorList_Throws()
    {
        var act = () => Result.Failure(Array.Empty<Error>());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least one error*");
    }

    [Fact]
    public void Value_OnFailedResult_Throws()
    {
        var result = Result<int>.Failure(AnyError());

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Value_OnSuccessfulResult_ReturnsTheValue()
    {
        Result<int>.Success(42).Value.Should().Be(42);
    }

    [Fact]
    public void ErrorToString_UsesBracketedCodeFormat()
    {
        AnyError().ToString().Should().Be("[SOURCE_UNAVAILABLE] fake could not be read: because");
    }

    [Fact]
    public void TruncatedError_CarriesTheTruncatedCode()
    {
        new FieldTruncatedError("csv", 1, "Comentario", 50).Code.Should().Be("TRUNCATED");
    }
}
