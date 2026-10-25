using System.Globalization;
using Hakedis.Contracts;

namespace Hakedis.TemelEgitim.Core;

public sealed record ReportTable(string Title,string[] Headers,double[] Widths,string[][] Rows,string[]? Total=null);
public sealed record PaymentReport(string FileName,string Title,bool Landscape,string[] Information,ReportTable[] Tables,string[] Notes);

// Belgeler yalnız dondurulmuş hakediş sonucunu kullanır; yeniden mali hesap yapılmaz.
public static class PaymentReports
{
    private static readonly CultureInfo Tr=CultureInfo.GetCultureInfo("tr-TR");
    private static string Money(decimal v)=>v.ToString("N2",Tr);
    private static string Number(decimal v)=>v.ToString("0.#########",Tr);
    public static PaymentReport[] Build(PaymentSnapshot data,ReportDetails details,ModuleId? module=null)
    {
        var rows=data.Rows;var period=$"{Tr.DateTimeFormat.GetMonthName(data.Month)}-{data.Year}";
        var selectedModule=module??rows.FirstOrDefault()?.Teacher.ModuleId??ModuleId.TemelEgitim;
        var p=data.Constants;
        var info=new[]{"DAİRESİ            : "+details.Institution,"İLGİLİ AY           : "+period,
            $"KATSAYI: {Number(p.Coefficient)}    GÜNDÜZ GÖSTERGE: {Number(p.Indicator)}    DYK çarpanı: {Number(p.DykMultiplier)}    GV İstinası: {Money(p.IncomeExemption)}    DV İstisnası: {Money(p.StampExemption)}",
            $"Uygulanan oranlar · SGK iştirakçi: {Number(p.EmployeeRate*100)}% · İşveren: {Number(p.EmployerRate*100)}% · Gelir: {Number(p.IncomeRate*100)}% · Damga: {Number(p.StampRate*100)}%"};
        var h1=new[]{ExcelHeadings.Sequence,ExcelHeadings.BordroB6,ExcelHeadings.BordroC6,ExcelHeadings.BordroE6,
            ExcelHeadings.BordroF5,ExcelHeadings.BordroG5,ExcelHeadings.BordroH5,ExcelHeadings.BordroM5,ExcelHeadings.BordroL5,
            ExcelHeadings.BordroJ5,ExcelHeadings.BordroI5,ExcelHeadings.BordroK5,ExcelHeadings.BordroN6,ModuleLabels.EmployerContributionHeader(p.EmployerRate),ExcelHeadings.BordroP6};
        var a=rows.Select((r,i)=>new[]{(i+1).ToString(),r.Teacher.FullName,ModuleLabels.Role(selectedModule),r.Teacher.IdentityNumber,
            Number(r.Input.DayHours),Number(r.Input.DykHours),Number(r.Input.DayHours+r.Input.DykHours),Number(r.Result.DayRate),Number(r.Result.DykRate),
            Money(r.Result.DayGross),Money(r.Result.DykGross),r.Result.PremiumDays.ToString(),Money(r.Result.Gross),Money(r.Result.EmployerPremium),Money(r.Result.Accrual)}).ToArray();
        var total1=new[]{"TOPLAM","","","",Number(rows.Sum(r=>r.Input.DayHours)),Number(rows.Sum(r=>r.Input.DykHours)),Number(rows.Sum(r=>r.Input.DayHours+r.Input.DykHours)),"","",
            Money(rows.Sum(r=>r.Result.DayGross)),Money(rows.Sum(r=>r.Result.DykGross)),rows.Sum(r=>r.Result.PremiumDays).ToString(),Money(rows.Sum(r=>r.Result.Gross)),Money(rows.Sum(r=>r.Result.EmployerPremium)),Money(rows.Sum(r=>r.Result.Accrual))};
        var h2=new[]{ExcelHeadings.Sequence,ExcelHeadings.BordroB6,ExcelHeadings.BordroE6,ExcelHeadings.BordroQ5,ExcelHeadings.BordroR5,
            ExcelHeadings.BordroS6,ExcelHeadings.BordroT6,ExcelHeadings.BordroU6,ExcelHeadings.BordroV6,ModuleLabels.EmployeeHeader(p.EmployeeRate),ModuleLabels.EmployerHeader(p.EmployerRate),ExcelHeadings.BordroY6,ExcelHeadings.BordroZ6,ExcelHeadings.BordroAA5};
        var b=rows.Select((r,i)=>new[]{(i+1).ToString(),r.Teacher.FullName,r.Teacher.IdentityNumber,Money(r.Result.IncomeBase),"0,00",Money(p.IncomeExemption),Money(r.Result.IncomeTax),Money(p.StampExemption),
            Money(r.Result.StampTax),Money(r.Result.EmployeePremium),Money(r.Result.EmployerPremium),Money(r.Input.Garnishment),Money(r.Result.Deductions),Money(r.Result.Net)}).ToArray();
        var total2=new[]{"TOPLAM","","",Money(rows.Sum(r=>r.Result.IncomeBase)),"0,00","",Money(rows.Sum(r=>r.Result.IncomeTax)),"",Money(rows.Sum(r=>r.Result.StampTax)),Money(rows.Sum(r=>r.Result.EmployeePremium)),
            Money(rows.Sum(r=>r.Result.EmployerPremium)),Money(rows.Sum(r=>r.Input.Garnishment)),Money(rows.Sum(r=>r.Result.Deductions)),Money(rows.Sum(r=>r.Result.Net))};
        var payroll=new PaymentReport("Bordro.pdf",ModuleLabels.PayrollTitle(selectedModule),true,info,
            [new("Ders, ücret ve tahakkuk",h1,[40,108,52,66,42,42,43,47,47,53,53,38,58,58,58],a,total1),
             new("Vergi, kesinti ve net ödeme",h2,[40,108,66,58,53,55,57,55,53,54,54,52,55,58],b,total2)],
            ["Bordroda yazılı alacaklar kayıtlara uygundur.","SÜRE GELEN VERGİ MATRAHI kaynak hesapta kullanılmadığından 0,00 gösterilir."]);
        var bankRows=rows.Select((r,i)=>new[]{(i+1).ToString(),r.Teacher.FullName,r.Teacher.IdentityNumber,r.Teacher.Iban,Money(r.Result.Net)}).ToArray();
        var bank=new PaymentReport("Banka Listesi.pdf","B  A  N  K  A       L  İ  S  T  E  S  İ",false,
            ["DAİRENİN ADI               "+details.Institution,"BANKANIN ADI              "+details.Bank,"AİT OLDUĞU AY             "+period],
            [new("",["SN","ADI SOYADI","Tc Kimlik No","BANKA IBAN HESAP NO","ÜCRET"],[28,125,77,183,82],bankRows,["Toplam","","","",Money(rows.Sum(r=>r.Result.Net))])],[]);
        var movement=data.Movement??new PersonnelMovement(0,rows.Count(r=>r.Teacher.StartDate is DateTime start && start.Year==data.Year && start.Month==data.Month),rows.Count(r=>r.Teacher.EndDate is DateTime end && end.Year==data.Year && end.Month==data.Month));
        var personnelRows=new[]{new[]{"1","Geçen Ay Personel Sayısı",movement.Previous.ToString(),"",""},
            new[]{"2","Bu Ay İçinde Giren(x)",movement.Entered.ToString(),"",""},new[]{"3","Bu Ay İçinde Çıkan",movement.Exited.ToString(),"",""},
            new[]{"4","Ödeme Yapılacak Personel Sayısı(1+2-3)",rows.Length.ToString(),Money(rows.Sum(r=>r.Result.Accrual)),Money(rows.Sum(r=>r.Result.Net))}};
        var personnel=new PaymentReport("Personel Bildirimi.pdf","PERSONEL BİLDİRİMİ",false,
            [details.Institution,"Ait Olduğu Ay: "+period,"Bütçe Yılı: "+data.Year],
            [new("1 Nolu Tablo",["","","Personel Sayısı","Tahakkuk Toplamı",ExcelHeadings.BordroAA5],[25,235,65,85,85],personnelRows),
             new("II Nolu Tablo   (xx)",["Sıra N","Memurun Adı Soyadı"," Geçen Aylarda\nKullandığı Hastalık\nizin süresi","Bu Ay Kullandığı\nHastalık İzni Süresi","Toplam Hastalık\nizni süresi"],[30,160,105,105,95],
                [new[]{"","","","",""},new[]{"","","","",""}])],
            ["Geçen ay sayısı aynı modülün önceki ay son hakedişinden; giriş ve çıkışlar ilgili aydaki normal işe giriş/çıkış tarihlerinden alınır.",
             "II Nolu tablo personel birimi tarafından kontrol edilerek tamamlanmalıdır.",
             "(x)Ay içinde girenlerin isim listesi eklenecektir",
             "(xx) II Nolu tablonun düzenlenmesinden sadece personel birimi yetkilisi sorumludur",
             "(xxx) Müstakil personel birimi olmayan dairelerde personel işleriyle görevlendirilmiş olanlar tarafından imzalanır."]);
        return [payroll,bank,personnel];
    }
}
