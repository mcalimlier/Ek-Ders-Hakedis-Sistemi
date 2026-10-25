using System.Text.Json;
using Hakedis.Contracts;
using Hakedis.Storage;

namespace Hakedis.Tests;

public sealed class ModuleTeacherTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "HakedisTests", Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(folder, "ogretmenler.json");
    private TeacherDirectory Store(ModuleId module) => new(FilePath, module);
    private static TeacherRecord Teacher(ModuleId module, string identity = "") => new(Guid.NewGuid(),
        "Örnek Öğretmen", identity, "", "Okul", "", null, null, module);
    [Fact]
    public void Reads_only_own_module_and_retains_other_modules_after_updates_and_deletes()
    {
        var basic = Teacher(ModuleId.TemelEgitim); var secondary = Teacher(ModuleId.Ortaogretim);
        Store(basic.ModuleId).Save(basic); Store(secondary.ModuleId).Save(secondary);
        Assert.Equal(basic, Assert.Single(Store(basic.ModuleId).Read()));
        Assert.Equal(secondary, Assert.Single(Store(secondary.ModuleId).Read()));
        Store(basic.ModuleId).Update(basic with { FullName = "Düzenlendi" });
        Store(basic.ModuleId).Delete(basic.Id);
        Assert.Empty(Store(basic.ModuleId).Read()); Assert.Equal(secondary, Assert.Single(Store(secondary.ModuleId).Read()));
    }
    [Fact]
    public void Foreign_module_cannot_save_update_or_delete_a_basic_teacher()
    {
        var basic = Teacher(ModuleId.TemelEgitim); Store(basic.ModuleId).Save(basic);
        var secondary = Store(ModuleId.Ortaogretim);
        Assert.Throws<InvalidOperationException>(() => secondary.Save(basic));
        Assert.Throws<InvalidOperationException>(() => secondary.Update(basic));
        Assert.Throws<InvalidOperationException>(() => secondary.Delete(basic.Id));
        Assert.Equal(basic, Assert.Single(Store(basic.ModuleId).Read()));
    }
    [Fact]
    public void Identity_or_record_id_cannot_be_registered_to_a_second_module()
    {
        var basic = Teacher(ModuleId.TemelEgitim, "11111111111"); Store(basic.ModuleId).Save(basic);
        Assert.Throws<ArgumentException>(() => Store(ModuleId.Ortaogretim).Save(Teacher(ModuleId.Ortaogretim, basic.IdentityNumber)));
        Assert.Throws<InvalidOperationException>(() => Store(ModuleId.Ortaogretim).Save(basic with { ModuleId = ModuleId.Ortaogretim, IdentityNumber = "" }));
        Assert.Equal(basic, Assert.Single(Store(basic.ModuleId).Read()));
    }
    [Fact]
    public void Legacy_catalog_is_basic_only_and_migrates_on_write_with_original_backup()
    {
        Directory.CreateDirectory(folder); var id = Guid.NewGuid();
        var text = JsonSerializer.Serialize(new { SchemaVersion = 1, Teachers = new[] { new { Id = id,
            FullName = "Eski Öğretmen", IdentityNumber = "", InsuranceNumber = "", Iban = "", School = "", Branch = "", OccupationCode = "", StartDate = (DateTime?)null, EndDate = (DateTime?)null } } });
        File.WriteAllText(FilePath, text);
        Assert.Equal(id, Assert.Single(Store(ModuleId.TemelEgitim).Read()).Id); Assert.Empty(Store(ModuleId.Ortaogretim).Read());
        Assert.Equal(text, File.ReadAllText(FilePath));
        Store(ModuleId.Ortaogretim).Save(Teacher(ModuleId.Ortaogretim));
        Assert.Equal(text, File.ReadAllText(FilePath + ".bak"));
        using var json = JsonDocument.Parse(File.ReadAllText(FilePath));
        Assert.Equal(4, json.RootElement.GetProperty("SchemaVersion").GetInt32());
        Assert.Equal("TemelEgitim", json.RootElement.GetProperty("Teachers")[0].GetProperty("ModuleId").GetString());
        Assert.Equal(id, Assert.Single(Store(ModuleId.TemelEgitim).Read()).Id);
    }
    [Fact]
    public void Version_two_cannot_guess_missing_module_ownership()
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(FilePath, "{\"SchemaVersion\":2,\"Teachers\":[{\"Id\":\"" + Guid.NewGuid() + "\",\"FullName\":\"Kişi\",\"IdentityNumber\":\"\",\"InsuranceNumber\":\"\",\"Iban\":\"\",\"School\":\"\",\"Branch\":\"\",\"OccupationCode\":\"\"}]}");
        Assert.Throws<InvalidDataException>(() => Store(ModuleId.TemelEgitim).Read());
    }
    public void Dispose() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
}
