using System.Collections.ObjectModel;
using System.ComponentModel;
using Hakedis.Contracts;
using Hakedis.Storage;

namespace Hakedis.App;

public sealed class ReportDetailsEditor
{
    public string Institution {get;set;}="";
    public string Bank {get;set;}="";
    public string PreparedBy {get;set;}="";
    public string PreparedTitle {get;set;}="Şube Müdürü";
    public string AuthorizedBy {get;set;}="";
    public string AuthorizedTitle {get;set;}="İlçe Milli Eğitim Müdürü";
    public string Treasurer {get;set;}="";
    public string SickLeaveStatement {get;set;}="Hastalık izni kullanan personel yoktur";
    public ReportDetails Read()=>new(Institution.Trim(),Bank.Trim(),PreparedBy.Trim(),PreparedTitle.Trim(),AuthorizedBy.Trim(),AuthorizedTitle.Trim(),Treasurer.Trim(),SickLeaveStatement.Trim());
    public static ReportDetailsEditor From(ReportDetails d)=>new(){Institution=d.Institution,Bank=d.Bank,PreparedBy=d.PreparedBy,PreparedTitle=d.PreparedTitle,AuthorizedBy=d.AuthorizedBy,AuthorizedTitle=d.AuthorizedTitle,Treasurer=d.Treasurer,SickLeaveStatement=d.SickLeaveStatement};
}

public sealed class OutputsViewModel : INotifyPropertyChanged
{
    public string DataRoot {get;}
    private readonly PaymentArchiveStore store;
    private readonly ReportDetailsStore detailsStore;
    private ArchivedPayment? selected;
    public ObservableCollection<ArchivedPayment> Records {get;}=[];
    public ArchivedPayment? Selected {get=>selected;set{selected=value;Notify();}}
    public bool HasSelection=>Selected is not null;
    public ReportDetailsEditor Details {get;}
    public string Status {get;private set;}="";
    public string DetailsStatus {get;private set;}="";
    public event PropertyChangedEventHandler? PropertyChanged;
    public OutputsViewModel(string root)
    {DataRoot=System.IO.Path.GetFullPath(root);store=new(root);detailsStore=new(root);Details=ReportDetailsEditor.From(detailsStore.Read());Reload();}
    public void SaveDetails(){detailsStore.Save(Details.Read());DetailsStatus="Belge bilgileri kaydedildi. Bundan sonra oluşturulacak hakedişlere bu bilgiler eklenir.";Notify();}
    public void Reload()
    {
        var path=Selected?.Path;Records.Clear();selected=null;
        try{foreach(var record in store.ReadAll())Records.Add(record);selected=Records.FirstOrDefault(r=>r.Path==path);Status=$"{Records.Count} kayıtlı hakediş. PDF veya Yazdır için kayıt seçin.";}
        catch(Exception ex) when(ex is System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException or ArgumentException){Status="Kayıtlar okunamadı: "+ex.Message;}
        Notify();
    }
    public ArchivedPayment ReadSelected()
    {
        var current=store.Read(Selected?.Path??throw new InvalidOperationException("Önce kayıtlı hakediş seçin."));
        if(current.Version!=Selected!.Version)throw new InvalidOperationException("Kayıt değişmiş. Listeyi yenileyin.");return current;
    }
    public void ShowStatus(string message){Status=message;Notify();}
    private void Notify(){foreach(var name in new[]{nameof(DetailsStatus),nameof(Selected),nameof(HasSelection),nameof(Status)})PropertyChanged?.Invoke(this,new(name));}
}
