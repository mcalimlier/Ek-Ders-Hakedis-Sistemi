using Hakedis.App;

namespace Hakedis.Tests;

public sealed class TeacherPanelTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "HakedisTests", Guid.NewGuid().ToString("N"));
    private string PathName => Path.Combine(folder, "ogretmenler.json");
    [Fact]
    public void Broken_catalog_shows_error_without_crashing_or_overwriting_file()
    {
        Directory.CreateDirectory(folder); File.WriteAllText(PathName, "yarım dosya");
        var vm = new TeacherListViewModel(PathName);
        Assert.Empty(vm.Records); Assert.Contains("bozuk", vm.Status);
        Assert.Equal("yarım dosya", File.ReadAllText(PathName));
    }
    [Fact]
    public void Editor_changes_do_not_modify_list_until_saved_and_survive_reopening()
    {
        var vm = new TeacherListViewModel(PathName); vm.Editor.FullName = "İlk ad"; vm.Save();
        var id = vm.Selected!.Id; vm.Editor.FullName = "Yeni ad";
        Assert.Equal("İlk ad", Assert.Single(vm.Records).FullName);
        vm.Save(); var reopened = new TeacherListViewModel(PathName);
        Assert.Equal(id, Assert.Single(reopened.Records).Id); Assert.Equal("Yeni ad", reopened.Records[0].FullName);
    }
    [Fact]
    public void New_clears_selection_and_delete_resets_the_editor()
    {
        var vm = new TeacherListViewModel(PathName); vm.Editor.FullName = "Bir"; vm.Save(); vm.New();
        Assert.False(vm.HasSelection); Assert.Equal("", vm.Editor.FullName);
        vm.Editor.FullName = "İki"; vm.Save(); vm.DeleteSelected();
        Assert.False(vm.HasSelection); Assert.Equal("Bir", Assert.Single(vm.Records).FullName);
        Assert.Single(new TeacherListViewModel(PathName).Records);
    }
    [Fact]
    public void Navigation_preserves_entry_and_selected_teacher_is_available_for_calculation()
    {
        var shell = new ShellViewModel(PathName); Assert.True(shell.EntryVisible);
        shell.Navigate("Teachers"); Assert.True(shell.TeachersVisible);
        shell.Teachers.Editor.FullName = "Örnek Öğretmen"; shell.Teachers.Save(); shell.UseSelectedTeacher();
        Assert.True(shell.EntryVisible); Assert.Equal("Örnek Öğretmen", shell.Workspace.Person);
        shell.Workspace.DayHours = "117"; shell.Navigate("Calculation"); shell.Workspace.Calculate();
        Assert.True(shell.CalculationVisible); Assert.NotEqual("—", shell.Workspace.NetText);
        shell.Navigate("Entry"); Assert.Equal("117", shell.Workspace.DayHours);
    }
    public void Dispose() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
}
