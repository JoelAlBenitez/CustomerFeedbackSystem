using Catalyst;
using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Mosaik.Core;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Text;

public sealed class CatalystSpanishAnalyzer : ITokenAnalyzer
{
 
    private static readonly HashSet<PartOfSpeech> KeywordTags =
    [
        PartOfSpeech.NOUN,
        PartOfSpeech.PROPN,
        PartOfSpeech.ADJ,
        PartOfSpeech.VERB,
    ];

   
    private static readonly HashSet<string> SpanishStopWords =
        new(StopWords.Snowball.For(Language.Spanish), StringComparer.OrdinalIgnoreCase);

    private readonly ILogger<CatalystSpanishAnalyzer> _logger;
    private Pipeline? _pipeline;

    public CatalystSpanishAnalyzer(ILogger<CatalystSpanishAnalyzer> logger)
    {
        _logger = logger;
    }

    public bool IsReady => _pipeline is not null;

    public async Task WarmUpAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Catalyst.Models.Spanish.Register();

         
            Storage.Current = new DiskStorage("catalyst-models");

            _pipeline = await Pipeline.ForAsync(Language.Spanish);

            _logger.LogInformation("Spanish NLP model loaded and ready for the T phase.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _pipeline = null;
            _logger.LogWarning(
                ex,
                "Could not load the Spanish NLP model. Extraction continues — the E phase does not need it, "
                + "but the T phase will require this to succeed.");
        }
    }

    public IReadOnlyList<string> ExtractLemmas(string text)
    {
        if (_pipeline is null || string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var document = new Document(text, Language.Spanish);
        _pipeline.ProcessSingle(document);

     
        var lemmas = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var span in document)
        {
            foreach (var token in span)
            {
                if (!KeywordTags.Contains(token.POS))
                {
                    continue;
                }

               
                var lemma = token.Lemma.ToLowerInvariant();

                if (SpanishStopWords.Contains(lemma) || SpanishStopWords.Contains(token.Value))
                {
                    continue;
                }

                if (lemma.Length < 2 || !seen.Add(lemma))
                {
                    continue;
                }

                lemmas.Add(lemma);
            }
        }

        return lemmas;
    }
}
