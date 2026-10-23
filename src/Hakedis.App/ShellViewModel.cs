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
    public ShellViewModel(string teacherPath)
    {
        Teachers = new(teacherPath, activeModule); Registration = new(teacherPath, activeModule);
        Workspace.PropertyChanged += WorkspaceChanged;
    }
    private void WorkspaceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Workspace.SelectedModule)) return;
        var next = Workspace.SelectedModule?.Id ?? ModuleId.TemelEgitim;
        if (next == activeModule) return;
        activeModule = next; teacher = null; Workspace.ResetTeacherInputsForModule();
        Registration.SwitchModule(next); Teachers.SwitchModule(next);
        PropertyChanged?.Invoke(this, new(nameof(CalculationTeacher)));
    }
    public bool EntryVisible => panel == "Entry";
    public bool CalculationVisible => panel == "Calculation";
    public bool TeachersVisible => panel == "Teachers";
    public string Title => panel switch { "Calculation" => "Hakediş Hesapla", "Teachers" => "Ücretli Öğretmen Listesi", _ => "Veri Girişi" };
    public string Subtitle => panel switch
    {
        "Calculation" => "Aylık ders ücretini hesaplayın; tahakkuk ve kesinti ayrıntılarını kontrol edin.",
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
        var saved = Registration.SaveNew(); Teachers.Reload();
        CalculationTeacher = Teachers.Records.Single(t => t.Id == saved.Id);
    }
    public void Navigate(string destination)
    {
        if (destination is not ("Entry" or "Calculation" or "Teachers")) throw new ArgumentException("Geçersiz panel.");
        panel = destination;
        foreach (var name in new[] { nameof(EntryVisible), nameof(CalculationVisible), nameof(TeachersVisible), nameof(Title), nameof(Subtitle) })
            PropertyChanged?.Invoke(this, new(name));
    }
    public void UseSelectedTeacher()
    {
        CalculationTeacher = Teachers.Selected ?? throw new InvalidOperationException("Önce öğretmen seçin."); Navigate("Entry");
    }
}
