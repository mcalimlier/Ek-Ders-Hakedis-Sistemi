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
    public string OccupationCode { get; set; } = "";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public TeacherRecord ToRecord() => new(Id, FullName, IdentityNumber, InsuranceNumber, Iban, School, Branch, OccupationCode, StartDate, EndDate);
    public static TeacherEditor From(TeacherRecord r) => new() { Id = r.Id, FullName = r.FullName,
        IdentityNumber = r.IdentityNumber, InsuranceNumber = r.InsuranceNumber, Iban = r.Iban,
        School = r.School, Branch = r.Branch, OccupationCode = r.OccupationCode, StartDate = r.StartDate, EndDate = r.EndDate };
}

public sealed class TeacherListViewModel : INotifyPropertyChanged
{
    private readonly TeacherDirectory directory;
    private TeacherRecord? selected;
    public ObservableCollection<TeacherRecord> Records { get; } = [];
    public TeacherEditor Editor { get; private set; } = new();
    public string Status { get; private set; } = "Yeni öğretmen ekleyebilir veya listeden bir kayıt seçerek düzenleyebilirsiniz.";
    public string CountText => $"{Records.Count} kayıtlı öğretmen";
    public bool HasSelection => Selected is not null;
    public TeacherRecord? Selected
    {
        get => selected;
        set { selected = value; if (value is not null) Editor = TeacherEditor.From(value); Notify(); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public TeacherListViewModel(string path)
    {
        directory = new(path);
        try { Replace(directory.Read()); }
        catch (Exception ex) when (ex is System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException) { ShowError(ex.Message); }
    }
    // Bir seçili kayıt immutable kopyadan düzenlenir; kaydetmeden liste değişmez.
    public void New() { selected = null; Editor = new(); Status = "Yeni öğretmen: ad soyad zorunludur; diğer bilgiler daha sonra tamamlanabilir."; Notify(); }
    public void Save()
    {
        var id = Editor.Id; Replace(directory.Save(Editor.ToRecord())); Selected = Records.Single(r => r.Id == id);
        Status = "Öğretmen kaydedildi. Önceki liste yedekte korundu."; Notify();
    }
    public void DeleteSelected()
    {
        var id = Selected?.Id ?? throw new InvalidOperationException("Silinecek öğretmeni seçin.");
        Replace(directory.Delete(id)); New(); Status = "Seçili öğretmen listeden silindi. Geçmiş hesap taslakları değiştirilmedi."; Notify();
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
