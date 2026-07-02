using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CustomerFeedbackSystem.Load.Common;
using CustomerFeedbackSystem.Load.Common.Errors;

namespace CustomerFeedbackSystem.Load.Sources;

public sealed class CsvDataSourceReader<TDto, TMap> : IDataSourceReader<TDto>
    where TMap : ClassMap<TDto>
{
    private readonly string _filePath;
    private readonly string _sourceName;

    public CsvDataSourceReader(string filePath, string sourceName)
    {
        _filePath = filePath;
        _sourceName = sourceName;
    }

    public async IAsyncEnumerable<Result<TDto>> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            yield return Result<TDto>.Failure(new SourceUnavailableError(_sourceName, $"file not found at '{_filePath}'"));
            yield break;
        }

        StreamReader? streamReader = null;
        CsvReader? csvReader = null;
        Exception? openException = null;
        try
        {
            streamReader = new StreamReader(_filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                BadDataFound = null,
                MissingFieldFound = null,
            };
            csvReader = new CsvReader(streamReader, configuration);
            csvReader.Context.RegisterClassMap<TMap>();
        }
        catch (Exception ex)
        {
            openException = ex;
        }

        if (openException is not null)
        {
            streamReader?.Dispose();
            csvReader?.Dispose();
            yield return Result<TDto>.Failure(new SourceUnavailableError(_sourceName, openException.Message));
            yield break;
        }

        using (streamReader)
        using (csvReader)
        {
            Exception? headerException = null;
            try
            {
                await csvReader!.ReadAsync();
                csvReader.ReadHeader();
            }
            catch (Exception ex)
            {
                headerException = ex;
            }

            if (headerException is not null)
            {
                yield return Result<TDto>.Failure(new SourceUnavailableError(_sourceName, $"invalid header: {headerException.Message}"));
                yield break;
            }

            var rowNumber = 1;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool moved;
                Exception? readException = null;
                try
                {
                    moved = await csvReader!.ReadAsync();
                }
                catch (Exception ex)
                {
                    moved = false;
                    readException = ex;
                }

                if (readException is not null)
                {
                    yield return Result<TDto>.Failure(
                        new ValidationError(_sourceName, rowNumber + 1, "<row>", $"could not be read: {readException.Message}"));
                    yield break;
                }

                if (!moved)
                {
                    yield break;
                }

                rowNumber++;

                TDto? record = default;
                Exception? mapException = null;
                try
                {
                    record = csvReader!.GetRecord<TDto>();
                }
                catch (Exception ex)
                {
                    mapException = ex;
                }

                if (mapException is not null)
                {
                    yield return Result<TDto>.Failure(
                        new ValidationError(_sourceName, rowNumber, "<row>", $"could not be parsed: {mapException.Message}"));
                    continue;
                }

                yield return Result<TDto>.Success(record!);
            }
        }
    }
}
