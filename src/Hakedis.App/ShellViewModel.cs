using System.ComponentModel;
using Hakedis.Contracts;
using Hakedis.Storage;

namespace Hakedis.App;

public sealed class ShellViewModel : INotifyPropertyChanged
{
    private Hakedis.Storage.ArchivedPayment? archiveEdit;
    private readonly Hakedis.Storage.PaymentArchiveStore archiveStore;
    public CommonConstantsViewModel CommonConstants {get;}
    public CommonConstantsViewModel RetiredConstants {get;}
    public OutputsViewModel Outputs {get;}
    public PersonalPayrollViewModel Personal {get;}
    public bool CanUseEntry=>!Calculation.IsArchiveEditing;
    private string panel = "Entry";
    private TeacherRecord? teacher;
    public WorkspaceViewModel Workspace { get; } = new();
    public UiSettingsViewModel Settings {get;}
    private ModuleId activeModule = ModuleId.TemelEgitim;
    public TeacherListViewModel Teachers { get; }
    public TeacherRegistrationViewModel Registration { get; }
    public PeriodWorkspace Monthly { get; }
    public PeriodWorkspace Calculation { get; }
    private ModuleDescriptor registrationModule;
    private ModuleDescriptor listModule;
    public ModuleDescriptor RegistrationModule
    {
        get => registrationModule;
        set { if(value is null || value.Id == registrationModule.Id)return;Registration.SwitchModule(value.Id);registrationModule=value;PropertyChanged?.Invoke(this,new(nameof(RegistrationModule))); }
    }
    public ModuleDescriptor ListModule
    {
        get => listModule;
        set { if(value is null || value.Id == listModule.Id)return;listModule=value;Teachers.SwitchModule(value.Id);PropertyChanged?.Invoke(this,new(nameof(ListModule))); }
    }
    public ShellViewModel(string teacherPath)
    {
        new Hakedis.Storage.PayrollEditStore(teacherPath).Recover();
        var root=System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(teacherPath))!;
        Settings=new(root);CommonConstants=new(root);RetiredConstants=new(root,ModuleId.HalkEgitimEmekliUstaOgretici);Outputs=new(root);Personal=new(root);archiveStore=new(root);
        Teachers = new(teacherPath, activeModule); Registration = new(teacherPath, activeModule);
        registrationModule = listModule = Workspace.Modules.Single(m=>m.Id==activeModule);
        Monthly = new(teacherPath); Calculation = new(teacherPath);
        Monthly.RowChanged += SyncRow; Calculation.RowChanged += SyncRow;
        Monthly.Saved += RefreshOther; Calculation.Saved += RefreshOther;

        Monthly.PropertyChanged += PeriodChanged; Calculation.PropertyChanged += PeriodChanged;
        Monthly.AdoptConstants(CommonConstants.Editor);Calculation.AdoptConstants(CommonConstants.Editor);
        Calculation.PaymentCreated+=Outputs.Reload;
        CommonConstants.Saved+=ApplyConstantEditors;RetiredConstants.Saved+=ApplyConstantEditors;
        Workspace.PropertyChanged += WorkspaceChanged;
    }
    private static bool SamePeriod(PeriodWorkspace a,PeriodWorkspace b)=>a.SelectedModule.Id==b.SelectedModule.Id && a.Year==b.Year && a.Month==b.Month;
    private PeriodWorkspace Other(PeriodWorkspace source)=>ReferenceEquals(source,Monthly)?Calculation:Monthly;
    private void SyncRow(PeriodWorkspace source,PayrollRow row) {if(source.IsArchiveEditing)return;var other=Other(source);if(!other.IsArchiveEditing && SamePeriod(source,other))other.ApplyPreview(row);}
    private void PeriodChanged(object? sender,PropertyChangedEventArgs e)
    {
        if(e.PropertyName==nameof(PeriodWorkspace.Constants) && sender is PeriodWorkspace source && !source.IsArchiveEditing)
            source.AdoptConstants(EditorFor(source));
    }
    private PeriodConstantsEditor EditorFor(PeriodWorkspace workspace)=>workspace.SelectedModule.Id==ModuleId.HalkEgitimEmekliUstaOgretici?RetiredConstants.Editor:CommonConstants.Editor;
    private void ApplyConstantEditors(){foreach(var w in new[]{Monthly,Calculation})if(!w.IsArchiveEditing)w.AdoptConstants(EditorFor(w));}
    private void RefreshOther(PeriodWorkspace source)
    {Teachers.Reload();var other=Other(source);other.RefreshTeachers();if(SamePeriod(source,other))other.AcceptSavedSelection();}
    private void WorkspaceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Workspace.SelectedModule)) return;
        var next = Workspace.SelectedModule?.Id ?? ModuleId.TemelEgitim;
        if (next == activeModule) return;
        activeModule = next; teacher = null; Workspace.ResetTeacherInputsForModule();
        RegistrationModule=Workspace.Modules.Single(m=>m.Id==next); ListModule=RegistrationModule;
        PropertyChanged?.Invoke(this, new(nameof(CalculationTeacher)));
    }
    public bool DetailsVisible=>panel=="Details";
    public bool OutputsVisible=>panel=="Outputs";
    public bool EntryVisible => panel == "Entry";
    public bool CalculationVisible => panel == "Calculation";
    public bool TeachersVisible => panel == "Teachers";
    public bool ConstantsVisible => panel == "Constants";
    public bool PersonalVisible=>panel=="Personal";
    public bool SettingsVisible=>panel=="Settings";
    public string Title => panel switch { "Settings"=>"Ayarlar", "Personal"=>"Kişisel Bordro", "Details" => "Kurum ve İmza Bilgileri", "Outputs" => "Çıktı", "Constants" => "Dönem sabitleri", "Calculation" => "Hakediş Hesapla", "Teachers" => "Öğretmen Listesi", _ => "Veri Girişi" };
    public string Subtitle => panel switch
    {
        "Settings"=>"EDHS pencere boyutunu ekranınıza göre ayarlayın.",
        "Personal"=>"Modül ve dönemi seçin; kayıtlı hakedişten bir öğretmenin kişisel bordrosunu hazırlayın.",
        "Details" => "Çıktılarda kullanılacak kurum, banka ve imza bilgilerini düzenleyin.",
        "Outputs" => "Kayıtlı hakedişleri görüntüleyin, üç belgeyi PDF oluşturun veya yazdırın.",
        "Constants" => "Tüm modüllerdeki aktif ve sonraki hesaplamalar için ortak sabitleri yönetin.",
        "Calculation" => "Modül ve dönemi seçin; aylık ders bilgilerini tabloda kontrol ederek hakedişi oluşturun.",
        "Teachers" => "Kayıtlı öğretmen bilgilerini görüntüleyin, düzenleyin ve yönetin.",
        _ => "Öğretmen kaydını oluşturun, ödeme dönemini ve ders bilgilerini hazırlayın."
    };
    public TeacherRecord? CalculationTeacher
    {
        get => teacher;
        set
        {
            if (value is not null && value.ModuleId != activeModule) throw new InvalidOperationException("Başka modülün öğretmeni bu hesapta kullanılamaz.");
            teacher = value; if (value is not null) Workspace.Person = value.FullName;
            PropertyChanged?.Invoke(this, new(nameof(CalculationTeacher)));
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public void SaveNewTeacher()
    {
        var saved = Registration.SaveNew(); Teachers.Reload();Monthly.RefreshTeachers();Calculation.RefreshTeachers();
        if(saved.ModuleId==activeModule && TeacherRouting.Included(saved,activeModule,Calculation.Year,Calculation.Month))CalculationTeacher = saved;
        if(saved.ModuleId==Monthly.SelectedModule.Id && Monthly.Teachers.SingleOrDefault(t=>t.Id==saved.Id) is { } selected)Monthly.SelectedTeacher=selected;
    }
    public void BeginHybridTeacherEdit()
    {
        var row=Calculation.SelectedHybridRow??throw new InvalidOperationException("Önce öğretmen seçin.");
        BeginTeacherEdit(row.Teacher,row.SourceTeacher);
    }
    private void BeginTeacherEdit(TeacherRecord record,TeacherRecord source)
    {
        if(Registration.IsEditingRecord)Registration.Reset();
        RegistrationModule=Workspace.Modules.Single(m=>m.Id==record.ModuleId);
        Registration.BeginEdit(record,source);Navigate("Entry");
    }
    public void SaveTeacherEdit()
    {
        var preview=TeacherDirectory.Normalize(Registration.Editor.ToRecord(Registration.Module));
        foreach(var workspace in new[]{Monthly,Calculation})
            if(workspace.Rows.Any(r=>r.Teacher.Id==preview.Id && r.HasChanges) && !TeacherRouting.Included(preview,workspace.SelectedModule.Id,workspace.Year,workspace.Month))
                throw new InvalidOperationException("Görev değişikliği bu öğretmeni mevcut dönem listesinden çıkarıyor. Önce aylık ders değişikliklerini kaydedin.");
        var saved=Registration.SaveEdit();Teachers.Reload();
        foreach(var workspace in new[]{Monthly,Calculation})
        {
            if(TeacherRouting.Modules(saved).Contains(workspace.SelectedModule.Id))
                workspace.Rows.FirstOrDefault(r=>r.Teacher.Id==saved.Id)?.RefreshTeacher(TeacherRouting.Project(saved,workspace.SelectedModule.Id),true);
            workspace.RefreshTeachers();
        }
        if(teacher is not null)
        {
            var updated=Calculation.Rows.FirstOrDefault(r=>r.Teacher.Id==teacher.Id)?.Teacher;
            CalculationTeacher=updated?.ModuleId==activeModule?updated:null;
        }
        Navigate("Calculation");
    }
    public void SaveBillingChoice(PayrollRow row,ModuleId module,MonthlyInput total)
    {
        if(!Calculation.Rows.Contains(row) || Calculation.IsArchiveEditing)throw new InvalidOperationException("Geçerli bir aktif bordro satırı seçin.");
        if(row.Teacher!=row.SourceTeacher)throw new InvalidOperationException("Önce öğretmen bilgileri için Değişiklikleri kaydet düğmesini kullanın.");
        var destinationVisible=Monthly.SelectedModule.Id==module && Monthly.Year==Calculation.Year && Monthly.Month==Calculation.Month;
        if(destinationVisible && module!=Calculation.SelectedModule.Id && Monthly.Rows.Any(r=>r.Teacher.Id==row.Teacher.Id && r.HasChanges))
            throw new InvalidOperationException("Aylık giriş ekranında bu kişinin kaydedilmemiş saatleri var. Önce o girişleri kaydedin.");
        Calculation.AcceptBillingChoice(row.SourceTeacher,module,total);Teachers.Reload();
        if(destinationVisible)
        {
            Monthly.RefreshTeachers();var target=Monthly.Rows.SingleOrDefault(r=>r.Teacher.Id==row.Teacher.Id);
            if(target is not null){target.Apply(total);target.AcceptChanges(total);Monthly.AcceptSavedSelection();}
        }
        if(module!=Calculation.SelectedModule.Id && !Calculation.Rows.Any(r=>r.HasInput || r.HasChanges))
            Calculation.SelectedModule=Calculation.Modules.Single(m=>m.Id==module);
    }
    public void CancelTeacherEdit(){Registration.Reset();Navigate("Calculation");}
    public void Navigate(string destination)
    {
        if (destination is not ("Settings" or "Entry" or "Calculation" or "Teachers" or "Constants" or "Outputs" or "Details" or "Personal")) throw new ArgumentException("Geçersiz panel.");
        if(Calculation.IsArchiveEditing && destination!="Calculation")throw new InvalidOperationException("Geçmiş düzenlemeyi Değiştir/Kaydet veya Vazgeç ile tamamlayın.");
        if(destination=="Outputs")Outputs.Reload();
        if(destination=="Personal")Personal.Reload();
        if(Registration.IsEditingRecord && destination!="Entry")Registration.Reset();
        panel = destination;
        foreach (var name in new[] { nameof(SettingsVisible),nameof(PersonalVisible),nameof(DetailsVisible),nameof(OutputsVisible),nameof(CanUseEntry),nameof(EntryVisible), nameof(CalculationVisible), nameof(TeachersVisible), nameof(ConstantsVisible), nameof(Title), nameof(Subtitle) })
            PropertyChanged?.Invoke(this, new(name));
    }
    public void ClearCurrentWork()
    {
        if(Calculation.IsArchiveEditing)throw new InvalidOperationException("Önce geçmiş düzenlemeden vazgeçin veya kaydedin.");
        Calculation.ClearWork();Monthly.ClearWork();Registration.Reset();
    }
    public void BeginArchiveEdit()
    {
        if(Calculation.HasActiveWork || Monthly.HasHybridChanges || Registration.IsEditingRecord || CommonConstants.IsEditing || RetiredConstants.IsEditing ||
           Registration.HasDraft)
            throw new InvalidOperationException("Aktif bir çalışma veya veri girişi var. Önce hakedişi oluşturun ya da Çalışmayı temizle düğmesini kullanın.");
        var record=Outputs.ReadSelected();archiveEdit=record;Calculation.LoadArchive(record);Navigate("Calculation");
    }
    public bool SaveArchiveEdit(bool confirmed)
    {
        if(!confirmed)return false;
        var original=archiveEdit??throw new InvalidOperationException("Düzenlenen hakediş bulunamadı.");
        archiveStore.Replace(original,Calculation.EditedSnapshot(original.Document.Data),true);
        EndArchiveEdit();Outputs.ShowStatus("Geçmiş hakediş güncellendi. Önceki sürüm yedek olarak korundu.");return true;
    }
    public void CancelArchiveEdit(){if(archiveEdit is null)return;EndArchiveEdit();Outputs.ShowStatus("Vazgeçildi. Eski hakediş kaydı değiştirilmedi.");}
    private void EndArchiveEdit()
    {archiveEdit=null;Calculation.ClearWork();Calculation.AdoptConstants(EditorFor(Calculation));Outputs.Reload();Navigate("Outputs");}
    public void UseSelectedTeacher()
    {
        var record = Teachers.Selected ?? throw new InvalidOperationException("Önce öğretmen seçin.");
        Monthly.SelectedModule=Monthly.Modules.Single(m=>m.Id==record.ModuleId);
        Monthly.RefreshTeachers();Monthly.SelectedTeacher=Monthly.Teachers.Single(t=>t.Id==record.Id);
        if(record.ModuleId==activeModule)CalculationTeacher=record;Navigate("Entry");
    }
}
