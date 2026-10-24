using System.ComponentModel;
using Hakedis.Contracts;

namespace Hakedis.App;

public sealed class ShellViewModel : INotifyPropertyChanged
{
    private Hakedis.Storage.ArchivedPayment? archiveEdit;
    private readonly Hakedis.Storage.PaymentArchiveStore archiveStore;
    public CommonConstantsViewModel CommonConstants {get;}
    public OutputsViewModel Outputs {get;}
    public bool CanUseEntry=>!Calculation.IsArchiveEditing;
    private string panel = "Entry";
    private TeacherRecord? teacher;
    public WorkspaceViewModel Workspace { get; } = new();
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
        CommonConstants=new(root);Outputs=new(root);archiveStore=new(root);
        Teachers = new(teacherPath, activeModule); Registration = new(teacherPath, activeModule);
        registrationModule = listModule = Workspace.Modules.Single(m=>m.Id==activeModule);
        Monthly = new(teacherPath); Calculation = new(teacherPath);
        Monthly.RowChanged += SyncRow; Calculation.RowChanged += SyncRow;
        Monthly.Saved += RefreshOther; Calculation.Saved += RefreshOther;

        Monthly.PropertyChanged += PeriodChanged; Calculation.PropertyChanged += PeriodChanged;
        Monthly.AdoptConstants(CommonConstants.Editor);Calculation.AdoptConstants(CommonConstants.Editor);
        Calculation.PaymentCreated+=Outputs.Reload;
        CommonConstants.Saved+=()=>{if(!Calculation.IsArchiveEditing)Calculation.AdoptConstants(CommonConstants.Editor);Monthly.AdoptConstants(CommonConstants.Editor);};
        Workspace.PropertyChanged += WorkspaceChanged;
    }
    private static bool SamePeriod(PeriodWorkspace a,PeriodWorkspace b)=>a.SelectedModule.Id==b.SelectedModule.Id && a.Year==b.Year && a.Month==b.Month;
    private PeriodWorkspace Other(PeriodWorkspace source)=>ReferenceEquals(source,Monthly)?Calculation:Monthly;
    private void SyncRow(PeriodWorkspace source,PayrollRow row) {if(source.IsArchiveEditing)return;var other=Other(source);if(!other.IsArchiveEditing && SamePeriod(source,other))other.ApplyPreview(row);}
    private void PeriodChanged(object? sender,PropertyChangedEventArgs e)
    {
        if(e.PropertyName==nameof(PeriodWorkspace.Constants) && sender is PeriodWorkspace source && !source.IsArchiveEditing)
            source.AdoptConstants(CommonConstants.Editor);
    }
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
    public string Title => panel switch { "Details" => "Kurum ve İmza Bilgileri", "Outputs" => "Çıktı", "Constants" => "Dönem sabitleri", "Calculation" => "Hakediş Hesapla", "Teachers" => "Ücretli Öğretmen Listesi", _ => "Veri Girişi" };
    public string Subtitle => panel switch
    {
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
        if(saved.ModuleId==activeModule)CalculationTeacher = saved;
        if(saved.ModuleId==Monthly.SelectedModule.Id)Monthly.SelectedTeacher=Monthly.Teachers.Single(t=>t.Id==saved.Id);
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
        var saved=Registration.SaveEdit();Teachers.Reload();
        foreach(var workspace in new[]{Monthly,Calculation})
        {workspace.Rows.FirstOrDefault(r=>r.Teacher.Id==saved.Id)?.RefreshTeacher(saved,true);workspace.RefreshTeachers();}
        if(teacher is not null)CalculationTeacher=Calculation.Rows.FirstOrDefault(r=>r.Teacher.Id==teacher.Id)?.Teacher;
        Navigate("Calculation");
    }
    public void CancelTeacherEdit(){Registration.Reset();Navigate("Calculation");}
    public void Navigate(string destination)
    {
        if (destination is not ("Entry" or "Calculation" or "Teachers" or "Constants" or "Outputs" or "Details")) throw new ArgumentException("Geçersiz panel.");
        if(Calculation.IsArchiveEditing && destination!="Calculation")throw new InvalidOperationException("Geçmiş düzenlemeyi Değiştir/Kaydet veya Vazgeç ile tamamlayın.");
        if(destination=="Outputs")Outputs.Reload();
        if(Registration.IsEditingRecord && destination!="Entry")Registration.Reset();
        panel = destination;
        foreach (var name in new[] { nameof(DetailsVisible),nameof(OutputsVisible),nameof(CanUseEntry),nameof(EntryVisible), nameof(CalculationVisible), nameof(TeachersVisible), nameof(ConstantsVisible), nameof(Title), nameof(Subtitle) })
            PropertyChanged?.Invoke(this, new(name));
    }
    public void ClearCurrentWork()
    {
        if(Calculation.IsArchiveEditing)throw new InvalidOperationException("Önce geçmiş düzenlemeden vazgeçin veya kaydedin.");
        Calculation.ClearWork();Monthly.ClearWork();Registration.Reset();
    }
    public void BeginArchiveEdit()
    {
        if(Calculation.HasActiveWork || Monthly.HasHybridChanges || Registration.IsEditingRecord || CommonConstants.IsEditing ||
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
    {archiveEdit=null;Calculation.ClearWork();Calculation.AdoptConstants(CommonConstants.Editor);Outputs.Reload();Navigate("Outputs");}
    public void UseSelectedTeacher()
    {
        var record = Teachers.Selected ?? throw new InvalidOperationException("Önce öğretmen seçin.");
        Monthly.SelectedModule=Monthly.Modules.Single(m=>m.Id==record.ModuleId);
        Monthly.RefreshTeachers();Monthly.SelectedTeacher=Monthly.Teachers.Single(t=>t.Id==record.Id);
        if(record.ModuleId==activeModule)CalculationTeacher=record;Navigate("Entry");
    }
}
