using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Facts;
using CustomerFeedbackSystem.OLAP.Infrastructure.Transformation;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Text;

public sealed class LexiconSentimentClassifier : ISentimentClassifier
{
    public SentimentPolarity Classify(byte? puntaje, string? texto)
    {
        if (puntaje is >= 1 and <= 5)
        {
            return puntaje.Value switch
            {
                <= 2 => SentimentPolarity.Negativa,
                3 => SentimentPolarity.Neutra,
                _ => SentimentPolarity.Positiva,
            };
        }

        var tokens = TextNormalization.TokenizeFolded(texto);
        if (tokens.Count == 0)
        {
            return SentimentPolarity.SinClasificar;
        }

        var score = SpanishSentimentLexicon.Score(tokens);

        if (score >= SpanishSentimentLexicon.DecisionThreshold)
        {
            return SentimentPolarity.Positiva;
        }

        if (score <= -SpanishSentimentLexicon.DecisionThreshold)
        {
            return SentimentPolarity.Negativa;
        }

        return SentimentPolarity.Neutra;
    }
}
