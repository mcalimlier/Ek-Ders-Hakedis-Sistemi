using System.Text.Json.Nodes;
using Hakedis.App;
using Hakedis.Contracts;
using Hakedis.Storage;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.Tests;

public sealed class ReadinessAuditTests : IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"HakedisAudit",Guid.NewGuid().ToString("N"));
    private string Catalog=>Path.Combine(root,"ogretmenler.json");
    private ShellViewModel Setup(int count=1)
    {
        var shell=new ShellViewModel(Catalog);
        for(var i=0;i<count;i++)
        {
            shell.Registration.Editor.FullName=$"Deneme Öğretmeni {i}";
            shell.Registration.Editor.Iban="TR330006100519786457841326";
            shell.Registration.Editor.OccupationCode="2341.04";
            shell.SaveNewTeacher();
        }
        return shell;
    }
    [Fact] public void Monthly_form_cannot_overwrite_hours_saved_in_another_window()
    {
        var first=Setup();first.Monthly.DayHours="30";first.Monthly.Save(true);
        var stale=new ShellViewModel(Catalog);stale.Monthly.SelectedTeacher=stale.Monthly.Teachers.Single();
        first.Monthly.DayHours="60";first.Monthly.Save(true);
        stale.Monthly.DayHours="90";
        Assert.Throws<InvalidOperationException>(()=>stale.Monthly.Save(true));
        Assert.Equal(60,new MonthlyPayrollStore(root).Read(ModuleId.TemelEgitim,2026,9).Entries.Single().DayHours);
        Assert.True(stale.Monthly.HasHybridChanges);
    }
    [Fact] public void Teacher_list_cannot_overwrite_an_edit_saved_in_another_window()
    {
        var first=Setup();first.Teachers.Selected=first.Teachers.Records.Single();
        var stale=new ShellViewModel(Catalog);stale.Teachers.Selected=stale.Teachers.Records.Single();
        first.Teachers.Editor.FullName="Güncel öğretmen";first.Teachers.Save();
        stale.Teachers.Editor.FullName="Eski ekranın değişikliği";
        Assert.Throws<InvalidOperationException>(stale.Teachers.Save);
        Assert.Equal("Güncel öğretmen",new TeacherDirectory(Catalog).Read().Single().FullName);
    }
    [Fact] public void Payroll_creation_rejects_a_stale_monthly_baseline_before_any_write()
    {
        var first=Setup();first.Monthly.DayHours="30";first.Monthly.Save(true);
        var stale=new ShellViewModel(Catalog);stale.Calculation.Rows[0].DayHours="90";
        first.Monthly.DayHours="60";first.Monthly.Save(true);
        var path=new MonthlyPayrollStore(root).FilePath(ModuleId.TemelEgitim,2026,9);var bytes=File.ReadAllBytes(path);
        Assert.Throws<InvalidOperationException>(stale.Calculation.CreatePayment);
        Assert.Equal(bytes,File.ReadAllBytes(path));Assert.Empty(new PaymentArchiveStore(root).ReadAll());
    }
    [Fact] public void Payroll_creation_cannot_silently_omit_new_inputs_from_another_window()
    {
        var first=Setup(2);first.Monthly.DayHours="30";first.Monthly.Save(true);
        var stale=new ShellViewModel(Catalog);
        first.Monthly.SelectedTeacher=first.Monthly.Teachers[0];first.Monthly.DayHours="60";first.Monthly.Save(true);
        Assert.Throws<InvalidOperationException>(stale.Calculation.CreatePayment);
        Assert.Empty(new PaymentArchiveStore(root).ReadAll());
    }
    [Fact] public void Identical_persisted_hours_allow_safe_retry_after_an_interrupted_creation()
    {
        var shell=Setup();var row=shell.Calculation.Rows.Single();row.DayHours="45";
        new MonthlyPayrollStore(root).Save(ModuleId.TemelEgitim,new(2026,9,PeriodValues.Source,[new(row.Teacher.Id,45,0,0)]));
        shell.Calculation.CreatePayment();
        Assert.Single(shell.Outputs.Records);Assert.Equal(45,shell.Outputs.Records[0].Document.Data.Rows[0].Input.DayHours);
    }
    [Fact] public void Invalid_report_settings_fail_before_monthly_inputs_are_written_and_retry_is_possible()
    {
        var shell=Setup();shell.Calculation.Rows.Single().DayHours="45";
        var detailsPath=Path.Combine(root,"BelgeBilgileri.json");File.WriteAllText(detailsPath,"{bozuk");
        Assert.Throws<InvalidDataException>(shell.Calculation.CreatePayment);
        Assert.False(File.Exists(new MonthlyPayrollStore(root).FilePath(ModuleId.TemelEgitim,2026,9)));
        Assert.Empty(new PaymentArchiveStore(root).ReadAll());
        File.Delete(detailsPath);shell.Calculation.CreatePayment();Assert.Single(shell.Outputs.Records);
    }
    [Theory] [InlineData("Gross")] [InlineData("EmployeePremium")] [InlineData("PremiumDays")]
    public void Corrupted_archived_calculations_are_rejected_without_rewriting_the_source(string field)
    {
        var shell=Setup();shell.Calculation.Rows[0].DayHours="117";shell.Calculation.CreatePayment();
        var path=shell.Outputs.Records.Single().Path;var json=JsonNode.Parse(File.ReadAllText(path))!;
        json["Data"]!["Rows"]![0]!["Result"]![field]=1;
        File.WriteAllText(path,json.ToJsonString());var bytes=File.ReadAllBytes(path);
        Assert.Throws<InvalidDataException>(()=>new PaymentArchiveStore(root).Read(path));
        Assert.Equal(bytes,File.ReadAllBytes(path));
    }
    [Fact] public void Overflowing_monthly_form_input_never_reaches_disk()
    {
        var shell=Setup();shell.Monthly.DayHours=decimal.MaxValue.ToString(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
        Assert.Null(shell.Monthly.Rows[0].Result);
        Assert.Throws<ArgumentException>(()=>shell.Monthly.Save(true));
        Assert.False(File.Exists(new MonthlyPayrollStore(root).FilePath(ModuleId.TemelEgitim,2026,9)));
    }
    [Fact] public void Overflowing_common_constants_are_reported_as_validation_without_changing_saved_values()
    {
        var store=new CommonConstantsStore(root);store.Save(PeriodValues.Source);
        var path=Path.Combine(root,"OrtakSabitler.json");var original=File.ReadAllBytes(path);
        Assert.Throws<ArgumentException>(()=>store.Save(PeriodValues.Source with{Coefficient=decimal.MaxValue}));
        Assert.Equal(original,File.ReadAllBytes(path));
    }
    [Theory] [InlineData("month")] [InlineData("year")] [InlineData("module")]
    public void Period_selection_cannot_discard_unsaved_hours_or_teacher_edits(string selection)
    {
        var shell=Setup();var workspace=shell.Calculation;workspace.Rows[0].DayHours="45";
        workspace.Rows[0].FullName="Kaydedilmemiş öğretmen";
        if(selection=="month")workspace.Month=10;
        if(selection=="year")workspace.Year=2027;
        if(selection=="module")workspace.SelectedModule=workspace.Modules.Single(m=>m.Id==ModuleId.Ortaogretim);
        Assert.Equal(2026,workspace.Year);Assert.Equal(9,workspace.Month);Assert.Equal(ModuleId.TemelEgitim,workspace.SelectedModule.Id);
        Assert.Equal("45",workspace.Rows[0].DayHours);Assert.Equal("Kaydedilmemiş öğretmen",workspace.Rows[0].FullName);
        Assert.Contains("kaydedilmemiş",workspace.Status,StringComparison.OrdinalIgnoreCase);
    }
    [Theory]
    [InlineData(2026,10,5,31,117,27,16,11)]
    [InlineData(2028,2,1,29,7.5,29,1,28)]
    [InlineData(2026,2,1,28,7.51,28,2,26)]
    [InlineData(2026,10,5,20,75,16,10,6)]
    [InlineData(2026,10,31,31,0.01,1,1,0)]
    public void Calendar_boundaries_follow_independently_checked_examples(int year,int month,int start,int end,decimal hours,int capacity,int days,int missing)
    {
        var result=TeacherCalendar.CalculateMonth(year,month,new(year,month,start),new(year,month,end),hours,0);
        Assert.Equal(new CalendarResult(capacity,days,missing),result);
    }
    [Fact] public void Eight_teachers_then_four_departures_and_six_arrivals_keep_both_months_independent()
    {
        var shell=Setup(8);
        foreach(var row in shell.Calculation.Rows)row.DayHours="75";
        shell.Calculation.CreatePayment();var september=shell.Outputs.Records.Single();var bytes=File.ReadAllBytes(september.Path);
        var directory=new TeacherDirectory(Catalog);var original=directory.Read().ToArray();
        foreach(var teacher in original.Take(4))directory.Update(teacher with{EndDate=new(2026,10,1)});
        for(var i=0;i<6;i++)directory.Save(new(Guid.NewGuid(),$"Yeni Öğretmen {i}","","TR330006100519786457841326","","2341.04",new(2026,10,5),null));
        shell=new ShellViewModel(Catalog);shell.Calculation.Month=10;
        Assert.All(shell.Calculation.Rows,r=>Assert.False(r.HasInput));
        foreach(var row in shell.Calculation.Rows.Where(r=>r.EndDate is null))row.DayHours="90";
        shell.Calculation.CreatePayment();var october=shell.Outputs.Records.Single(r=>r.Document.Data.Month==10).Document.Data;
        Assert.Equal(10,october.Rows.Length);Assert.All(october.Rows,r=>Assert.Equal(90,r.Input.DayHours));
        Assert.Equal(8,october.Movement!.Previous);Assert.Equal(6,october.Movement.Entered);Assert.Equal(4,october.Movement.Exited);
        Assert.Equal(bytes,File.ReadAllBytes(september.Path));
        shell=new ShellViewModel(Catalog);Assert.Equal(8,shell.Calculation.Rows.Count(r=>r.HasInput));
        shell.Calculation.Year=2027;shell.Calculation.Month=10;
        Assert.All(shell.Calculation.Rows,r=>Assert.False(r.HasInput));
        Assert.Equal(2,new PaymentArchiveStore(root).ReadAll().Count);
    }
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
}
