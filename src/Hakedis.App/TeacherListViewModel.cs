using System.Collections.ObjectModel;
using System.ComponentModel;
using Hakedis.Contracts;
using Hakedis.Storage;

namespace Hakedis.App;

public sealed class TeacherEditor
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = "";
    public string IdentityNumber { get; set; } = "";
    public string InsuranceNumber { get; set; } = "";
    public string Iban { get; set; } = "";
    public string School { get; set; } = "";
    public string Branch { get; set; } = "";
    public string School2 { get; set; } = "";
    public string Branch2 { get; set; } = "";
    public string OccupationCode { get; set; } = "";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public TeacherRecord ToRecord(ModuleId module = ModuleId.TemelEgitim) => new(Id, FullName, IdentityNumber, InsuranceNumber, Iban, School, Branch, OccupationCode, StartDate, EndDate, module,School2,Branch2);
    public static TeacherEditor From(TeacherRecord r) => new() { Id = r.Id, FullName = r.FullName,
        IdentityNumber = r.IdentityNumber, InsuranceNumber = r.InsuranceNumber, Iban = r.Iban,
        School = r.School, Branch = r.Branch, School2=r.School2,Branch2=r.Branch2,OccupationCode = r.OccupationCode, StartDate = r.StartDate, EndDate = r.EndDate };
}

public sealed class TeacherListViewModel : INotifyPropertyChanged
{
    private TeacherDirectory directory;
    private readonly string path;
    public ModuleId Module { get; private set; }
    private TeacherRecord? selected;
    public ObservableCollection<TeacherRecord> Records { get; } = [];
    public TeacherEditor Editor { get; private set; } = new();
    public string Status { get; private set; } = "Düzenlemek için listeden öğretmen seçin. Yeni kayıtlar Veri Girişi ekranından yapılır.";
    public string CountText => $"{Records.Count} kayıtlı öğretmen";
    public bool HasSelection => Selected is not null;
    public TeacherRecord? Selected
    {
        get => selected;
        set
        {
            if (value is not null && value.ModuleId != Module) throw new InvalidOperationException("Bu öğretmen seçili modüle ait değil.");
            selected = value; Editor = value is null ? new() : TeacherEditor.From(value); Notify();
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public TeacherListViewModel(string path, ModuleId module = ModuleId.TemelEgitim)
    {
        this.path = path; Module = module; directory = new(path, module);
        try { Replace(directory.Read()); }
        catch (Exception ex) when (ex is System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException) { ShowError(ex.Message); }
    }
    public void SwitchModule(ModuleId module)
    {
        directory = new(path, module); Module = module; Records.Clear(); Selected = null;
        Status = "Seçili modülün öğretmenleri gösteriliyor. Yeni kayıt için Veri Girişi ekranını kullanın.";
        try { Reload(); }
        catch (Exception ex) when (ex is System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException) { ShowError(ex.Message); }
        Notify();
    }
    // Bir seçili kayıt immutable kopyadan düzenlenir; kaydetmeden liste değişmez.
    public void Reload() { Replace(directory.Read()); Selected = null; }
    public void Save()
    {
        var id = Selected?.Id ?? throw new InvalidOperationException("Düzenlemek için kayıtlı bir öğretmen seçin. Yeni kayıt için Veri Girişi ekranını kullanın.");
        if (Editor.Id != id) throw new InvalidOperationException("Seçili öğretmen ile düzenlenen kayıt uyuşmuyor.");
        Replace(directory.Update(Editor.ToRecord(Module))); Selected = Records.Single(r => r.Id == id);
        Status = "Öğretmen bilgileri güncellendi."; Notify();
    }
    public void DeleteSelected()
    {
        var id = Selected?.Id ?? throw new InvalidOperationException("Silinecek öğretmeni seçin.");
        Replace(directory.Delete(id)); Selected = null; Status = "Seçili öğretmen listeden silindi. Geçmiş hesap taslakları değiştirilmedi."; Notify();
    }
    public void ShowError(string message) { Status = message; Notify(); }
    private void Replace(IReadOnlyList<TeacherRecord> rows)
    {
        Records.Clear(); foreach (var row in rows.OrderBy(r => r.FullName, StringComparer.CurrentCultureIgnoreCase)) Records.Add(row);
        Notify();
    }
    private void Notify()
    {
        foreach (var name in new[] { nameof(Selected), nameof(Editor), nameof(Status), nameof(CountText), nameof(HasSelection) })
            PropertyChanged?.Invoke(this, new(name));
    }
}
