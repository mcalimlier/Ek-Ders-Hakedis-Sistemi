using Hakedis.App;
using Hakedis.Contracts;
using Hakedis.Storage;

namespace Hakedis.Tests;

public sealed class SchoolHistoryTests : IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"EdhsHistory",Guid.NewGuid().ToString("N"));
    private string PathName=>Path.Combine(root,"ogretmenler.json");
    private TeacherRecord Teacher()=>new(Guid.NewGuid(),"Geçiş Deneme","10000000146","TR330006100519786457841326","İlk Okul","2341.04",new(2026,9,1),null);
    private TeacherRecord Transferred()
    {
        var t=Teacher();return t with {Assignments=new(ModuleId.TemelEgitim,t.School,[new(new(2026,9,25),ModuleId.TemelEgitim,t.School,ModuleId.DinOgretimi,"Yeni Okul")],[])};
    }
    [Fact] public void Normal_editor_hides_optional_fields_and_keeps_old_json_compatible()
    {
        var e=new TeacherEditor();Assert.False(e.HasSchoolTransfer);var t=Teacher();new TeacherDirectory(PathName).Save(t);
        Assert.Null(new TeacherDirectory(PathName).Read().Single().Assignments);
        Assert.DoesNotContain("Assignments",File.ReadAllText(PathName));
    }
    [Fact] public void One_catalog_person_is_projected_into_both_module_lists_without_duplicating_tc()
    {
        var t=Transferred();new TeacherDirectory(PathName).Save(t);
        var basic=new TeacherDirectory(PathName).Read().Single();var din=new TeacherDirectory(PathName,ModuleId.DinOgretimi).Read().Single();
        Assert.Equal(basic.Id,din.Id);Assert.Equal(basic.IdentityNumber,din.IdentityNumber);
        Assert.Equal("İlk Okul",basic.School);Assert.Equal("Yeni Okul",din.School);Assert.Equal(ModuleId.DinOgretimi,din.ModuleId);
        using var json=System.Text.Json.JsonDocument.Parse(File.ReadAllText(PathName));Assert.Equal(1,json.RootElement.GetProperty("Teachers").GetArrayLength());
        Assert.Equal(t,new TeacherDirectory(PathName).Read().Single());
    }
    [Fact] public void Routing_asks_only_for_transition_month_and_keeps_sgk_employment_dates()
    {
        var t=Transferred();Assert.Equal([ModuleId.TemelEgitim],TeacherRouting.Candidates(t,2026,8));
        Assert.Equal(2,TeacherRouting.Candidates(t,2026,9).Count);Assert.True(TeacherRouting.NeedsChoice(t,2026,9));
        Assert.Equal([ModuleId.DinOgretimi],TeacherRouting.Candidates(t,2026,10));Assert.False(TeacherRouting.NeedsChoice(t,2026,10));
        new TeacherDirectory(PathName).Save(t);var view=new TeacherDirectory(PathName,ModuleId.DinOgretimi).ReadForPeriod(2026,10).Single();
        Assert.Equal(new DateTime(2026,9,1),view.StartDate);Assert.Null(view.EndDate);
        Assert.Empty(new TeacherDirectory(PathName,ModuleId.TemelEgitim).ReadForPeriod(2026,10));
    }
    [Fact] public void Invalid_or_incomplete_checked_transfer_is_not_saved()
    {
        var e=TeacherEditor.From(Teacher());e.HasSchoolTransfer=true;e.TransferSchool="";e.TransferDate=null;
        Assert.Throws<ArgumentException>(()=>new TeacherDirectory(PathName).Save(e.ToRecord()));Assert.False(File.Exists(PathName));
    }
    [Fact] public void Projected_teacher_edit_preserves_home_module_and_updates_shared_person_details()
    {
        new TeacherDirectory(PathName).Save(Transferred());var store=new TeacherDirectory(PathName,ModuleId.DinOgretimi);var p=store.Read().Single();
        store.UpdateBatch([p],[p with {FullName="Düzeltilen Ad"}]);
        Assert.Equal("Düzeltilen Ad",new TeacherDirectory(PathName).Read().Single().FullName);
        Assert.Equal(ModuleId.TemelEgitim,new TeacherDirectory(PathName).Read().Single().ModuleId);
        Assert.Equal("Yeni Okul",store.Read().Single().School);
    }
    [Fact] public void Billing_choice_is_atomic_with_target_month_and_never_sums_duplicate_hours()
    {
        var t=Transferred();new TeacherDirectory(PathName).Save(t);var months=new MonthlyPayrollStore(root);
        months.Save(ModuleId.TemelEgitim,new(2026,9,PeriodValues.Source,[new(t.Id,117,0,0)]));
        months.Save(ModuleId.DinOgretimi,new(2026,9,PeriodValues.Source,[new(t.Id,117,0,0)]));
        new BillingChoiceStore(PathName).Save(t,2026,9,ModuleId.DinOgretimi,new(t.Id,117,0,0),PeriodValues.Source);
        Assert.Empty(new TeacherDirectory(PathName).ReadForPeriod(2026,9));
        var chosen=new TeacherDirectory(PathName,ModuleId.DinOgretimi).ReadForPeriod(2026,9).Single();
        Assert.False(TeacherRouting.NeedsChoice(chosen,2026,9));
        Assert.Equal(117,months.Read(ModuleId.DinOgretimi,2026,9).Entries.Single().DayHours);
        Assert.Equal(117,months.Read(ModuleId.TemelEgitim,2026,9).Entries.Single().DayHours);
        Assert.False(File.Exists(Path.Combine(root,"hibrit-islem.json")));
    }
    [Fact] public void Creation_requires_choice_then_only_chosen_module_can_pay_and_old_archives_remain_frozen()
    {
        var t=Transferred();new TeacherDirectory(PathName).Save(t);new MonthlyPayrollStore(root).Save(ModuleId.TemelEgitim,new(2026,9,PeriodValues.Source,[new(t.Id,117,0,0)]));
        var w=new PeriodWorkspace(PathName);Assert.Single(w.BillingChoicesNeeded);
        Assert.Throws<InvalidOperationException>(()=>w.CreatePayment());
        new BillingChoiceStore(PathName).Save(t,2026,9,ModuleId.DinOgretimi,new(t.Id,117,0,0),PeriodValues.Source);
        w.SelectedModule=w.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);w.CreatePayment();
        var record=new PaymentArchiveStore(root).ReadAll().Single();Assert.Equal(ModuleId.DinOgretimi,record.Document.ModuleId);
        var bytes=File.ReadAllBytes(record.Path);var current=new TeacherDirectory(PathName).Read().Single();
        Assert.Throws<InvalidOperationException>(()=>new BillingChoiceStore(PathName).Save(current,2026,9,ModuleId.TemelEgitim,new(t.Id,117,0,0),PeriodValues.Source));
        Assert.Equal(bytes,File.ReadAllBytes(record.Path));
    }
    [Fact] public void Optional_form_can_add_and_edit_history_without_changing_sgk_dates()
    {
        var t=Teacher();var e=TeacherEditor.From(t);e.HasSchoolTransfer=true;e.TransferDate=new(2026,9,25);
        e.TransferSchool="Yeni Okul";e.TransferModule=ModuleId.DinOgretimi;
        new TeacherDirectory(PathName).Save(e.ToRecord());
        var stored=new TeacherDirectory(PathName).Read().Single();Assert.Single(stored.Assignments!.Transfers);
        var reopened=TeacherEditor.From(stored);Assert.False(reopened.HasSchoolTransfer);Assert.True(reopened.HasAssignmentHistory);
        reopened.EditLastTransfer();reopened.TransferSchool="Düzeltilen Okul";
        new TeacherDirectory(PathName).UpdateBatch([stored],[reopened.ToRecord()]);
        var edited=new TeacherDirectory(PathName,ModuleId.DinOgretimi).Read().Single();Assert.Equal("Düzeltilen Okul",edited.School);
        Assert.Equal(t.StartDate,edited.StartDate);Assert.Null(edited.EndDate);Assert.Single(edited.Assignments!.Transfers);
    }
    [Fact] public void Later_return_to_home_module_does_not_rewrite_earlier_school_projection()
    {
        var t=Transferred();var h=t.Assignments!;
        t=t with {Assignments=new(h.HomeModuleId,h.HomeSchool,[..h.Transfers,new(new(2026,10,10),ModuleId.DinOgretimi,"Yeni Okul",ModuleId.TemelEgitim,"Dönüş Okulu")],[])};
        new TeacherDirectory(PathName).Save(t);
        var before=new TeacherDirectory(PathName).ReadForPeriod(2026,8).Single();Assert.Equal("İlk Okul",before.School);
        Assert.Equal("Dönüş Okulu",new TeacherDirectory(PathName).ReadForPeriod(2026,11).Single().School);
        new TeacherDirectory(PathName).UpdateBatch([before],[before with {FullName="Güncel Ad"}]);
        Assert.Equal("Dönüş Okulu",new TeacherDirectory(PathName).ReadForPeriod(2026,11).Single().School);
    }
    [Fact] public void Interrupted_billing_choice_recovers_catalog_and_target_month_together()
    {
        var t=Transferred();new TeacherDirectory(PathName).Save(t);var before=File.ReadAllBytes(PathName);
        new BillingChoiceStore(PathName).Save(t,2026,9,ModuleId.DinOgretimi,new(t.Id,117,0,0),PeriodValues.Source);
        var monthly=new MonthlyPayrollStore(root).FilePath(ModuleId.DinOgretimi,2026,9);
        var after=File.ReadAllBytes(PathName);var monthAfter=File.ReadAllBytes(monthly);
        File.WriteAllBytes(PathName,before);File.Delete(monthly);
        File.WriteAllBytes(Path.Combine(root,"hibrit-islem.json"),System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new PayrollEditStore.BatchJournal(false,
            [new(PathName,before,after),new(monthly,null,monthAfter)])));
        var shell=new ShellViewModel(PathName);
        Assert.Empty(new TeacherDirectory(PathName).ReadForPeriod(2026,9));
        Assert.Equal(117,new MonthlyPayrollStore(root).Read(ModuleId.DinOgretimi,2026,9).Entries.Single().DayHours);
        Assert.False(File.Exists(Path.Combine(root,"hibrit-islem.json")));
    }
    [Fact] public void Decision_dialog_requires_explicit_selection_and_never_sums_two_monthly_entries()
    {
        var t=Transferred();new TeacherDirectory(PathName).Save(t);
        var store=new MonthlyPayrollStore(root);foreach(var m in new[]{ModuleId.TemelEgitim,ModuleId.DinOgretimi})store.Save(m,new(2026,9,PeriodValues.Source,[new(t.Id,117,0,0)]));
        var vm=new BillingChoiceViewModel(t,2026,9,new(t.Id,117,0,0),root);Assert.False(vm.CanAccept);
        Assert.Throws<ArgumentException>(()=>vm.ReadInput());vm.SelectedModule=vm.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);
        Assert.Equal(117,vm.ReadInput().DayHours);Assert.Equal(2,vm.Modules.Count);
        vm.DayHours="-1";Assert.Throws<ArgumentException>(()=>vm.ReadInput());
    }
    [Fact] public void Shell_choice_preserves_other_teacher_previews_and_redirects_only_when_appropriate()
    {
        var t=Transferred();new TeacherDirectory(PathName).Save(t);
        var other=Teacher() with {Id=Guid.NewGuid(),IdentityNumber="10000000200"};new TeacherDirectory(PathName).Save(other);
        var shell=new ShellViewModel(PathName);shell.Calculation.Rows.Single(r=>r.Teacher.Id==t.Id).DayHours="117";
        shell.Calculation.Rows.Single(r=>r.Teacher.Id==other.Id).DayHours="90";
        var row=shell.Calculation.Rows.Single(r=>r.Teacher.Id==t.Id);shell.SaveBillingChoice(row,ModuleId.DinOgretimi,row.Read());
        Assert.Equal(ModuleId.TemelEgitim,shell.Calculation.SelectedModule.Id);
        Assert.Equal("90",Assert.Single(shell.Calculation.Rows).DayHours);Assert.True(shell.Calculation.HasHybridChanges);
        shell.Calculation.CreatePayment();shell.Calculation.SelectedModule=shell.Calculation.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);
        Assert.Equal("117",shell.Calculation.Rows.Single().DayHours);shell.Calculation.CreatePayment();Assert.Equal(2,shell.Outputs.Records.Count);
    }
    [Fact] public void Registering_a_transfer_before_selected_month_does_not_select_a_missing_home_row()
    {
        var shell=new ShellViewModel(PathName);var e=shell.Registration.Editor;e.FullName="Önceki Geçiş";e.IdentityNumber="10000000146";
        e.School="İlk Okul";e.HasSchoolTransfer=true;e.TransferDate=new(2026,8,25);e.TransferSchool="Yeni Okul";e.TransferModule=ModuleId.DinOgretimi;
        shell.SaveNewTeacher();Assert.Empty(shell.Monthly.Teachers);Assert.Null(shell.Monthly.SelectedTeacher);
        shell.Calculation.SelectedModule=shell.Calculation.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);Assert.Single(shell.Calculation.Teachers);
    }
    [Fact] public void Hybrid_profile_edit_in_destination_module_updates_person_without_legacy_module_error()
    {
        new TeacherDirectory(PathName).Save(Transferred());var shell=new ShellViewModel(PathName);
        shell.Calculation.SelectedModule=shell.Calculation.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);
        var row=shell.Calculation.Rows.Single();shell.Calculation.SelectedHybridRow=row;shell.BeginHybridTeacherEdit();
        shell.Registration.Editor.FullName="Güncellenen Geçiş";shell.SaveTeacherEdit();
        Assert.Equal("Güncellenen Geçiş",new TeacherDirectory(PathName).ReadCanonical().Single().FullName);
        Assert.Equal("Güncellenen Geçiş",shell.Calculation.Rows.Single().FullName);
    }
    [Fact] public void History_arrays_are_defensive_copies_and_roundtrip_keeps_value_equality()
    {
        var t=Transferred();var history=t.Assignments!;var exposed=history.Transfers;
        exposed[0]=exposed[0] with {ToSchool="Yanlışlıkla değişen kopya"};Assert.Equal("Yeni Okul",history.Transfers[0].ToSchool);
        new TeacherDirectory(PathName).Save(t);Assert.Equal(t,new TeacherDirectory(PathName).ReadCanonical().Single());
    }
    [Fact] public void Destination_module_is_explicit_not_guessed_from_the_school_name()
    {
        var e=TeacherEditor.From(Teacher());e.HasSchoolTransfer=true;e.TransferSchool="Yeni Okul";e.TransferDate=new(2026,9,25);
        Assert.Null(e.TransferModule);Assert.Throws<ArgumentException>(()=>new TeacherDirectory(PathName).Save(e.ToRecord()));
        Assert.False(File.Exists(PathName));
    }
    [Fact] public void Incomplete_transfer_with_pending_hours_shows_validation_before_changing_teacher_or_hours()
    {
        var t=Teacher();new TeacherDirectory(PathName).Save(t);var shell=new ShellViewModel(PathName);
        var row=shell.Calculation.Rows.Single();row.DayHours="117";shell.Calculation.SelectedHybridRow=row;shell.BeginHybridTeacherEdit();
        shell.Registration.Editor.HasSchoolTransfer=true;shell.Registration.Editor.TransferSchool="Yeni Okul";
        shell.Registration.Editor.TransferModule=ModuleId.DinOgretimi;
        Assert.Throws<ArgumentException>(()=>shell.SaveTeacherEdit());Assert.Equal("117",row.DayHours);
        Assert.Null(new TeacherDirectory(PathName).ReadCanonical().Single().Assignments);
    }
    [Fact] public void Saved_choice_can_change_only_after_confirmed_archive_deletion_without_splitting_total()
    {
        var t=Transferred();new TeacherDirectory(PathName).Save(t);var shell=new ShellViewModel(PathName);
        var row=shell.Calculation.Rows.Single();row.DayHours="117";
        shell.SaveBillingChoice(row,ModuleId.DinOgretimi,row.Read());shell.Calculation.CreatePayment();
        var archives=new PaymentArchiveStore(root);var paid=archives.ReadAll().Single();
        shell.Calculation.Reload();row=shell.Calculation.Rows.Single();
        Assert.Throws<InvalidOperationException>(()=>shell.SaveBillingChoice(row,ModuleId.TemelEgitim,row.Read()));
        Assert.False(archives.Delete(paid,false));Assert.True(File.Exists(paid.Path));
        Assert.True(archives.Delete(paid,true));
        shell.SaveBillingChoice(row,ModuleId.TemelEgitim,row.Read());
        Assert.Equal(ModuleId.TemelEgitim,shell.Calculation.SelectedModule.Id);
        Assert.Equal(117,shell.Calculation.Rows.Single().Read().DayHours);
        Assert.Empty(new TeacherDirectory(PathName,ModuleId.DinOgretimi).ReadForPeriod(2026,9));
        shell.Calculation.CreatePayment();Assert.Equal(ModuleId.TemelEgitim,archives.ReadAll().Single().Document.ModuleId);
    }
    [Fact] public void Saved_monthly_choice_remains_editable_only_for_selected_transfer_month()
    {
        new TeacherDirectory(PathName).Save(Transferred());var shell=new ShellViewModel(PathName);
        bool Visible()=>shell.Calculation.CanChooseBillingModule;
        Assert.False(Visible());var row=shell.Calculation.Rows.Single();row.DayHours="117";
        shell.Calculation.SelectedHybridRow=row;Assert.True(Visible());
        shell.SaveBillingChoice(row,ModuleId.DinOgretimi,row.Read());
        shell.Calculation.SelectedHybridRow=shell.Calculation.Rows.Single();Assert.True(Visible());
        shell.Calculation.CreatePayment();shell.Calculation.LoadArchive(new PaymentArchiveStore(root).ReadAll().Single());
        shell.Calculation.SelectedHybridRow=shell.Calculation.Rows.Single();Assert.False(Visible());
    }
    [Fact] public void Payment_choice_action_is_hidden_for_normal_teacher_or_single_module_month()
    {
        new TeacherDirectory(PathName).Save(Teacher());var w=new PeriodWorkspace(PathName);
        w.SelectedHybridRow=w.Rows.Single();w.Rows.Single().DayHours="117";Assert.False(w.CanChooseBillingModule);
        new TeacherDirectory(PathName).Save(Transferred() with {IdentityNumber="10000000200"});w.Reload();
        w.SelectedHybridRow=w.Rows.Single(r=>r.Teacher.Assignments is not null);Assert.False(w.CanChooseBillingModule);
        w.SelectedHybridRow.DayHours="117";Assert.True(w.CanChooseBillingModule);
        w.SaveHybridChanges();w.Month=10;w.SelectedModule=w.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);
        w.SelectedHybridRow=w.Rows.Single();w.SelectedHybridRow.DayHours="117";Assert.False(w.CanChooseBillingModule);
    }
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
}
