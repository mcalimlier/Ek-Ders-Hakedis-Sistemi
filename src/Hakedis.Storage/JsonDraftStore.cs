using Hakedis.Contracts;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hakedis.Storage;

public sealed record DraftDocument<T>(int SchemaVersion, ModuleId ModuleId, string RuleVersion,
    DateTimeOffset SavedAt, T Data);

public sealed class JsonDraftStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };
    public void Save<T>(string path, DraftDocument<T> document)
    {
        Validate(document, document.ModuleId, document.RuleVersion);
        var full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        // Kilit dosyası kalabilir; sahiplik dosyanın varlığı değil açık FileShare.None tutamacıdır.
        using var held = new FileStream(full + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        if (File.Exists(full)) _ = Load<T>(full, document.ModuleId, document.RuleVersion);
        var temporary = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, document, Options); stream.Flush(flushToDisk: true);
            }
            _ = Load<T>(temporary, document.ModuleId, document.RuleVersion);
            if (File.Exists(full)) File.Replace(temporary, full, full + ".bak");
            else File.Move(temporary, full);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public DraftDocument<T> Load<T>(string path, ModuleId expectedModule, string expectedRule)
    {
        try
        {
            using var stream = File.OpenRead(path);
            var document = JsonSerializer.Deserialize<DraftDocument<T>>(stream, Options);
            Validate(document, expectedModule, expectedRule);
            return document!;
        }
        catch (JsonException ex) { throw new InvalidDataException("Taslak dosyası bozuk veya beklenen biçimde değil.", ex); }
    }
    private static void Validate<T>(DraftDocument<T>? document, ModuleId expectedModule, string expectedRule)
    {
        if (document is null || document.SchemaVersion != 1 || !Enum.IsDefined(document.ModuleId) ||
            document.ModuleId != expectedModule || string.IsNullOrWhiteSpace(document.RuleVersion) ||
            document.RuleVersion != expectedRule || document.SavedAt == default || document.Data is null)
            throw new InvalidDataException("Taslağın veri sürümü, modülü veya hesap kuralı uyuşmuyor; mevcut veri değiştirilmedi.");
    }
}
