using Hakedis.Contracts;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Hakedis.Storage;

public sealed class TeacherDirectory
{
    private readonly string path;
    private readonly ModuleId module;
    private sealed record Catalog(int SchemaVersion, TeacherRecord[] Teachers);
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) } };
    public TeacherDirectory(string path, ModuleId module = ModuleId.TemelEgitim)
    {
        if (!Enum.IsDefined(module)) throw new ArgumentOutOfRangeException(nameof(module));
        this.path = Path.GetFullPath(path); this.module = module;
    }
    public IReadOnlyList<TeacherRecord> Read() => ReadAll().Where(t => t.ModuleId == module).ToArray();
    private IReadOnlyList<TeacherRecord> ReadAll()
    {
        if (!File.Exists(path)) return [];
        try
        {
            using var stream = File.OpenRead(path); using var json = JsonDocument.Parse(stream);
            if(json.RootElement.ValueKind!=JsonValueKind.Object ||
               !json.RootElement.TryGetProperty("SchemaVersion",out var schema) || schema.ValueKind!=JsonValueKind.Number ||
               !schema.TryGetInt32(out var version) || version is not (1 or 2 or 3))
                throw new InvalidDataException("Öğretmen dosyasının sürümü veya içeriği geçersiz.");
            var node = JsonNode.Parse(json.RootElement.GetRawText())!;
            // Yalnız eski şemaların kaldırılmış alanları temizlenir; diğer bilinmeyen alanlar reddedilir.
            if (version is 1 or 2 && node["Teachers"] is JsonArray legacy)
                foreach (var item in legacy.OfType<JsonObject>())
                    foreach (var key in new[]{"InsuranceNumber","Branch","School2","Branch2"}) item.Remove(key);
            var catalog = node.Deserialize<Catalog>(Options);
            if (catalog is null || catalog.SchemaVersion is not (1 or 2 or 3) || catalog.Teachers is null)
                throw new InvalidDataException("Öğretmen dosyasının sürümü veya içeriği geçersiz.");
            if (catalog.SchemaVersion >= 2 && json.RootElement.GetProperty("Teachers").EnumerateArray().Any(t =>
                t.ValueKind != JsonValueKind.Object || !t.TryGetProperty("ModuleId", out _)))
                throw new InvalidDataException("Öğretmen kaydının ait olduğu modül belirtilmemiş.");
            var records = catalog.Teachers.Select(Normalize).ToArray();
            // İlk sürümün kapsamı Temel Eğitim idi; modülsüz şema 1 bu modüle ait kabul edilir.
            if (catalog.SchemaVersion == 1 && records.Any(t => t.ModuleId != ModuleId.TemelEgitim))
                throw new InvalidDataException("Eski kayıt dosyasında beklenmeyen modül bilgisi var.");
            if (records.Select(t => t.Id).Distinct().Count() != records.Length ||
                records.Where(t => t.IdentityNumber != "").GroupBy(t => t.IdentityNumber).Any(g => g.Count() > 1))
                throw new InvalidDataException("Öğretmen dosyasında yinelenen kimlik bilgisi var.");
            return records;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        { throw new InvalidDataException("Öğretmen dosyası bozuk veya kayıt bilgileri geçersiz; dosya değiştirilmedi.", ex); }
    }
    public IReadOnlyList<TeacherRecord> Save(TeacherRecord teacher) => SaveCore(teacher, false);
    public IReadOnlyList<TeacherRecord> Update(TeacherRecord teacher) => SaveCore(teacher, true);
    public IReadOnlyList<TeacherRecord> UpdateBatch(IReadOnlyList<TeacherRecord> expected, IReadOnlyList<TeacherRecord> replacements)
        => Mutate(rows =>
        {
            if(expected.Count!=replacements.Count || expected.Select(t=>t.Id).Distinct().Count()!=expected.Count)
                throw new ArgumentException("Düzenlenen kayıtlar uyuşmuyor.");
            for(var i=0;i<expected.Count;i++)
            {
                var original=expected[i];var next=Normalize(replacements[i]);
                var index=rows.FindIndex(t=>t.Id==original.Id);
                if(index<0 || rows[index]!=original)throw new InvalidOperationException("Öğretmen bilgileri değişmiş. Listeyi yenileyin.");
                if(original.ModuleId!=module || next.ModuleId!=module || next.Id!=original.Id)
                    throw new InvalidOperationException("Öğretmenin kimliği veya modülü değiştirilemez.");
                rows[index]=next;
            }
            if(rows.Where(t=>t.IdentityNumber!="").GroupBy(t=>t.IdentityNumber).Any(g=>g.Count()>1))
                throw new ArgumentException("Bu T.C. kimlik numarası zaten kayıtlı. Aynı öğretmen farklı bir modüle kaydedilemez.");
        });
    private IReadOnlyList<TeacherRecord> SaveCore(TeacherRecord teacher, bool requireExisting)
    {
        teacher = Normalize(teacher);
        if (teacher.ModuleId != module) throw new InvalidOperationException("Bu öğretmen seçili modüle ait değil.");
        return Mutate(rows =>
        {
            var index = rows.FindIndex(t => t.Id == teacher.Id);
            if (index >= 0 && rows[index].ModuleId != module)
                throw new InvalidOperationException("Öğretmen kaydı başka bir modüle taşınamaz.");
            if (teacher.IdentityNumber != "" && rows.Any(t => t.Id != teacher.Id && t.IdentityNumber == teacher.IdentityNumber))
                throw new ArgumentException("Bu T.C. kimlik numarası zaten kayıtlı. Aynı öğretmen farklı bir modüle kaydedilemez.");
            if (requireExisting && index < 0) throw new InvalidOperationException("Düzenlenecek öğretmen kaydı bulunamadı. Listeyi yenileyin.");
            if (index < 0) rows.Add(teacher); else rows[index] = teacher;
        });
    }
    public IReadOnlyList<TeacherRecord> Delete(Guid id) => Mutate(rows =>
    {
        if (rows.RemoveAll(t => t.Id == id && t.ModuleId == module) == 0) throw new InvalidOperationException("Öğretmen kaydı bu modülde bulunamadı.");
    });
    private IReadOnlyList<TeacherRecord> Mutate(Action<List<TeacherRecord>> action)
    {
        new PayrollEditStore(path).Recover();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var held = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        // Ortak kilit altında tüm katalog okunur; diğer modüller korunur ve TCKN tekilliği denetlenir.
        var rows = ReadAll().ToList(); action(rows);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { JsonSerializer.Serialize(stream, new Catalog(3, rows.ToArray()), Options); stream.Flush(true); }
            _ = new TeacherDirectory(temporary, module).Read();
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak"); else File.Move(temporary, path);
            return rows.Where(t => t.ModuleId == module).ToArray();
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public static TeacherRecord Normalize(TeacherRecord teacher)
    {
        ArgumentNullException.ThrowIfNull(teacher);
        if (!Enum.IsDefined(teacher.ModuleId)) throw new ArgumentException("Öğretmenin modül bilgisi geçersiz.");
        if (teacher.Id == Guid.Empty || string.IsNullOrWhiteSpace(teacher.FullName))
            throw new ArgumentException("Öğretmenin adını ve soyadını girin.");
        if (teacher.IdentityNumber is null || teacher.Iban is null ||
            teacher.School is null || teacher.OccupationCode is null)
            throw new ArgumentException("Öğretmen alanları eksik.");
        var normalized = teacher with { FullName = teacher.FullName.Trim(), IdentityNumber = teacher.IdentityNumber.Trim(),
            Iban = teacher.Iban.Replace(" ", "").ToUpperInvariant(),
            School = teacher.School.Trim(), OccupationCode = teacher.OccupationCode.Trim(),
            StartDate = teacher.StartDate?.Date, EndDate = teacher.EndDate?.Date };
        if (normalized.IdentityNumber != "" && !Regex.IsMatch(normalized.IdentityNumber, "^[0-9]{11}$"))
            throw new ArgumentException("T.C. kimlik numarası 11 rakam olmalıdır veya boş bırakılmalıdır.");
        if (normalized.Iban != "" && !Regex.IsMatch(normalized.Iban, "^TR[0-9]{24}$"))
            throw new ArgumentException("IBAN, TR ile başlayan 26 karakter olmalıdır veya boş bırakılmalıdır.");
        if (normalized.EndDate < normalized.StartDate) throw new ArgumentException("Ayrılış tarihi başlangıç tarihinden önce olamaz.");
        return normalized;
    }
}
