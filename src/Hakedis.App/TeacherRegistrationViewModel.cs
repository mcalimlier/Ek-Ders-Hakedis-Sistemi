using System.ComponentModel;
using Hakedis.Contracts;
using Hakedis.Storage;

namespace Hakedis.App;

// Yeni kayıt formu yalnızca Veri Girişi ekranına bağlanır.
public sealed class TeacherRegistrationViewModel : INotifyPropertyChanged
{
    private TeacherDirectory directory;
    private readonly string path;
    private TeacherRecord? original;
    public bool IsEditingRecord=>original is not null;
    public bool CanSwitchModule=>!IsEditingRecord;
    public bool HasDraft=>Editor.HasSchoolTransfer || new[]{Editor.FullName,Editor.IdentityNumber,Editor.Iban,Editor.School,Editor.OccupationCode}.Any(s=>!string.IsNullOrWhiteSpace(s)) || Editor.StartDate.HasValue || Editor.EndDate.HasValue;
    public string SaveButtonText=>IsEditingRecord?"Öğretmen Değişiklik kaydet":"Öğretmeni Kaydet";
    public string CancelButtonText=>IsEditingRecord?"Vazgeç":"Formu Temizle";
    public void BeginEdit(TeacherRecord record,TeacherRecord source)
    {
        SwitchModule(record.ModuleId);original=source;Editor=TeacherEditor.From(record);ShowStatus("Öğretmen bilgilerini düzenleyin; Kaydet veya Vazgeç düğmesini kullanın.");
    }
    public TeacherRecord SaveEdit()
    {
        var expected=original??throw new InvalidOperationException("Düzenlenecek öğretmen seçilmedi.");
        var rows=directory.UpdateBatch([expected],[Editor.ToRecord(Module)]);var saved=rows.Single(t=>t.Id==expected.Id);
        Reset();return saved;
    }
    public ModuleId Module { get; private set; }
    public TeacherEditor Editor { get; private set; } = new();
    public string Status { get; private set; } = "Öğretmen bilgilerini doldurun ve Kaydet düğmesine basın.";
    public event PropertyChangedEventHandler? PropertyChanged;
    public TeacherRegistrationViewModel(string path, ModuleId module = ModuleId.TemelEgitim)
    { this.path = path; Module = module; directory = new(path, module);Editor=new(module); }
    public void SwitchModule(ModuleId module)
    { if(IsEditingRecord)throw new InvalidOperationException("Düzenleme sırasında modül değiştirilemez.");directory = new(path, module); Module = module; Editor = new(module); ShowStatus("Öğretmen kaydı seçili modüle yapılacaktır."); }
    public TeacherRecord SaveNew()
    {
        if(IsEditingRecord)throw new InvalidOperationException("Düzenleme sırasında yeni kayıt oluşturulamaz.");
        var record = Editor.ToRecord(Module) with { Id = Guid.NewGuid() };
        var rows = directory.Save(record); var saved = rows.Single(r => r.Id == record.Id);
        Editor = new(Module); ShowStatus($"{saved.FullName} öğretmen listesine kaydedildi."); return saved;
    }
    public void Reset() { original=null;Editor = new(Module); ShowStatus("Yeni öğretmen kaydı için bilgiler temizlendi."); }
    public void ShowStatus(string message)
    {
        Status = message; PropertyChanged?.Invoke(this, new(nameof(Editor))); PropertyChanged?.Invoke(this, new(nameof(Status)));
        foreach(var name in new[]{nameof(IsEditingRecord),nameof(CanSwitchModule),nameof(SaveButtonText),nameof(CancelButtonText)})PropertyChanged?.Invoke(this,new(name));
    }
}
