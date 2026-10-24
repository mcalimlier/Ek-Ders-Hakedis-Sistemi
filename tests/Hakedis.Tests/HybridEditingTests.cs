using Hakedis.App;
using Hakedis.Contracts;
using Hakedis.Storage;

namespace Hakedis.Tests;

public sealed class HybridEditingTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "HakedisTests", Guid.NewGuid().ToString("N"));
    private string Catalog => Path.Combine(root,"ogretmenler.json");
    private ShellViewModel Setup()
    {
        var shell=new ShellViewModel(Catalog);
        foreach(var name in new[]{"Bir","İki"})
        {shell.Registration.Editor.FullName=name;shell.Registration.Editor.OccupationCode="2341.04";shell.SaveNewTeacher();}
        return shell;
    }
    [Theory] [InlineData("null")] [InlineData("[]")]
    [InlineData("{\"SchemaVersion\":\"bozuk\"}")]
    [InlineData("{\"SchemaVersion\":3.5}")]
    public void Malformed_catalog_shape_is_reported_without_changing_the_file(string json)
    {
        Directory.CreateDirectory(root);File.WriteAllText(Catalog,json);
        Assert.Throws<InvalidDataException>(()=>new TeacherDirectory(Catalog).Read());
        Assert.Equal(json,File.ReadAllText(Catalog));
    }
    [Fact] public void Removed_fields_are_absent_from_the_active_contract()
    {
        var names=typeof(TeacherRecord).GetProperties().Select(p=>p.Name).ToArray();
        foreach(var removed in new[]{"InsuranceNumber","Branch","School2","Branch2"})Assert.DoesNotContain(removed,names);
    }
    [Fact] public void Cancel_preserves_existing_table_preview_and_disables_edit_mode()
    {
        var shell=Setup();var row=shell.Calculation.Rows[0];row.DayHours="45";
        shell.Calculation.SelectedHybridRow=row;shell.BeginHybridTeacherEdit();
        shell.Registration.Editor.FullName="Kaydedilmez";shell.CancelTeacherEdit();
        Assert.Equal("Bir",row.Teacher.FullName);Assert.Equal("45",row.DayHours);
        Assert.True(row.HasChanges);Assert.True(shell.CalculationVisible);Assert.False(shell.Registration.IsEditingRecord);
    }
    [Fact] public void Form_save_updates_every_view_but_preserves_monthly_preview()
    {
        var shell=Setup();var row=shell.Calculation.Rows[0];row.DayHours="45";
        shell.Calculation.SelectedHybridRow=row;shell.BeginHybridTeacherEdit();
        shell.Registration.Editor.FullName="Yeni ad";shell.SaveTeacherEdit();
        Assert.Equal("Yeni ad",shell.Calculation.Rows[0].Teacher.FullName);
        Assert.Equal("45",shell.Calculation.Rows[0].DayHours);
        Assert.Contains(shell.Teachers.Records,t=>t.FullName=="Yeni ad");
        Assert.Equal("Yeni ad",shell.Monthly.Rows[0].Teacher.FullName);
        Assert.True(shell.CalculationVisible);Assert.False(shell.Registration.IsEditingRecord);
        Assert.Equal("Öğretmeni Kaydet",shell.Registration.SaveButtonText);
    }
    [Fact] public void Hybrid_batch_rejects_all_changes_when_one_row_is_invalid()
    {
        var shell=Setup();var before=File.ReadAllBytes(Catalog);
        shell.Calculation.Rows[0].FullName="Değişti";shell.Calculation.Rows[0].DayHours="45";
        shell.Calculation.Rows[1].DayHours="hatalı";
        Assert.ThrowsAny<ArgumentException>(shell.Calculation.SaveHybridChanges);
        Assert.Equal(before,File.ReadAllBytes(Catalog));
        Assert.Empty(new MonthlyPayrollStore(root).Read(ModuleId.TemelEgitim,2026,9).Entries);
    }
    [Fact] public void Hybrid_batch_persists_both_teacher_and_hours_and_accepts_baseline()
    {
        var shell=Setup();var row=shell.Calculation.Rows[0];
        row.FullName="Değişti";row.DayHours="45";Assert.True(shell.Calculation.HasHybridChanges);
        shell.Calculation.SaveHybridChanges();Assert.False(shell.Calculation.HasHybridChanges);
        var reopened=new ShellViewModel(Catalog);Assert.Equal("Değişti",reopened.Calculation.Rows[0].Teacher.FullName);
        Assert.Equal("45",reopened.Calculation.Rows[0].DayHours);
        row.DayHours="90";row.DayHours="45";Assert.False(row.HasChanges);
    }
    [Fact] public void Stale_teacher_preview_cannot_overwrite_another_saved_edit()
    {
        var shell=Setup();var row=shell.Calculation.Rows[0];row.FullName="Eski ekran";
        new TeacherDirectory(Catalog).Update(row.SourceTeacher with {FullName="Dışarıda kaydedildi"});
        Assert.Throws<InvalidOperationException>(shell.Calculation.SaveHybridChanges);
        Assert.Equal("Dışarıda kaydedildi",new TeacherDirectory(Catalog).Read()[0].FullName);
    }
    [Fact] public void Legacy_catalog_migrates_without_removed_fields_and_keeps_original_backup()
    {
        var shell=Setup();var node=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Catalog))!;
        node["SchemaVersion"]=2;
        foreach(var item in node["Teachers"]!.AsArray())
            foreach(var key in new[]{"InsuranceNumber","Branch","School2","Branch2"})item![key]="eski değer";
        File.WriteAllText(Catalog,node.ToJsonString());var original=File.ReadAllBytes(Catalog);
        var store=new TeacherDirectory(Catalog);var teacher=store.Read()[0];store.Update(teacher with {FullName="Yeni"});
        Assert.Equal(original,File.ReadAllBytes(Catalog+".bak"));
        var saved=File.ReadAllText(Catalog);Assert.Contains("\"SchemaVersion\": 3",saved);
        foreach(var key in new[]{"InsuranceNumber","Branch","School2","Branch2"})Assert.DoesNotContain(key,saved);
        Assert.Equal(2,store.Read().Count);
    }
    [Fact] public void Duplicate_identity_in_batch_rejects_all_rows()
    {
        var shell=Setup();var before=File.ReadAllBytes(Catalog);
        foreach(var row in shell.Calculation.Rows)row.IdentityNumber="12345678901";
        Assert.Throws<ArgumentException>(shell.Calculation.SaveHybridChanges);
        Assert.Equal(before,File.ReadAllBytes(Catalog));
    }
    [Fact] public void Stale_monthly_input_does_not_overwrite_new_hours_or_master()
    {
        var shell=Setup();var row=shell.Calculation.Rows[0];row.FullName="Kaydedilmez";row.DayHours="45";
        var monthly=new MonthlyPayrollStore(root);
        monthly.Save(ModuleId.TemelEgitim,new(2026,9,PeriodValues.Source,[new(row.Teacher.Id,90,0,0)]));
        Assert.Throws<InvalidOperationException>(shell.Calculation.SaveHybridChanges);
        Assert.Equal("Bir",new TeacherDirectory(Catalog).Read()[0].FullName);
        Assert.Equal(90,monthly.Read(ModuleId.TemelEgitim,2026,9).Entries.Single().DayHours);
    }
    [Fact] public void Form_can_revert_an_unsaved_table_name_to_the_persisted_name()
    {
        var shell=Setup();var row=shell.Calculation.Rows[0];row.FullName="Tablo taslağı";
        shell.Calculation.SelectedHybridRow=row;shell.BeginHybridTeacherEdit();
        shell.Registration.Editor.FullName="Bir";shell.SaveTeacherEdit();
        Assert.Equal("Bir",row.FullName);Assert.False(row.HasChanges);
    }
    [Fact] public void Saving_monthly_hours_does_not_accept_unsaved_teacher_changes()
    {
        var shell=Setup();var row=shell.Monthly.Rows[0];shell.Monthly.SelectedTeacher=row.SourceTeacher;
        row.FullName="Tablo taslağı";row.DayHours="45";shell.Monthly.Save(true);
        Assert.True(row.HasChanges);Assert.Equal("Bir",row.SourceTeacher.FullName);
        Assert.Equal("Bir",new TeacherDirectory(Catalog).Read()[0].FullName);
    }
    [Fact] public void Hybrid_save_does_not_change_an_archived_payment()
    {
        var shell=Setup();var row=shell.Calculation.Rows[0];row.Iban="TR330006100519786457841326";row.DayHours="45";
        shell.Calculation.SaveHybridChanges();shell.Calculation.CreatePayment();
        var archive=Directory.GetFiles(new MonthlyPayrollStore(root).Folder(ModuleId.TemelEgitim),"hakediş-*.json").Single();
        var bytes=File.ReadAllBytes(archive);row.FullName="Yeni ad";row.DayHours="60";shell.Calculation.SaveHybridChanges();
        Assert.Equal(bytes,File.ReadAllBytes(archive));
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void Interrupted_file_batch_recovers_its_durable_decision(bool rollback)
    {
        Setup();var monthly=new MonthlyPayrollStore(root).FilePath(ModuleId.TemelEgitim,2026,9);
        Directory.CreateDirectory(Path.GetDirectoryName(monthly)!);
        var before=File.ReadAllBytes(Catalog);var after=before.Concat(new byte[]{32}).ToArray();
        var changes=new[]{new PayrollEditStore.FileChange(Catalog,before,after),new PayrollEditStore.FileChange(monthly,null,new byte[]{123,125})};
        File.WriteAllBytes(Catalog,after);
        File.WriteAllText(Path.Combine(root,"hibrit-islem.json"),System.Text.Json.JsonSerializer.Serialize(new PayrollEditStore.BatchJournal(rollback,changes)));
        new PayrollEditStore(Catalog).Recover();
        Assert.Equal(rollback?before:after,File.ReadAllBytes(Catalog));Assert.Equal(!rollback,File.Exists(monthly));
        Assert.False(File.Exists(Path.Combine(root,"hibrit-islem.json")));
    }
    [Fact] public void Failed_second_file_write_recovers_without_partial_teacher_update()
    {
        var shell=Setup();shell.Monthly.SelectedTeacher=shell.Monthly.Teachers[0];
        shell.Monthly.DayHours="30";shell.Monthly.Save(true);
        var monthly=new MonthlyPayrollStore(root).FilePath(ModuleId.TemelEgitim,2026,9);
        var beforeCatalog=File.ReadAllBytes(Catalog);var beforeMonth=File.ReadAllBytes(monthly);
        shell.Calculation.Rows[0].FullName="Kaydedilmez";shell.Calculation.Rows[0].DayHours="45";
        using(var block=new FileStream(monthly,FileMode.Open,FileAccess.Read,FileShare.Read))
            Assert.ThrowsAny<IOException>(shell.Calculation.SaveHybridChanges);
        new PayrollEditStore(Catalog).Recover();
        Assert.Equal(beforeCatalog,File.ReadAllBytes(Catalog));Assert.Equal(beforeMonth,File.ReadAllBytes(monthly));
        Assert.True(shell.Calculation.HasHybridChanges);
    }
    [Fact] public void Deleting_selected_hybrid_teacher_clears_selection_and_keeps_other_previews()
    {
        var shell=Setup();Assert.False(shell.Calculation.HasHybridSelection);
        var kept=shell.Calculation.Rows[1];kept.DayHours="45";
        shell.Calculation.SelectedHybridRow=shell.Calculation.Rows[0];shell.Calculation.DeleteHybridTeacher();
        Assert.False(shell.Calculation.HasHybridSelection);Assert.Same(kept,Assert.Single(shell.Calculation.Rows));
        Assert.Equal("45",kept.DayHours);Assert.Single(shell.Teachers.Records);Assert.Single(shell.Monthly.Rows);
    }
    [Fact] public void Next_monthly_save_finishes_pending_recovery_before_writing_new_hours()
    {
        var shell=Setup();shell.Monthly.SelectedTeacher=shell.Monthly.Teachers[0];
        shell.Monthly.DayHours="30";shell.Monthly.Save(true);
        var monthly=new MonthlyPayrollStore(root).FilePath(ModuleId.TemelEgitim,2026,9);
        var beforeMonth=File.ReadAllBytes(monthly);var beforeCatalog=File.ReadAllBytes(Catalog);
        shell.Monthly.DayHours="45";shell.Monthly.Save(true);var afterMonth=File.ReadAllBytes(monthly);
        var changes=new[]{new PayrollEditStore.FileChange(Catalog,beforeCatalog,beforeCatalog),new PayrollEditStore.FileChange(monthly,beforeMonth,afterMonth)};
        File.WriteAllText(Path.Combine(root,"hibrit-islem.json"),System.Text.Json.JsonSerializer.Serialize(new PayrollEditStore.BatchJournal(true,changes)));
        shell.Monthly.DayHours="60";Assert.Throws<InvalidOperationException>(()=>shell.Monthly.Save(true));
        Assert.Equal(30,new MonthlyPayrollStore(root).Read(ModuleId.TemelEgitim,2026,9).Entries.Single().DayHours);
        Assert.False(File.Exists(Path.Combine(root,"hibrit-islem.json")));
        shell.Monthly.Reload();shell.Monthly.SelectedTeacher=shell.Monthly.Teachers[0];
        shell.Monthly.DayHours="60";shell.Monthly.Save(true);new PayrollEditStore(Catalog).Recover();
        Assert.Equal(60,new MonthlyPayrollStore(root).Read(ModuleId.TemelEgitim,2026,9).Entries.Single().DayHours);
    }
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
}
