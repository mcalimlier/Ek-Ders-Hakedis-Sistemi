using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using Hakedis.Contracts;
using Hakedis.Storage;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.App;

public sealed class PersonalPayrollViewModel : INotifyPropertyChanged
{
    private readonly PaymentArchiveStore store;
    private ArchivedPayment? archive;
    private TeacherRecord? selected;
    private ModuleDescriptor module;
    private int year=DateTime.Today.Year,month=DateTime.Today.Month;
    private string lastOutput="";
    public IReadOnlyList<ModuleDescriptor> Modules {get;}=new ModuleRegistry([new TeacherPayrollModule()]).Catalog;
    public IReadOnlyList<MonthOption> Months {get;}=Enumerable.Range(1,12).Select(i=>new MonthOption(i,CultureInfo.GetCultureInfo("tr-TR").DateTimeFormat.GetMonthName(i))).ToArray();
    public IReadOnlyList<int> Years {get;}=Enumerable.Range(1900,201).ToArray();
    public bool CanChangePeriod=>true;
    public ObservableCollection<TeacherRecord> Teachers {get;}=[];
    public ModuleDescriptor SelectedModule {get=>module;set{if(value is null || value.Id==module.Id)return;module=Modules.Single(m=>m.Id==value.Id);ClearSelection();Reload();}}
    public int Year {get=>year;set{if(value is <1900 or >2100 || value==year)return;year=value;ClearSelection();Reload();}}
    public int Month {get=>month;set{if(value is <1 or >12 || value==month)return;month=value;ClearSelection();Reload();}}
    public string PeriodText=>$"{Months[month-1].Name}-{year}";
    public TeacherRecord? SelectedTeacher
    {
        get=>selected;
        set{var next=value is not null && Teachers.Contains(value)?value:null;if(next==selected)return;selected=next;lastOutput="";Notify();}
    }
    public bool CanCreate=>archive is not null && selected is not null;
    public bool CanOpenFolder=>lastOutput!="" && File.Exists(lastOutput);
    public string? OutputFolder=>lastOutput==""?null:Path.GetDirectoryName(lastOutput);
    public string LastOutputPath=>lastOutput;
    public string CountText=>$"{Teachers.Count} öğretmen · {PeriodText}";
    public string Status {get;private set;}="";
    public event PropertyChangedEventHandler? PropertyChanged;
    public PersonalPayrollViewModel(string root)
    {store=new(root);module=Modules.Single(m=>m.Id==ModuleId.TemelEgitim);Reload();}
    private void ClearSelection(){selected=null;archive=null;lastOutput="";}
    public void Reload()
    {
        var oldId=selected?.Id;var oldVersion=archive?.Version;var oldOutput=lastOutput;
        ClearSelection();Teachers.Clear();
        try
        {
            if(!module.IsAvailable){Status="Seçilen modülün hesaplama çekirdeği henüz eklenmedi.";return;}
            var matches=store.ReadAll().Where(r=>r.Document.ModuleId==module.Id && r.Document.Data.Year==year && r.Document.Data.Month==month).ToArray();
            if(matches.Length>1)throw new InvalidDataException("Bu dönem için birden fazla hakediş var. Çıktı panelindeki kayıtları kontrol edin.");
            archive=matches.SingleOrDefault();
            if(archive is null){Status="Bu modül ve döneme ait kayıtlı hakediş bulunmuyor. Önce hakedişi oluşturun veya başka bir dönem seçin.";return;}
            foreach(var t in archive.Document.Data.Rows.Select(r=>r.Teacher).OrderBy(t=>t.FullName,StringComparer.CurrentCultureIgnoreCase))Teachers.Add(t);
            selected=Teachers.SingleOrDefault(t=>t.Id==oldId);
            if(selected is not null && oldVersion==archive.Version)lastOutput=oldOutput;
            Status="Kayıtlı hakedişten bir öğretmen seçin. Kişisel bordro, o dönemin kaydedilmiş tutarlarıyla hazırlanır.";
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {ClearSelection();Teachers.Clear();Status=ex.Message;}
        finally{Notify();}
    }
    public ArchivedPayment ReadSelection()
    {
        if(archive is null || selected is null || !Teachers.Contains(selected))throw new InvalidOperationException("Önce kayıtlı hakediş bulunan dönemi ve bir öğretmeni seçin.");
        var current=store.Read(archive.Path);
        if(current.Version!=archive.Version)throw new InvalidOperationException("Hakediş kaydı değişmiş. Kişisel Bordro listesini yenileyip öğretmeni tekrar seçin.");
        if(current.Document.ModuleId!=module.Id || current.Document.Data.Year!=year || current.Document.Data.Month!=month ||
           !current.Document.Data.Rows.Any(r=>r.Teacher.Id==selected.Id))throw new InvalidOperationException("Seçilen öğretmen bu dönemin hakedişinde bulunmuyor.");
        return current;
    }
    public void AcceptOutput(string path)
    {
        if(!CanCreate || !File.Exists(path))throw new InvalidOperationException("Kişisel bordro dosyası oluşturulamadı.");
        lastOutput=Path.GetFullPath(path);
        Status=$"{selected!.FullName} · {PeriodText} için kişisel bordro PDF olarak kaydedildi. Klasör düğmesiyle dosyaya ulaşabilirsiniz.";Notify();
    }
    public void ShowError(string message){Status=message;Notify();}
    private void Notify()
    {
        foreach(var name in new[]{nameof(SelectedModule),nameof(Year),nameof(Month),nameof(PeriodText),nameof(SelectedTeacher),nameof(CanCreate),nameof(CanOpenFolder),nameof(OutputFolder),nameof(LastOutputPath),nameof(CountText),nameof(Status)})
            PropertyChanged?.Invoke(this,new(name));
    }
}
