using Hakedis.App;
using Hakedis.Contracts;
using Hakedis.Storage;

namespace Hakedis.Tests;

public sealed class PersonalPayrollTests : IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"HakedisPersonalTests",Guid.NewGuid().ToString("N"));
    private string Catalog=>Path.Combine(root,"ogretmenler.json");
    private ShellViewModel Setup()
    {
        var shell=new ShellViewModel(Catalog);
        foreach(var (name,tc) in new[]{("Birinci Öğretmen","10000000146"),("İkinci Öğretmen","10000000200")})
        {
            shell.Registration.Editor.FullName=name;shell.Registration.Editor.IdentityNumber=tc;
            shell.Registration.Editor.Iban="TR330006100519786457841326";shell.Registration.Editor.OccupationCode="2341.04";
            shell.SaveNewTeacher();
        }
        shell.Calculation.Rows[0].DayHours="117";shell.Calculation.CreatePayment();return shell;
    }
    private PersonalPayrollViewModel Open(ShellViewModel shell)
    {shell.Navigate("Personal");shell.Personal.Year=2026;shell.Personal.Month=9;shell.Personal.Reload();return shell.Personal;}
    [Fact] public void List_comes_only_from_the_selected_archived_period_not_the_current_catalog()
    {
        var shell=Setup();var personal=Open(shell);var person=Assert.Single(personal.Teachers);
        Assert.Equal("Birinci Öğretmen",person.FullName);Assert.Equal("10000000146",person.IdentityNumber);
        Assert.Equal(2,shell.Teachers.Records.Count);Assert.False(personal.CanCreate);
        personal.SelectedTeacher=person;Assert.True(personal.CanCreate);
        Assert.Equal(25806.89m,personal.ReadSelection().Document.Data.Rows.Single(r=>r.Teacher.Id==person.Id).Result.Gross);
        Assert.True(shell.PersonalVisible);Assert.Equal("Kişisel Bordro",shell.Title);
    }
    [Fact] public void Changing_period_or_module_clears_selection_and_never_leaks_old_teachers()
    {
        var shell=Setup();var p=Open(shell);p.SelectedTeacher=p.Teachers.Single();
        p.Month=10;Assert.Empty(p.Teachers);Assert.Null(p.SelectedTeacher);Assert.False(p.CanCreate);
        Assert.Throws<InvalidOperationException>(()=>p.ReadSelection());
        p.Month=9;p.SelectedTeacher=p.Teachers.Single();p.Year=2027;Assert.Empty(p.Teachers);
        p.Year=2026;p.SelectedModule=p.Modules.Single(m=>m.Id==ModuleId.Ortaogretim);Assert.Empty(p.Teachers);Assert.False(p.CanCreate);
    }
    [Fact] public void Deleted_or_renamed_master_teacher_and_new_constants_do_not_change_historical_personal_payroll()
    {
        var shell=Setup();var record=shell.Outputs.Records.Single();var bytes=File.ReadAllBytes(record.Path);
        var teacher=record.Document.Data.Rows.Single().Teacher;
        new TeacherDirectory(Catalog).Delete(teacher.Id);new CommonConstantsStore(root).Save(PeriodValues.Source with{Coefficient=2});
        var p=Open(shell);p.SelectedTeacher=Assert.Single(p.Teachers);
        var selected=p.ReadSelection().Document.Data.Rows.Single();
        Assert.Equal(teacher,selected.Teacher);Assert.Equal(25806.89m,selected.Result.Gross);
        Assert.Equal(bytes,File.ReadAllBytes(record.Path));
    }
    [Fact] public void A_changed_source_archive_requires_refresh_before_pdf_creation()
    {
        var shell=Setup();var p=Open(shell);p.SelectedTeacher=p.Teachers.Single();
        var record=shell.Outputs.Records.Single();File.AppendAllText(record.Path," ");
        Assert.Throws<InvalidOperationException>(()=>p.ReadSelection());
        p.Reload();p.SelectedTeacher=p.Teachers.Single();Assert.NotNull(p.ReadSelection());
    }
    [Fact] public void A_foreign_teacher_cannot_be_selected_and_deleted_archive_clears_the_list()
    {
        var shell=Setup();var p=Open(shell);p.SelectedTeacher=shell.Teachers.Records.Single(t=>!p.Teachers.Any(a=>a.Id==t.Id));
        Assert.Null(p.SelectedTeacher);Assert.False(p.CanCreate);
        var record=shell.Outputs.Records.Single();new PaymentArchiveStore(root).Delete(record,true);
        p.Reload();Assert.Empty(p.Teachers);Assert.False(p.CanCreate);Assert.False(p.CanOpenFolder);
    }
    [Fact] public void Teacher_list_navigation_uses_the_shortened_title()
    {var shell=Setup();shell.Navigate("Teachers");Assert.Equal("Öğretmen Listesi",shell.Title);}
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
}
