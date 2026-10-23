using System.ComponentModel;
using Hakedis.Contracts;
using Hakedis.Storage;

namespace Hakedis.App;

// Yeni kayıt formu yalnızca Veri Girişi ekranına bağlanır.
public sealed class TeacherRegistrationViewModel(string path) : INotifyPropertyChanged
{
    private readonly TeacherDirectory directory = new(path);
    public TeacherEditor Editor { get; private set; } = new();
    public string Status { get; private set; } = "Öğretmen bilgilerini doldurun ve Kaydet düğmesine basın.";
    public event PropertyChangedEventHandler? PropertyChanged;
    public TeacherRecord SaveNew()
    {
        var record = Editor.ToRecord() with { Id = Guid.NewGuid() };
        var rows = directory.Save(record); var saved = rows.Single(r => r.Id == record.Id);
        Editor = new(); ShowStatus($"{saved.FullName} öğretmen listesine kaydedildi."); return saved;
    }
    public void Reset() { Editor = new(); ShowStatus("Yeni öğretmen kaydı için bilgiler temizlendi."); }
    public void ShowStatus(string message)
    {
        Status = message; PropertyChanged?.Invoke(this, new(nameof(Editor))); PropertyChanged?.Invoke(this, new(nameof(Status)));
    }
}
