using System.Globalization;
using System.Text.Json;

namespace Hakedis.Contracts;

public sealed record Occupation(string Code, string Name)
{
    public string Display => $"{Code} — {Name}";
}
public static class OccupationCatalog
{
    public static IReadOnlyList<Occupation> All { get; } = Load();
    private static IReadOnlyList<Occupation> Load()
    {
        using var stream = typeof(OccupationCatalog).Assembly.GetManifestResourceStream("Hakedis.Contracts.Data.occupations.json")!;
        var rows = JsonSerializer.Deserialize<Occupation[]>(stream)!;
        if (rows.Length != 7190 || rows.Select(x => x.Code).Distinct().Count() != rows.Length)
            throw new InvalidDataException("Meslek kodları kaynağı eksik veya yinelenen kayıt içeriyor.");
        return Array.AsReadOnly(rows);
    }
    public static IReadOnlyList<Occupation> Search(string? query)
    {
        var words = (query ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var compare = CultureInfo.GetCultureInfo("tr-TR").CompareInfo;
        return words.Length == 0 ? All : All.Where(x => words.All(w => compare.IndexOf(x.Display, w,
            CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0)).ToArray();
    }
    public static bool Contains(string code) => All.Any(x => x.Code == code);
}
