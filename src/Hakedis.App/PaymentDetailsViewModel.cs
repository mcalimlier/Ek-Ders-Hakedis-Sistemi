using System.Globalization;
using Hakedis.Storage;
using Hakedis.TemelEgitim.Core;
namespace Hakedis.App;
public sealed class PaymentDetailsViewModel
{
    public ArchivedPayment Archive {get;}
    public PaymentSnapshot Snapshot=>Archive.Document.Data;
    public IReadOnlyList<PaymentRow> Rows=>Snapshot.Rows;
    public decimal Net=>Rows.Sum(r=>r.Result.Net);
    public string Heading=>$"{Archive.Module} · {Archive.Period}";
    public string People=>$"{Rows.Count} kişi · {Rows.Sum(r=>r.Input.DayHours+r.Input.DykHours):N2} saat";
    public string Gross=>$"{Rows.Sum(r=>r.Result.Gross):N2} TL";
    public string Accrual=>$"{Rows.Sum(r=>r.Result.Accrual):N2} TL";
    public string Deductions=>$"{Rows.Sum(r=>r.Result.Deductions):N2} TL";
    public string NetText=>$"{Net:N2} TL";
    public string Metadata=>$"Kayıt tarihi: {Archive.SavedAt.LocalDateTime:dd.MM.yyyy HH:mm}\n{Snapshot.MissingDaysStatus}";
    public string Constants
    {
        get {var p=Snapshot.Constants;return $"Katsayı: {p.Coefficient}   Gösterge: {p.Indicator}   DYK çarpanı: {p.DykMultiplier}\nSGK iştirakçi: {p.EmployeeRate:P2}   SGK işveren: {p.EmployerRate:P2}   Gelir vergisi: {p.IncomeRate:P2}   Damga vergisi: {p.StampRate:P3}\nGV istisnası: {p.IncomeExemption:N2} TL   DV istisnası: {p.StampExemption:N2} TL";}
    }
    public string Institution
    {
        get {var d=Snapshot.Details;return d is null?"Bu hakedişte kurum / imza bilgileri kaydedilmemiş.":$"{d.Institution}\nBanka: {d.Bank}\nGerçekleştirme görevlisi: {d.PreparedBy} · {d.PreparedTitle}\nHarcama yetkilisi: {d.AuthorizedBy} · {d.AuthorizedTitle}\nSayman: {d.Treasurer}\n{d.SickLeaveStatement}";}
    }
    public string Movement=>Snapshot.Movement is {} m?$"Önceki ay: {m.Previous}   Başlayan: {m.Entered}   Ayrılan: {m.Exited}":"Personel hareket bilgisi bu kayıtta bulunmuyor.";
    public string Rules=>$"Hesaplama kuralı: {Archive.Document.RuleVersion}\nSGK kuralı: {Snapshot.SgkRuleVersion}\nKaynak kayıt: {Archive.Path}";
    public PaymentDetailsViewModel(ArchivedPayment archive)=>Archive=archive;
    public string PersonDetails(PaymentRow row)
    {
        var t=row.Teacher;var i=row.Input;
        var history=t.Assignments is null?"Okul/modül değişikliği yok.":TeacherEditor.From(t).HistoryText;
        return $"{t.FullName} · TC: {t.IdentityNumber} · IBAN: {t.Iban}\nGörev yeri: {t.School} · Meslek kodu: {t.OccupationCode}\nİşe başlama: {t.StartDate:dd.MM.yyyy} · Ayrılma: {t.EndDate:dd.MM.yyyy}\n{history}\n"+
            (i.SchoolHoursSeparated?$"Önceki kurum gündüz: {i.PreviousSchoolDayHours?.ToString(CultureInfo.GetCultureInfo("tr-TR"))??"Boş (0)"} · Yeni kurum gündüz: {i.NewSchoolDayHours?.ToString(CultureInfo.GetCultureInfo("tr-TR"))??"Boş (0)"}\n":"")+
            $"SGK kontrolü: {(string.IsNullOrEmpty(row.SgkError)?"Uygun":row.SgkError)}";
    }
}
