using System.Collections.ObjectModel;
using System.ComponentModel;
using Hakedis.Contracts;
using Hakedis.Storage;

namespace Hakedis.App;

public sealed class TeacherEditor : INotifyPropertyChanged
{
    private TeacherAssignmentHistory? savedHistory;
    private string originalSchool="";
    private ModuleId originalModule;
    private bool special;
    private int editTransfer=-1;
    public event PropertyChangedEventHandler? PropertyChanged;
    public bool HasSchoolTransfer {get=>special;set{special=value;NotifyTransfer();}}
    public string TransferSchool {get;set;}="";
    public DateTime? TransferDate {get;set;}
    public ModuleId? TransferModule {get;set;}
    public IReadOnlyList<ModuleDescriptor> TransferModules=>PayrollCores.Catalog.Where(m=>m.IsAvailable).ToArray();
    public bool HasAssignmentHistory=>savedHistory is not null;
    public string HistoryText=>savedHistory is null?"":string.Join(Environment.NewLine,savedHistory.Transfers.Select(t=>
        $"Yeni okulda başlama: {t.Date:dd.MM.yyyy} · {Name(t.FromModule)} / {t.FromSchool} → {Name(t.ToModule)} / {t.ToSchool}"));
    public string TransferModeText=>editTransfer<0?"Yeni okul/modül değişikliği":"Son okul/modül değişikliğini düzenle";
    public System.Windows.Input.ICommand EditLastTransferCommand=>new EditorAction(EditLastTransfer);
    public System.Windows.Input.ICommand NewTransferCommand=>new EditorAction(NewTransfer);
    private static string Name(ModuleId id)=>PayrollCores.Catalog.FirstOrDefault(m=>m.Id==id)?.Name??"Eski görev";
    public TeacherEditor(ModuleId module=ModuleId.TemelEgitim){originalModule=module;}
    public void EditLastTransfer()
    {
        if(savedHistory is null)return;editTransfer=savedHistory.Transfers.Length-1;var t=savedHistory.Transfers[editTransfer];
        special=true;TransferSchool=t.ToSchool;TransferDate=t.Date;TransferModule=t.ToModule;NotifyTransfer();
    }
    public void NewTransfer()
    {
        editTransfer=-1;special=true;TransferSchool="";TransferDate=null;
        TransferModule=null;NotifyTransfer();
    }
    private void NotifyTransfer(){foreach(var name in new[]{nameof(HasSchoolTransfer),nameof(TransferSchool),nameof(TransferDate),nameof(TransferModule),nameof(TransferModeText),nameof(HasAssignmentHistory),nameof(HistoryText)})PropertyChanged?.Invoke(this,new(name));}
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = "";
    public string IdentityNumber { get; set; } = "";
    public string Iban { get; set; } = "";
    public string School { get; set; } = "";
    public string OccupationCode { get; set; } = "";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public TeacherRecord ToRecord(ModuleId module = ModuleId.TemelEgitim)
    {
        var history=savedHistory;
        if(history is not null && School!=originalSchool)history=history.WithSchool(originalModule,originalSchool,School);
        if(special)
        {
            var changes=history?.Transfers.ToList()??[];
            var from=editTransfer>=0?changes[editTransfer].FromModule:changes.LastOrDefault()?.ToModule??module;
            var fromSchool=editTransfer>=0?changes[editTransfer].FromSchool:changes.LastOrDefault()?.ToSchool??School;
            var item=new SchoolTransfer(TransferDate,from,fromSchool,TransferModule??(ModuleId)(-1),TransferSchool);
            if(editTransfer>=0)changes[editTransfer]=item;else changes.Add(item);
            history=new(history?.HomeModuleId??module,history?.HomeSchool??School,changes.ToArray(),history?.Choices??[]);
        }
        return new(Id,FullName,IdentityNumber,Iban,School,OccupationCode,StartDate,EndDate,module,history);
    }
    public static TeacherEditor From(TeacherRecord r) => new(r.ModuleId) { Id = r.Id, FullName = r.FullName,
        IdentityNumber = r.IdentityNumber, Iban = r.Iban,
        School = r.School, OccupationCode = r.OccupationCode, StartDate = r.StartDate, EndDate = r.EndDate,
        savedHistory=r.Assignments,originalSchool=r.School,originalModule=r.ModuleId };
    private sealed class EditorAction(Action action) : System.Windows.Input.ICommand
    {public event EventHandler? CanExecuteChanged {add{}remove{}} public bool CanExecute(object? parameter)=>true;public void Execute(object? parameter)=>action();}
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
    public bool HasChanges=>Selected is not null && Editor.ToRecord(Module)!=Selected;
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
        Replace(directory.UpdateBatch([Selected!],[Editor.ToRecord(Module)])); Selected = Records.Single(r => r.Id == id);
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
