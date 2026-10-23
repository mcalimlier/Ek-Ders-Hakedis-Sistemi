using Hakedis.Contracts;
using Hakedis.Storage;

namespace Hakedis.Tests;

public class StorageTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "HakedisTests", Guid.NewGuid().ToString("N"));
    private readonly JsonDraftStore store = new();
    private string FilePath => Path.Combine(folder, "taslak.json");
    private static DraftDocument<string> Doc(string text) => new(1, ModuleId.TemelEgitim, "v1", DateTimeOffset.UtcNow, text);

    [Fact]
    public void Saving_again_keeps_previous_valid_draft_as_backup()
    {
        store.Save(FilePath, Doc("İlk kayıt"));
        store.Save(FilePath, Doc("İkinci kayıt"));
        Assert.Equal("İkinci kayıt", store.Load<string>(FilePath, ModuleId.TemelEgitim, "v1").Data);
        Assert.Equal("İlk kayıt", store.Load<string>(FilePath + ".bak", ModuleId.TemelEgitim, "v1").Data);
    }

    [Fact]
    public void Module_and_rule_mismatch_are_rejected_before_display()
    {
        store.Save(FilePath, Doc("veri"));
        Assert.Throws<InvalidDataException>(() => store.Load<string>(FilePath, ModuleId.Ortaogretim, "v1"));
        Assert.Throws<InvalidDataException>(() => store.Load<string>(FilePath, ModuleId.TemelEgitim, "v2"));
    }

    [Fact]
    public void Corrupt_existing_file_cannot_overwrite_last_good_backup()
    {
        store.Save(FilePath, Doc("sağlam"));
        store.Save(FilePath, Doc("yeni"));
        File.WriteAllText(FilePath, "{yarım dosya");
        Assert.Throws<InvalidDataException>(() => store.Save(FilePath, Doc("son")));
        Assert.Equal("sağlam", store.Load<string>(FilePath + ".bak", ModuleId.TemelEgitim, "v1").Data);
    }

    [Theory]
    [InlineData("null")] [InlineData("{}")] [InlineData("{\"SchemaVersion\":99}")] [InlineData("broken")]
    public void Invalid_document_is_not_treated_as_empty_data(string text)
    {
        Directory.CreateDirectory(folder); File.WriteAllText(FilePath, text);
        Assert.Throws<InvalidDataException>(() => store.Load<string>(FilePath, ModuleId.TemelEgitim, "v1"));
    }

    [Fact]
    public void Concurrent_writer_lock_prevents_save()
    {
        store.Save(FilePath, Doc("ilk"));
        using var held = new FileStream(FilePath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        Assert.Throws<IOException>(() => store.Save(FilePath, Doc("ikinci")));
        Assert.Equal("ilk", store.Load<string>(FilePath, ModuleId.TemelEgitim, "v1").Data);
    }

    public void Dispose() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
}
