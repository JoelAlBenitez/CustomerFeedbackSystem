namespace CustomerFeedbackSystem.Data.Transformation;

/// <summary>
/// Turns the distinct values of a CSV column (e.g. Categoría, TipoFuente) into the
/// catalog rows a lookup table needs, and the name-to-id dictionary later used to
/// resolve each row's foreign key. Assumes the caller already allocated one id per
/// distinct value, in the same order returned by <see cref="DistinctValues{TDto}"/>.
/// </summary>
internal static class LookupCatalogBuilder
{
    public static IReadOnlyList<string> DistinctValues<TDto>(
        IEnumerable<TDto> records,
        Func<TDto, string> effectiveValueSelector)
    {
        return records
            .Select(effectiveValueSelector)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyDictionary<string, int> ToIdMap(IReadOnlyList<string> values, IReadOnlyList<int> assignedIds)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < values.Count; i++)
        {
            map[values[i]] = assignedIds[i];
        }

        return map;
    }
}
