using Hakedis.Contracts;
using Hakedis.Storage;

namespace Hakedis.Tests;

public sealed class TeacherDirectoryTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "HakedisTests", Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(folder, "ogretmenler.json");
    private TeacherDirectory Store => new(FilePath);
    private static TeacherRecord Teacher(string name = "Örnek Öğretmen", string identity = "")
        => new(Guid.NewGuid(), name, identity, "", "Örnek Okul", "2341.04", null, null);
    [Fact]
    public void Teachers_survive_reopening_and_same_names_have_different_ids()
    {
        var a = Teacher(); var b = Teacher(); Store.Save(a); Store.Save(b);
        Assert.Equal(2, Store.Read().Count); Assert.Contains(a, Store.Read()); Assert.Contains(b, Store.Read());
    }
    [Fact]
    public void Update_preserves_id_and_other_people_and_keeps_previous_backup()
    {
        var a = Teacher("Birinci"); var b = Teacher("İkinci"); Store.Save(a); Store.Save(b);
        var edited = a with { FullName = "Düzenlendi", School = "Başka Okul" }; Store.Save(edited);
        Assert.Contains(edited, Store.Read()); Assert.Contains(b, Store.Read()); Assert.Equal(2, Store.Read().Count);
        Assert.Contains(a, new TeacherDirectory(FilePath + ".bak").Read());
    }
    [Fact]
    public void Delete_removes_only_selected_id()
    {
        var a = Teacher(); var b = Teacher(); Store.Save(a); Store.Save(b);
        Store.Delete(a.Id); Assert.Equal(b, Assert.Single(Store.Read()));
    }
    [Fact]
    public void Separate_instances_do_not_lose_each_others_changes()
    {
        var first = Store; var second = Store; var a = Teacher(); var b = Teacher();
        first.Save(a); second.Save(b); first.Save(a with { School = "Yeni" });
        Assert.Equal(2, second.Read().Count); Assert.Contains(b, second.Read());
    }
    [Fact]
    public void Duplicate_identity_is_rejected_but_editing_same_person_is_allowed()
    {
        var a = Teacher(identity: "11111111111"); Store.Save(a); Store.Save(a with { FullName = "Yeni ad" });
        Assert.Throws<ArgumentException>(() => Store.Save(Teacher(identity: a.IdentityNumber))); Assert.Single(Store.Read());
    }
    [Fact]
    public void Invalid_teacher_does_not_change_saved_data()
    {
        var a = Teacher(); Store.Save(a);
        Assert.Throws<ArgumentException>(() => Store.Save(Teacher("  ")));
        Assert.Throws<ArgumentException>(() => Store.Save(Teacher(identity: "123")));
        Assert.Throws<ArgumentException>(() => Store.Save(Teacher() with { Iban = "TR123" }));
        Assert.Throws<ArgumentException>(() => Store.Save(Teacher() with { StartDate = new(2026, 9, 2), EndDate = new(2026, 9, 1) }));
        Assert.Equal(a, Assert.Single(Store.Read()));
    }
    [Fact]
    public void Corrupt_catalog_is_never_silently_replaced()
    {
        Store.Save(Teacher()); File.WriteAllText(FilePath, "broken");
        Assert.Throws<InvalidDataException>(() => Store.Read()); Assert.Throws<InvalidDataException>(() => Store.Save(Teacher()));
        Assert.Equal("broken", File.ReadAllText(FilePath));
    }
    public void Dispose() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
}
