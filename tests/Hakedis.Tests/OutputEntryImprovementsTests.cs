using Hakedis.App;
using Hakedis.Contracts;
using Hakedis.Storage;
namespace Hakedis.Tests;
public sealed class OutputEntryImprovementsTests : IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"OutputEntry",Guid.NewGuid().ToString("N"));
    private string Catalog=>Path.Combine(root,"ogretmenler.json");
    private ShellViewModel Setup()
    {
        var shell=new ShellViewModel(Catalog);shell.Registration.Editor.FullName="Deneme";shell.Registration.Editor.OccupationCode="2341.04";shell.SaveNewTeacher();return shell;
    }
    [Fact] public void Monthly_card_requires_explicit_module_and_teacher_even_after_registration()
    {
        var shell=Setup();Assert.Null(shell.Monthly.EntryModule);Assert.Null(shell.Monthly.EntryTeacher);Assert.Empty(shell.Monthly.EntryTeachers);Assert.Equal("",shell.Monthly.EntryDayHours);
        shell.Monthly.EntryModule=shell.Monthly.Modules.First();Assert.Null(shell.Monthly.EntryTeacher);Assert.Single(shell.Monthly.EntryTeachers);
        shell.Monthly.EntryTeacher=shell.Monthly.EntryTeachers.Single();shell.Monthly.EntryDayHours="25";Assert.Equal(25,shell.Monthly.Rows.Single().Read().DayHours);
    }
    [Fact] public void Missing_iban_can_be_saved_but_blocks_payment_with_actionable_message()
    {
        var shell=Setup();Assert.Equal("",new TeacherDirectory(Catalog).Read().Single().Iban);
        shell.Calculation.Rows.Single().DayHours="25";var ex=Assert.Throws<ArgumentException>(shell.Calculation.CreatePayment);
        Assert.Contains("IBAN",ex.Message);Assert.Contains("güncelle",ex.Message.ToLowerInvariant());Assert.Empty(shell.Outputs.Records);
    }
    [Fact] public void Output_filter_and_detail_preserve_archived_bytes()
    {
        var shell=Setup();var t=new TeacherDirectory(Catalog).Read().Single();new TeacherDirectory(Catalog).UpdateBatch([t],[t with {Iban="TR330006100519786457841326"}]);shell.Calculation.RefreshTeachers();shell.Calculation.Rows.Single().DayHours="25";shell.Calculation.CreatePayment();
        var a=shell.Outputs.Records.Single();var bytes=File.ReadAllBytes(a.Path);
        shell.Outputs.SelectedModule=shell.Outputs.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);Assert.Empty(shell.Outputs.Records);
        shell.Outputs.SelectedModule=shell.Outputs.Modules.Single(m=>m.Id==ModuleId.TemelEgitim);Assert.Single(shell.Outputs.Records);
        var detail=new PaymentDetailsViewModel(shell.Outputs.ReadSelected());Assert.Single(detail.Rows);Assert.Equal(a.Net,detail.Net);Assert.Equal(bytes,File.ReadAllBytes(a.Path));
        var reopened=new ShellViewModel(Catalog);Assert.Null(reopened.Monthly.EntryModule);Assert.Null(reopened.Monthly.EntryTeacher);Assert.Single(reopened.Outputs.Records);
    }
    [Fact] public void Taking_teacher_into_entry_is_an_explicit_selection()
    {
        var shell=Setup();shell.Teachers.Selected=shell.Teachers.Records.Single();shell.UseSelectedTeacher();
        Assert.NotNull(shell.Monthly.EntryModule);Assert.Equal(shell.Teachers.Selected.Id,shell.Monthly.EntryTeacher!.Id);
    }
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
}
