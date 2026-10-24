using Hakedis.App;
using Hakedis.Contracts;
using Hakedis.Storage;

namespace Hakedis.Tests;

public sealed class OutputsWorkflowTests : IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"HakedisTests",Guid.NewGuid().ToString("N"));
    private string PathName=>Path.Combine(root,"ogretmenler.json");
    private ShellViewModel Setup()
    {
        var s=new ShellViewModel(PathName);s.Registration.Editor.FullName="Deneme Öğretmeni";
        s.Registration.Editor.Iban="TR330006100519786457841326";s.Registration.Editor.OccupationCode="2341.04";
        s.SaveNewTeacher();s.Monthly.DayHours="117";return s;
    }
    [Fact] public void Second_payroll_for_same_module_month_is_rejected_before_any_files_change()
    {
        var s=Setup();s.Calculation.CreatePayment();
        var original=s.Outputs.Records.Single();var bytes=File.ReadAllBytes(original.Path);
        var monthly=new MonthlyPayrollStore(root).FilePath(ModuleId.TemelEgitim,2026,9);var input=File.ReadAllBytes(monthly);
        s.Calculation.Rows[0].DayHours="90";
        var error=Assert.Throws<InvalidOperationException>(s.Calculation.CreatePayment);
        Assert.Contains("kayıtlı hakediş",error.Message);
        Assert.Equal(bytes,File.ReadAllBytes(original.Path));Assert.Equal(input,File.ReadAllBytes(monthly));
        Assert.Single(new PaymentArchiveStore(root).ReadAll());
        s.Calculation.Month=10;s.Calculation.Rows[0].DayHours="90";s.Calculation.CreatePayment();
        Assert.Equal(2,new PaymentArchiveStore(root).ReadAll().Count);
    }
    [Fact] public async Task Concurrent_windows_cannot_create_duplicate_payrolls()
    {
        var first=Setup();var second=new ShellViewModel(PathName);second.Calculation.Rows[0].DayHours="90";
        using var gate=new ManualResetEventSlim();
        Task<bool> Create(ShellViewModel s)=>Task.Run(()=>{gate.Wait();try{s.Calculation.CreatePayment();return true;}catch(InvalidOperationException){return false;}});
        var a=Create(first);var b=Create(second);gate.Set();var results=await Task.WhenAll(a,b);
        Assert.Single(results,x=>x);Assert.Single(new PaymentArchiveStore(root).ReadAll());
    }
    [Fact] public void Period_locks_are_scoped_to_module_and_released_after_disposal()
    {
        var store=new PaymentArchiveStore(root);
        using(var first=store.BeginNewPeriod(ModuleId.TemelEgitim,2026,9))
        {Assert.Throws<InvalidOperationException>(()=>store.BeginNewPeriod(ModuleId.TemelEgitim,2026,9));using var other=store.BeginNewPeriod(ModuleId.Ortaogretim,2026,9);using var next=store.BeginNewPeriod(ModuleId.TemelEgitim,2026,10);}
        using var retry=store.BeginNewPeriod(ModuleId.TemelEgitim,2026,9);
    }
    [Fact] public void Xml_export_detects_an_archive_changed_while_its_review_window_is_open()
    {
        var s=Setup();s.Calculation.CreatePayment();var record=s.Outputs.Records.Single();
        var store=new SgkXmlStore(root);store.EnsureCurrent(record);
        File.AppendAllText(record.Path," ");Assert.Throws<InvalidOperationException>(()=>store.EnsureCurrent(record));
    }
    [Fact] public void Monthly_contract_has_no_extra_employment_date_fields()
    {
        var names=typeof(MonthlyInput).GetProperties().Select(p=>p.Name);
        Assert.DoesNotContain("MonthStart",names);Assert.DoesNotContain("MonthEnd",names);
    }
    [Fact] public void Common_constants_start_locked_and_do_not_rewrite_previous_records()
    {
        var s=Setup();s.Calculation.CreatePayment();s.Outputs.Reload();var archived=s.Outputs.Records.Single();
        var bytes=File.ReadAllBytes(archived.Path);var monthly=new MonthlyPayrollStore(root).FilePath(ModuleId.TemelEgitim,2026,9);var inputBytes=File.ReadAllBytes(monthly);
        Assert.True(s.CommonConstants.IsReadOnly);Assert.Equal("Güncelle",s.CommonConstants.ButtonText);
        s.CommonConstants.ToggleSave();Assert.False(s.CommonConstants.IsReadOnly);
        s.CommonConstants.Editor.Coefficient="2";Assert.Equal(32760,s.Calculation.Rows[0].Result!.Gross);
        s.CommonConstants.ToggleSave();Assert.True(s.CommonConstants.IsReadOnly);
        Assert.Equal(bytes,File.ReadAllBytes(archived.Path));Assert.Equal(inputBytes,File.ReadAllBytes(monthly));
        var reopened=new ShellViewModel(PathName);reopened.Calculation.Year=2027;
        Assert.Equal("2",reopened.Calculation.Constants.Coefficient);
        Assert.Equal(25806.89m,reopened.Outputs.Records.Single().Document.Data.Rows[0].Result.Gross);
    }
    [Fact] public void Active_work_blocks_archive_edit_and_clear_does_not_delete_monthly_data()
    {
        var s=Setup();s.Calculation.CreatePayment();s.Outputs.Reload();s.Outputs.Selected=s.Outputs.Records.Single();
        s.Calculation.Rows[0].DayHours="90";Assert.Throws<InvalidOperationException>(s.BeginArchiveEdit);
        s.ClearCurrentWork();s.BeginArchiveEdit();Assert.True(s.Calculation.IsArchiveEditing);
        Assert.Equal("117",s.Calculation.Rows[0].DayHours);
        Assert.Equal(117,new MonthlyPayrollStore(root).Read(ModuleId.TemelEgitim,2026,9).Entries.Single().DayHours);
    }
    [Fact] public void Archive_edit_cancel_and_unconfirmed_save_preserve_original_bytes()
    {
        var s=Setup();s.Calculation.CreatePayment();s.Outputs.Reload();s.Outputs.Selected=s.Outputs.Records.Single();
        var path=s.Outputs.Selected.Path;var bytes=File.ReadAllBytes(path);
        s.BeginArchiveEdit();s.Calculation.Rows[0].DayHours="90";
        Assert.False(s.SaveArchiveEdit(false));Assert.Equal(bytes,File.ReadAllBytes(path));
        s.CancelArchiveEdit();Assert.Equal(bytes,File.ReadAllBytes(path));Assert.True(s.OutputsVisible);
    }
    [Fact] public void Confirmed_archive_edit_uses_original_constants_and_only_updates_selected_archive()
    {
        var s=Setup();s.Calculation.CreatePayment();s.Outputs.Reload();s.Outputs.Selected=s.Outputs.Records.Single();
        var path=s.Outputs.Selected.Path;var bytes=File.ReadAllBytes(path);var catalog=File.ReadAllBytes(PathName);
        var monthly=new MonthlyPayrollStore(root).FilePath(ModuleId.TemelEgitim,2026,9);var inputBytes=File.ReadAllBytes(monthly);
        s.CommonConstants.ToggleSave();s.CommonConstants.Editor.Coefficient="2";s.CommonConstants.ToggleSave();
        s.BeginArchiveEdit();s.Calculation.Rows[0].DayHours="90";s.Calculation.Rows[0].FullName="Geçmişteki ad";
        Assert.Equal(19851.45m,s.Calculation.Rows[0].Result!.Gross);Assert.True(s.SaveArchiveEdit(true));
        Assert.Equal(bytes,File.ReadAllBytes(path+".bak"));Assert.Equal(catalog,File.ReadAllBytes(PathName));Assert.Equal(inputBytes,File.ReadAllBytes(monthly));
        Assert.Equal("Geçmişteki ad",s.Outputs.Records.Single().Document.Data.Rows[0].Teacher.FullName);
    }
    [Fact] public void Changed_archive_is_never_overwritten_by_a_stale_editor()
    {
        var s=Setup();s.Calculation.CreatePayment();s.Outputs.Reload();s.Outputs.Selected=s.Outputs.Records.Single();
        var path=s.Outputs.Selected.Path;s.BeginArchiveEdit();s.Calculation.Rows[0].DayHours="90";
        File.AppendAllText(path," ");var newer=File.ReadAllBytes(path);
        Assert.Throws<InvalidOperationException>(()=>s.SaveArchiveEdit(true));Assert.Equal(newer,File.ReadAllBytes(path));
    }
    [Fact] public void Old_json_date_keys_are_read_but_not_written_and_original_archive_is_unchanged()
    {
        var s=Setup();s.Calculation.CreatePayment();s.Outputs.Reload();var record=s.Outputs.Records.Single();
        var node=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(record.Path))!;
        var row=node["Data"]!["Rows"]![0]!;row["Input"]!["MonthStart"]="2026-09-14T00:00:00";row["Input"]!["MonthEnd"]=null;
        row["Teacher"]!["InsuranceNumber"]="eski";row["Teacher"]!["Branch"]="eski";
        File.WriteAllText(record.Path,node.ToJsonString());var bytes=File.ReadAllBytes(record.Path);
        var read=new PaymentArchiveStore(root).Read(record.Path);Assert.Single(read.Document.Data.Rows);Assert.Equal(bytes,File.ReadAllBytes(record.Path));
        var monthly=new MonthlyPayrollStore(root);var path=monthly.FilePath(ModuleId.TemelEgitim,2026,9);
        var old=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;old["Data"]!["Entries"]![0]!["MonthStart"]="2026-09-14T00:00:00";
        File.WriteAllText(path,old.ToJsonString());monthly.Update(ModuleId.TemelEgitim,2026,9,x=>x);
        Assert.DoesNotContain("MonthStart",File.ReadAllText(path));Assert.DoesNotContain("MonthEnd",File.ReadAllText(path));
    }
    [Fact] public void Archive_can_edit_a_teacher_no_longer_in_the_master_catalog()
    {
        var s=Setup();s.Calculation.CreatePayment();s.Outputs.Reload();s.Outputs.Selected=s.Outputs.Records.Single();
        new TeacherDirectory(PathName).Delete(s.Calculation.Rows[0].Teacher.Id);
        s.BeginArchiveEdit();s.Calculation.Rows[0].DayHours="90";Assert.True(s.SaveArchiveEdit(true));
        Assert.Empty(new TeacherDirectory(PathName).Read());
    }
    [Fact] public void All_modules_use_common_constants_and_invalid_save_stays_in_edit_mode()
    {
        var s=Setup();s.CommonConstants.ToggleSave();s.CommonConstants.Editor.Coefficient="x";
        Assert.Throws<ArgumentException>(s.CommonConstants.ToggleSave);Assert.False(s.CommonConstants.IsReadOnly);
        s.CommonConstants.Editor.Coefficient="2";s.CommonConstants.ToggleSave();
        s.Calculation.SelectedModule=s.Calculation.Modules.Single(m=>m.Id==ModuleId.Ortaogretim);
        Assert.Same(s.CommonConstants.Editor,s.Calculation.Constants);Assert.Same(s.Monthly.Constants,s.Calculation.Constants);
    }
    [Fact] public void Three_reports_reconcile_bank_and_payroll_without_recalculating_frozen_results()
    {
        var s=Setup();s.Calculation.CreatePayment();var data=s.Outputs.Records.Single().Document.Data;
        var reports=Hakedis.TemelEgitim.Core.PaymentReports.Build(data,new("Kurum","Banka","Görevli","Ünvan","Yetkili"));
        Assert.Equal(3,reports.Length);Assert.Equal("Bordro.pdf",reports[0].FileName);
        Assert.Equal("22.193,93",reports[1].Tables[0].Rows[0][4]);
        Assert.Equal(reports[0].Tables[1].Rows[0][13],reports[1].Tables[0].Rows[0][4]);
        Assert.Contains("Eylül-2026",string.Join(" ",reports[2].Information));
        Assert.DoesNotContain("#REF!",string.Join(" ",reports.SelectMany(x=>x.Information)));
    }
    [Fact] public void Personnel_entries_use_employment_dates_not_roster_differences()
    {
        var s=Setup();s.Calculation.CreatePayment();var rows=s.Outputs.Records.Single().Document.Data.Rows;
        var old=rows[0] with {Teacher=rows[0].Teacher with {StartDate=new DateTime(2026,8,1)}};
        var store=new PaymentArchiveStore(root);
        var september=store.Movement(ModuleId.TemelEgitim,2026,9,[old]);
        Assert.Equal(0,september.Entered);Assert.Equal(0,september.Exited);
        var joined=old with {Teacher=old.Teacher with {StartDate=new DateTime(2026,10,5),EndDate=new DateTime(2026,10,25)}};
        var october=store.Movement(ModuleId.TemelEgitim,2026,10,[joined]);
        Assert.Equal(1,october.Previous);Assert.Equal(1,october.Entered);Assert.Equal(1,october.Exited);
        var nextYear=store.Movement(ModuleId.TemelEgitim,2027,10,[joined]);
        Assert.Equal(0,nextYear.Entered);Assert.Equal(0,nextYear.Exited);
    }
    [Fact] public void Personnel_movement_crosses_year_and_counts_departures_outside_the_paid_roster()
    {
        var s=Setup();s.Calculation.Year=2026;s.Calculation.Month=12;s.Calculation.Rows[0].DayHours="117";
        s.Calculation.CreatePayment();
        var catalog=new TeacherDirectory(PathName);var person=catalog.Read().Single();
        catalog.Update(person with {EndDate=new DateTime(2027,1,25)});
        new TeacherDirectory(PathName,ModuleId.Ortaogretim).Save(person with {Id=Guid.NewGuid(),ModuleId=ModuleId.Ortaogretim,StartDate=new DateTime(2027,1,1),IdentityNumber=""});
        var movement=new PaymentArchiveStore(root).Movement(ModuleId.TemelEgitim,2027,1,[]);
        Assert.Equal(1,movement.Previous);Assert.Equal(0,movement.Entered);Assert.Equal(1,movement.Exited);
        Assert.Equal("employment-dates.v2",movement.RuleVersion);
    }
    [Fact] public void Recorded_personnel_movement_is_frozen_when_master_dates_change()
    {
        var s=Setup();s.Calculation.Rows[0].StartDate=new DateTime(2026,9,5);s.Calculation.SaveHybridChanges();s.Calculation.CreatePayment();
        var archived=s.Outputs.Records.Single();Assert.Equal(1,archived.Document.Data.Movement!.Entered);
        var catalog=new TeacherDirectory(PathName);var person=catalog.Read().Single();catalog.Update(person with {StartDate=new DateTime(2026,8,1)});
        var reopened=new PaymentArchiveStore(root).Read(archived.Path);
        Assert.Equal(1,reopened.Document.Data.Movement!.Entered);
    }
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
}
