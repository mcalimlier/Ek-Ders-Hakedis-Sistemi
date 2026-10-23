using System.ComponentModel;
using Hakedis.Contracts;
using Hakedis.Storage;

namespace Hakedis.App;

// Yeni kayıt formu yalnızca Veri Girişi ekranına bağlanır.
public sealed class TeacherRegistrationViewModel : INotifyPropertyChanged
{
    private TeacherDirectory directory;
    private readonly string path;
    public ModuleId Module { get; private set; }
    public TeacherEditor Editor { get; private set; } = new();
    public string Status { get; private set; } = "Öğretmen bilgilerini doldurun ve Kaydet düğmesine basın.";
    public event PropertyChangedEventHandler? PropertyChanged;
    public TeacherRegistrationViewModel(string path, ModuleId module = ModuleId.TemelEgitim)
    { this.path = path; Module = module; directory = new(path, module); }
    public void SwitchModule(ModuleId module)
    { directory = new(path, module); Module = module; Editor = new(); ShowStatus("Öğretmen kaydı seçili modüle yapılacaktır."); }
    public TeacherRecord SaveNew()
    {
        var record = Editor.ToRecord(Module) with { Id = Guid.NewGuid() };
        var rows = directory.Save(record); var saved = rows.Single(r => r.Id == record.Id);
        Editor = new(); ShowStatus($"{saved.FullName} öğretmen listesine kaydedildi."); return saved;
    }
    public void Reset() { Editor = new(); ShowStatus("Yeni öğretmen kaydı için bilgiler temizlendi."); }
    public void ShowStatus(string message)
    {
        Status = message; PropertyChanged?.Invoke(this, new(nameof(Editor))); PropertyChanged?.Invoke(this, new(nameof(Status)));
    }
}
