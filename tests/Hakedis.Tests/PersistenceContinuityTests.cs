using Hakedis.App;
using Hakedis.Contracts;

namespace Hakedis.Tests;

public sealed class PersistenceContinuityTests : IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"HakedisTests",Guid.NewGuid().ToString("N"));
    private string TeacherFile=>Path.Combine(root,"ogretmenler.json");

    [Fact]
    public void Reopening_shows_saved_payroll_instead_of_an_empty_entry_panel()
    {
        var first=new ShellViewModel(TeacherFile);
        first.Registration.Editor.FullName="Kalıcı Örnek";
        first.Registration.Editor.Iban="TR330006100519786457841326";
        first.Registration.Editor.OccupationCode="2341.04";
        first.SaveNewTeacher();first.Calculation.Month=10;first.Monthly.Month=10;
        first.Calculation.Rows.Single().DayHours="117";
        first.Calculation.CreatePayment();
        var original=first.Outputs.Records.Single();var bytes=File.ReadAllBytes(original.Path);

        var reopened=new ShellViewModel(TeacherFile);

        Assert.True(reopened.OutputsVisible);
        Assert.Equal(original.Path,reopened.Outputs.Selected?.Path);
        Assert.Equal(2026,reopened.Calculation.Year);Assert.Equal(10,reopened.Calculation.Month);
        Assert.Equal(bytes,File.ReadAllBytes(original.Path));
        Assert.Single(reopened.Teachers.Records);
    }

    [Fact]
    public void Reopening_restores_the_saved_module_for_entry_and_calculation()
    {
        var first=new ShellViewModel(TeacherFile);
        var module=first.Workspace.Modules.Single(m=>m.Id==ModuleId.HalkEgitimUstaOgretici);
        first.RegistrationModule=module;first.Monthly.SelectedModule=module;first.Calculation.SelectedModule=module;
        first.Calculation.Month=10;first.Monthly.Month=10;
        first.Registration.Editor.FullName="Kalıcı Usta Öğretici";
        first.Registration.Editor.Iban="TR330006100519786457841326";
        first.Registration.Editor.OccupationCode="2341.04";first.SaveNewTeacher();
        first.Calculation.Rows.Single().DayHours="60";first.Calculation.CreatePayment();
        var reopened=new ShellViewModel(TeacherFile);
        Assert.True(reopened.OutputsVisible);Assert.Single(reopened.Outputs.Records);
        Assert.Equal(module.Id,reopened.Workspace.SelectedModule!.Id);
        Assert.Equal(module.Id,reopened.RegistrationModule.Id);Assert.Equal(module.Id,reopened.ListModule.Id);
        Assert.Equal(module.Id,reopened.Monthly.SelectedModule.Id);Assert.Equal(module.Id,reopened.Calculation.SelectedModule.Id);
        Assert.Equal(10,reopened.Monthly.Month);Assert.Single(reopened.Teachers.Records);
    }

    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
}
