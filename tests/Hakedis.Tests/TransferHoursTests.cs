using Hakedis.App;
using Hakedis.Contracts;
using Hakedis.Storage;
using System.Text.Json;
namespace Hakedis.Tests;
public sealed class TransferHoursTests
{
    private static TeacherRecord Teacher()=>new(Guid.NewGuid(),"Deneme","10000000146","TR330006100519786457841326","İlk","2341.04",new(2026,9,1),null,ModuleId.TemelEgitim,
        new(ModuleId.TemelEgitim,"İlk",[new(new(2026,9,15),ModuleId.TemelEgitim,"İlk",ModuleId.DinOgretimi,"Yeni"),new(new(2026,11,10),ModuleId.DinOgretimi,"Yeni",ModuleId.TemelEgitim,"Son")],[]));
    [Fact] public void Split_hours_sum_preserve_blanks_and_follow_each_transfer_month()
    {
        var t=Teacher();var row=new PayrollRow(t,null,()=>new PeriodConstantsEditor(PeriodValues.Source).Parameters(),period:()=> (2026,9));
        Assert.True(row.HasSchoolHourSplit);row.PreviousSchoolDayHours="40";row.NewSchoolDayHours="";
        Assert.Equal(40,row.Read().DayHours);Assert.Null(row.Read().NewSchoolDayHours);Assert.NotEmpty(row.SchoolHoursWarning);
        row.NewSchoolDayHours="35";Assert.Equal(75,row.Read().DayHours);Assert.Empty(row.SchoolHoursWarning);
        var restored=new PayrollRow(t,row.Read(),()=>new PeriodConstantsEditor(PeriodValues.Source).Parameters(),period:()=> (2026,9));
        Assert.Equal("40",restored.PreviousSchoolDayHours);Assert.Equal("35",restored.NewSchoolDayHours);Assert.False(restored.HasChanges);
        Assert.False(new PayrollRow(t,null,()=>new PeriodConstantsEditor(PeriodValues.Source).Parameters(),period:()=> (2026,10)).HasSchoolHourSplit);
        var next=new PayrollRow(t,null,()=>new PeriodConstantsEditor(PeriodValues.Source).Parameters(),period:()=> (2026,11));
        Assert.True(next.HasSchoolHourSplit);Assert.Contains("Yeni",next.PreviousSchoolLabel);Assert.Contains("Son",next.NewSchoolLabel);
        row.PreviousSchoolDayHours="-1";Assert.Throws<ArgumentException>(()=>row.Read());
    }
    [Fact] public void Dates_write_clear_name_and_read_legacy_name()
    {
        var t=Teacher().Assignments!.Transfers[0];var json=JsonSerializer.Serialize(t);
        Assert.Contains("NewSchoolStartDate",json);Assert.DoesNotContain("\"Date\"",json);
        Assert.Equal(t,JsonSerializer.Deserialize<SchoolTransfer>(json.Replace("NewSchoolStartDate","Date")));
    }
    [Fact] public void Split_roundtrips_month_billing_and_archive_with_blank_warning()
    {
        var root=Path.Combine(Path.GetTempPath(),"TransferHours",Guid.NewGuid().ToString("N"));
        try
        {
            var path=Path.Combine(root,"teachers.json");var t=Teacher();new TeacherDirectory(path).Save(t);
            var w=new PeriodWorkspace(path);w.SelectedTeacher=w.Teachers.Single();w.PreviousSchoolDayHours="40";w.NewSchoolDayHours="";w.Save(false);
            var stored=new MonthlyPayrollStore(root).Read(ModuleId.TemelEgitim,2026,9).Entries.Single();Assert.True(stored.SchoolHoursSeparated);Assert.Null(stored.NewSchoolDayHours);
            var choice=new BillingChoiceViewModel(t,2026,9,stored,root);choice.SelectedModule=choice.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);
            Assert.Equal(stored,choice.ReadInput());w.AcceptBillingChoice(t,ModuleId.DinOgretimi,choice.ReadInput());
            w.SelectedModule=w.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);w.SelectedTeacher=w.Teachers.Single();
            Assert.Equal("40",w.PreviousSchoolDayHours);Assert.Equal("",w.NewSchoolDayHours);Assert.NotEmpty(w.SchoolHoursWarning);
            w.CreatePayment();var archive=new PaymentArchiveStore(root).ReadAll().Single();Assert.Equal(40,archive.Document.Data.Rows.Single().Input.DayHours);
            w.LoadArchive(archive);Assert.NotEmpty(w.Rows.Single().SchoolHoursWarning);Assert.Null(w.Rows.Single().Read().NewSchoolDayHours);
            Assert.Contains("NewSchoolStartDate",File.ReadAllText(archive.Path));
        }
        finally {if(Directory.Exists(root))Directory.Delete(root,true);}
    }
    [Fact] public void Both_blank_and_explicit_zero_remain_distinct_after_reload()
    {
        var t=Teacher();var p=new PeriodConstantsEditor(PeriodValues.Source);
        var row=new PayrollRow(t,new(t.Id,0,0,0,null,null,true),()=>p.Parameters(),period:()=> (2026,9));
        Assert.Contains("İlk",row.SchoolHoursWarning);Assert.Contains("Yeni",row.SchoolHoursWarning);
        row.PreviousSchoolDayHours="0";row.NewSchoolDayHours="0";Assert.Empty(row.SchoolHoursWarning);Assert.Equal(0,row.Read().DayHours);
        Assert.Throws<ArgumentException>(()=>new MonthlyInput(t.Id,1,0,0,2,3,true).ValidateSchoolHours());
    }
    [Fact] public void Adding_transfer_after_monthly_input_keeps_existing_total()
    {
        var transferred=Teacher();var t=transferred with {Assignments=null};
        var row=new PayrollRow(t,new(t.Id,75,0,0),()=>new PeriodConstantsEditor(PeriodValues.Source).Parameters(),period:()=> (2026,9));
        row.RefreshTeacher(transferred);Assert.Equal(75,row.Read().DayHours);Assert.Equal("75",row.PreviousSchoolDayHours);Assert.NotEmpty(row.SchoolHoursWarning);
        Assert.Throws<ArgumentException>(()=>new MonthlyInput(t.Id,75,0,0,null,null,true).ValidateSchoolHours());
    }
    [Fact] public void Same_module_and_first_day_transfers_still_show_split_only_in_their_month()
    {
        var t=Teacher();t=t with {Assignments=new(ModuleId.TemelEgitim,"İlk",[new(new(2026,9,1),ModuleId.TemelEgitim,"İlk",ModuleId.TemelEgitim,"Yeni")],[])};
        var row=new PayrollRow(t,null,()=>new PeriodConstantsEditor(PeriodValues.Source).Parameters(),period:()=> (2026,9));
        Assert.True(row.HasSchoolHourSplit);row.PreviousSchoolDayHours="0";row.NewSchoolDayHours="50";Assert.Equal(50,row.Read().DayHours);Assert.Empty(row.SchoolHoursWarning);
        var edited=t with {Assignments=new(ModuleId.TemelEgitim,"İlk",[new(new(2026,10,1),ModuleId.TemelEgitim,"İlk",ModuleId.TemelEgitim,"Yeni")],[])};
        row.RefreshTeacher(edited);Assert.False(row.HasSchoolHourSplit);Assert.Equal(50,row.Read().DayHours);
    }
}
