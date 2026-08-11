using CustomerFeedbackSystem.OLAP.Core.Facts;

namespace CustomerFeedbackSystem.OLAP.Core.Abstractions;

public interface ISentimentClassifier
{
    SentimentPolarity Classify(byte? puntaje, string? texto);
}
