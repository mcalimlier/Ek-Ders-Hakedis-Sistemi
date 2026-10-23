using Hakedis.App;

namespace Hakedis.Tests;

public sealed class TeacherPanelTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "HakedisTests", Guid.NewGuid().ToString("N"));
    private string PathName => Path.Combine(folder, "ogretmenler.json");
    [Fact]
    public void Switching_module_hides_other_teachers_and_clears_person_and_hours()
    {
        var shell = new ShellViewModel(PathName); shell.Registration.Editor.FullName = "Temel Öğretmeni";
        shell.SaveNewTeacher(); shell.Workspace.DayHours = "117"; shell.Workspace.Calculate();
        shell.Workspace.SelectedModule = shell.Workspace.Modules.Single(m => m.Id == Hakedis.Contracts.ModuleId.Ortaogretim);
        Assert.Empty(shell.Teachers.Records); Assert.Null(shell.CalculationTeacher);
        Assert.Equal("", shell.Workspace.Person); Assert.Equal("0", shell.Workspace.DayHours); Assert.Equal("—", shell.Workspace.NetText);
    }
    [Fact]
    public void New_registration_follows_active_module_and_returning_shows_only_basic_teachers()
    {
        var shell = new ShellViewModel(PathName); shell.Registration.Editor.FullName = "Temel Öğretmeni";
        shell.SaveNewTeacher(); var basic = shell.CalculationTeacher!;
        shell.Registration.Editor.FullName = "Kaydedilmemiş Temel";
        shell.Workspace.SelectedModule = shell.Workspace.Modules.Single(m => m.Id == Hakedis.Contracts.ModuleId.Ortaogretim);
        Assert.Equal("", shell.Registration.Editor.FullName);
        shell.Registration.Editor.FullName = "Orta Öğretmeni"; shell.SaveNewTeacher(); var secondary = shell.CalculationTeacher!;
        Assert.Equal(Hakedis.Contracts.ModuleId.Ortaogretim, secondary.ModuleId);
        Assert.Equal(secondary, Assert.Single(shell.Teachers.Records));
        shell.Workspace.SelectedModule = shell.Workspace.Modules.Single(m => m.Id == Hakedis.Contracts.ModuleId.TemelEgitim);
        Assert.Equal(basic, Assert.Single(shell.Teachers.Records));
        Assert.Throws<InvalidOperationException>(() => shell.CalculationTeacher = secondary);
        Assert.Throws<InvalidOperationException>(() => shell.Teachers.Selected = secondary);
    }
    [Fact]
    public void Switching_to_same_module_does_not_discard_entered_hours()
    {
        var shell = new ShellViewModel(PathName); shell.Workspace.DayHours = "45";
        shell.Workspace.SelectedModule = shell.Workspace.Modules.Single(m => m.Id == Hakedis.Contracts.ModuleId.TemelEgitim);
        Assert.Equal("45", shell.Workspace.DayHours);
    }
    [Fact]
    public void Teacher_list_cannot_create_a_new_record_without_existing_selection()
    {
        var vm = new TeacherListViewModel(PathName); vm.Editor.FullName = "Listeden eklenemez";
        Assert.Throws<InvalidOperationException>(vm.Save);
        Assert.Empty(new TeacherListViewModel(PathName).Records);
    }
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
        var entry = new TeacherRegistrationViewModel(PathName); entry.Editor.FullName = "İlk ad"; entry.SaveNew();
        var vm = new TeacherListViewModel(PathName); vm.Selected = vm.Records.Single();
        var id = vm.Selected!.Id; vm.Editor.FullName = "Yeni ad";
        Assert.Equal("İlk ad", Assert.Single(vm.Records).FullName);
        vm.Save(); var reopened = new TeacherListViewModel(PathName);
        Assert.Equal(id, Assert.Single(reopened.Records).Id); Assert.Equal("Yeni ad", reopened.Records[0].FullName);
    }
    [Fact]
    public void Registration_reset_and_list_delete_preserve_other_records()
    {
        var entry = new TeacherRegistrationViewModel(PathName); entry.Editor.FullName = "Bir"; entry.SaveNew();
        Assert.Equal("", entry.Editor.FullName);
        entry.Editor.FullName = "İki"; var second = entry.SaveNew();
        var vm = new TeacherListViewModel(PathName); vm.Selected = vm.Records.Single(t => t.Id == second.Id); vm.DeleteSelected();
        Assert.False(vm.HasSelection); Assert.Equal("Bir", Assert.Single(vm.Records).FullName);
        Assert.Single(new TeacherListViewModel(PathName).Records);
    }
    [Fact]
    public void Navigation_preserves_entry_and_selected_teacher_is_available_for_calculation()
    {
        var shell = new ShellViewModel(PathName); Assert.True(shell.EntryVisible);
        shell.Navigate("Teachers"); Assert.True(shell.TeachersVisible);
        shell.Navigate("Entry"); shell.Registration.Editor.FullName = "Örnek Öğretmen"; shell.SaveNewTeacher();
        shell.Teachers.Selected = shell.Teachers.Records.Single(); shell.UseSelectedTeacher();
        Assert.True(shell.EntryVisible); Assert.Equal("Örnek Öğretmen", shell.Workspace.Person);
        shell.Workspace.DayHours = "117"; shell.Navigate("Calculation"); shell.Workspace.Calculate();
        Assert.True(shell.CalculationVisible); Assert.NotEqual("—", shell.Workspace.NetText);
        shell.Navigate("Entry"); Assert.Equal("117", shell.Workspace.DayHours);
    }
    [Fact]
    public void Entry_registration_refreshes_list_selects_person_and_clears_new_form()
    {
        var shell = new ShellViewModel(PathName); shell.Registration.Editor.FullName = "Yeni Öğretmen";
        shell.SaveNewTeacher(); Assert.Single(shell.Teachers.Records);
        Assert.Equal("Yeni Öğretmen", shell.CalculationTeacher!.FullName);
        Assert.Equal("Yeni Öğretmen", shell.Workspace.Person); Assert.Equal("", shell.Registration.Editor.FullName);
        Assert.Throws<ArgumentException>(shell.SaveNewTeacher); Assert.Single(shell.Teachers.Records);
    }
    [Fact]
    public void A_record_deleted_elsewhere_cannot_be_recreated_by_list_edit()
    {
        var entry = new TeacherRegistrationViewModel(PathName); entry.Editor.FullName = "Öğretmen"; var saved = entry.SaveNew();
        var list = new TeacherListViewModel(PathName); list.Selected = list.Records.Single();
        new Hakedis.Storage.TeacherDirectory(PathName).Delete(saved.Id);
        list.Editor.FullName = "Değişti"; Assert.Throws<InvalidOperationException>(list.Save);
        Assert.Empty(new TeacherListViewModel(PathName).Records);
    }
    public void Dispose() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
}
