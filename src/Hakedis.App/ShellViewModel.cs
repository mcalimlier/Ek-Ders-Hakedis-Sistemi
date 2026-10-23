using System.ComponentModel;
using Hakedis.Contracts;

namespace Hakedis.App;

public sealed class ShellViewModel(string teacherPath) : INotifyPropertyChanged
{
    private string panel = "Entry";
    private TeacherRecord? teacher;
    public WorkspaceViewModel Workspace { get; } = new();
    public TeacherListViewModel Teachers { get; } = new(teacherPath);
    public bool EntryVisible => panel == "Entry";
    public bool CalculationVisible => panel == "Calculation";
    public bool TeachersVisible => panel == "Teachers";
    public string Title => panel switch { "Calculation" => "Hakediş Hesapla", "Teachers" => "Ücretli Öğretmen Listesi", _ => "Veri Girişi" };
    public TeacherRecord? CalculationTeacher
    {
        get => teacher;
        set { teacher = value; if (value is not null) Workspace.Person = value.FullName; PropertyChanged?.Invoke(this, new(nameof(CalculationTeacher))); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public void Navigate(string destination)
    {
        if (destination is not ("Entry" or "Calculation" or "Teachers")) throw new ArgumentException("Geçersiz panel.");
        panel = destination;
        foreach (var name in new[] { nameof(EntryVisible), nameof(CalculationVisible), nameof(TeachersVisible), nameof(Title) })
            PropertyChanged?.Invoke(this, new(name));
    }
    public void UseSelectedTeacher()
    {
        CalculationTeacher = Teachers.Selected ?? throw new InvalidOperationException("Önce öğretmen seçin."); Navigate("Entry");
    }
}
