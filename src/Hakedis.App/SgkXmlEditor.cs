using System.Collections.ObjectModel;
using System.Globalization;
using Hakedis.Contracts;
using Hakedis.Storage;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.App;

public sealed record SgkNatureOption(string Code,string Label);

public sealed class SgkPersonEditor
{
    public Guid TeacherId {get;init;}
    public string FullName {get;init;}="";
    public string IdentityNumber {get;init;}="";
    public string GivenName {get;set;}="";
    public string FamilyName {get;set;}="";
    public string MissingReason {get;set;}="";
    public string ExitReason {get;set;}="";
    public string WorkedOnReport {get;set;}="";
    public string FirstSurname {get;set;}="";
    public string Bonus {get;set;}="";
    public string PaidLeaveDays {get;set;}="";
    public string PaidLeaveGross {get;set;}="";
    public string Days {get;init;}="—";
    public string MissingDays {get;init;}="—";
    public string Start {get;init;}="";
    public string End {get;init;}="";
    public SgkPerson Read()=>new(TeacherId,GivenName.Trim(),FamilyName.Trim(),MissingReason.Trim(),ExitReason.Trim(),WorkedOnReport.Trim(),FirstSurname.Trim(),Bonus.Trim(),PaidLeaveDays.Trim(),PaidLeaveGross.Trim());
}

public sealed class SgkXmlEditor
{
    public ArchivedPayment Record {get;}
    private readonly SgkXmlSettings initial;
    public string Title=>$"SGK XML Oluştur · {Record.Module} · {Record.Period}";
    public string RegistrationNumber {get;set;}
    public string WorkplaceTitle {get;set;}
    public string Address {get;set;}
    public string DocumentType {get;set;}
    public string LawCode {get;set;}
    private string nature="";
    public string DocumentNature {get=>nature;set=>nature=value??"";}
    public string DefaultMissingReason {get;set;}
    public string TaxNumber {get;set;}
    public bool Confirmed {get;set;}
    public IReadOnlyList<SgkNatureOption> Natures {get;}=[new("A","A — Asıl"),new("E","E — Ek"),new("İ","İ — İptal")];
    public ObservableCollection<SgkPersonEditor> People {get;}=[];
    public SgkXmlEditor(ArchivedPayment record,SgkXmlSettings settings)
    {
        Record=record;initial=settings;var w=settings.Workplace;
        RegistrationNumber=w.RegistrationNumber;WorkplaceTitle=w.Title;Address=w.Address;DocumentType=w.DocumentType;LawCode=w.LawCode;
        DocumentNature=Natures.Any(n=>n.Code==w.DocumentNature)?w.DocumentNature:"";
        DefaultMissingReason=w.DefaultMissingReason;TaxNumber=w.TaxNumber;
        var data=record.Document.Data;
        foreach(var row in data.Rows)
        {
            var t=row.Teacher;var parts=t.FullName.Split(' ',StringSplitOptions.RemoveEmptyEntries);var known=settings.Names.FirstOrDefault(n=>n.TeacherId==t.Id);
            // Katalogda isim değişmişse önceki XML'deki eski adı tekrar önermeyin.
            if(known is not null && CultureInfo.GetCultureInfo("tr-TR").CompareInfo.Compare(
                string.Join(" ",(known.GivenName+" "+known.FamilyName).Split(' ',StringSplitOptions.RemoveEmptyEntries)),
                string.Join(" ",parts),CompareOptions.IgnoreCase)!=0)known=null;
            CalendarResult? days=null;
            try{days=PayrollCores.Calendar(record.Document.ModuleId,data.Year,data.Month,t.StartDate,t.EndDate,row.Input.DayHours,row.Input.DykHours);}catch(ArgumentException){}
            string Date(DateTime? date)=>date is DateTime d && d.Year==data.Year && d.Month==data.Month?d.ToString("dd.MM.yyyy",CultureInfo.InvariantCulture):"";
            People.Add(new(){TeacherId=t.Id,FullName=t.FullName,IdentityNumber=t.IdentityNumber,
                GivenName=known?.GivenName??string.Join(" ",parts.SkipLast(1)),FamilyName=known?.FamilyName??parts.LastOrDefault()??"",
                Days=days?.PremiumDays.ToString()??"—",MissingDays=days?.MissingDays.ToString()??"—",Start=Date(t.StartDate),End=Date(t.EndDate)});
        }
    }
    public SgkWorkplace Workplace()=>new(RegistrationNumber.Trim(),WorkplaceTitle.Trim(),Address.Trim(),DocumentType.Trim(),LawCode.Trim(),DocumentNature.Trim(),DefaultMissingReason.Trim(),TaxNumber.Trim());
    public SgkPerson[] ReadPeople()=>People.Select(p=>p.Read()).ToArray();
    public byte[] Build()
    {
        if(!Confirmed)throw new ArgumentException("Ad/soyad ayrımını, işyeri bilgilerini ve SGK neden kodlarını kontrol edip onay kutusunu işaretleyin.");
        return SgkXmlStore.Build(Record.Document.ModuleId,Record.Document.Data,Workplace(),ReadPeople());
    }
    public SgkXmlSettings Settings()=>new(Workplace(),initial.Names.Where(n=>People.All(p=>p.TeacherId!=n.TeacherId))
        .Concat(People.Select(p=>new SgkName(p.TeacherId,p.GivenName.Trim(),p.FamilyName.Trim()))).ToArray());
}
