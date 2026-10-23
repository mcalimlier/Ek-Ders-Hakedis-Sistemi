using System.ComponentModel;
using Hakedis.Contracts;

namespace Hakedis.App;

public sealed class ShellViewModel : INotifyPropertyChanged
{
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
        set { if(value is null || value.Id == registrationModule.Id)return;registrationModule=value;Registration.SwitchModule(value.Id);PropertyChanged?.Invoke(this,new(nameof(RegistrationModule))); }
    }
    public ModuleDescriptor ListModule
    {
        get => listModule;
        set { if(value is null || value.Id == listModule.Id)return;listModule=value;Teachers.SwitchModule(value.Id);PropertyChanged?.Invoke(this,new(nameof(ListModule))); }
    }
    public ShellViewModel(string teacherPath)
    {
        Teachers = new(teacherPath, activeModule); Registration = new(teacherPath, activeModule);
        registrationModule = listModule = Workspace.Modules.Single(m=>m.Id==activeModule);
        Monthly = new(teacherPath); Calculation = new(teacherPath);
        Monthly.RowChanged += SyncRow; Calculation.RowChanged += SyncRow;
        Monthly.Saved += RefreshOther; Calculation.Saved += RefreshOther;
        Monthly.ConstantsChanged += SyncConstants; Calculation.ConstantsChanged += SyncConstants;
        Monthly.PropertyChanged += PeriodChanged; Calculation.PropertyChanged += PeriodChanged;
        Calculation.AdoptConstants(Monthly.Constants);
        Workspace.PropertyChanged += WorkspaceChanged;
    }
    private static bool SamePeriod(PeriodWorkspace a,PeriodWorkspace b)=>a.SelectedModule.Id==b.SelectedModule.Id && a.Year==b.Year && a.Month==b.Month;
    private PeriodWorkspace Other(PeriodWorkspace source)=>ReferenceEquals(source,Monthly)?Calculation:Monthly;
    private void SyncRow(PeriodWorkspace source,PayrollRow row) {var other=Other(source);if(SamePeriod(source,other))other.ApplyPreview(row);}
    private void SyncConstants(PeriodWorkspace source) {var other=Other(source);if(SamePeriod(source,other))other.AdoptConstants(source.Constants);}
    private void PeriodChanged(object? sender,PropertyChangedEventArgs e)
    {if(e.PropertyName==nameof(PeriodWorkspace.Constants) && sender is PeriodWorkspace source)SyncConstants(source);}
    private void RefreshOther(PeriodWorkspace source)
    {var other=Other(source);if(SamePeriod(source,other)) {other.RefreshTeachers();other.AcceptSavedSelection();}}
    private void WorkspaceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Workspace.SelectedModule)) return;
        var next = Workspace.SelectedModule?.Id ?? ModuleId.TemelEgitim;
        if (next == activeModule) return;
        activeModule = next; teacher = null; Workspace.ResetTeacherInputsForModule();
        RegistrationModule=Workspace.Modules.Single(m=>m.Id==next); ListModule=RegistrationModule;
        PropertyChanged?.Invoke(this, new(nameof(CalculationTeacher)));
    }
    public bool EntryVisible => panel == "Entry";
    public bool CalculationVisible => panel == "Calculation";
    public bool TeachersVisible => panel == "Teachers";
    public bool ConstantsVisible => panel == "Constants";
    public string Title => panel switch { "Constants" => "Dönem sabitleri", "Calculation" => "Hakediş Hesapla", "Teachers" => "Ücretli Öğretmen Listesi", _ => "Veri Girişi" };
    public string Subtitle => panel switch
    {
        "Constants" => "Seçili modül ve dönemin katsayı, gösterge ve kesinti değerlerini düzenleyin.",
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
    public void Navigate(string destination)
    {
        if (destination is not ("Entry" or "Calculation" or "Teachers" or "Constants")) throw new ArgumentException("Geçersiz panel.");
        panel = destination;
        foreach (var name in new[] { nameof(EntryVisible), nameof(CalculationVisible), nameof(TeachersVisible), nameof(ConstantsVisible), nameof(Title), nameof(Subtitle) })
            PropertyChanged?.Invoke(this, new(name));
    }
    public void UseSelectedTeacher()
    {
        var record = Teachers.Selected ?? throw new InvalidOperationException("Önce öğretmen seçin.");
        Monthly.SelectedModule=Monthly.Modules.Single(m=>m.Id==record.ModuleId);
        Monthly.RefreshTeachers();Monthly.SelectedTeacher=Monthly.Teachers.Single(t=>t.Id==record.Id);
        if(record.ModuleId==activeModule)CalculationTeacher=record;Navigate("Entry");
    }
}
