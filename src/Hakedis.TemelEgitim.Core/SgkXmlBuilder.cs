using Hakedis.Contracts;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Hakedis.TemelEgitim.Core;

public static class SgkXmlBuilder
{
    public static Stream OpenSchema()=>typeof(SgkXmlBuilder).Assembly.GetManifestResourceStream("Hakedis.TemelEgitim.Core.Schemas.SgkAylikBildirgeler.xsd")
        ??throw new InvalidOperationException("SGK doğrulama şeması bulunamadı.");
    public static void ValidateWorkplace(SgkWorkplace w)
    {
        if(!Regex.IsMatch(w.RegistrationNumber,"^[0-9]{26}$"))throw new ArgumentException("İşyeri sicil/kontrol/aracı numarası toplam 26 rakam olmalıdır.");
        _=Code(w.RegistrationNumber.Substring(21,2),1,97,"KONTROLNO");
        _=Code(w.RegistrationNumber[23..],0,999,"ISYERIARACINO");
        Text(w.Title,50,"ISYERIUNVAN / İşyeri unvanı");Text(w.Address,50,"ISYERIADRES / İşyeri adresi");
        _=Code(w.DocumentType,1,92,"BELGETURU / Belge türü");
        if(!Regex.IsMatch(w.LawCode,"^[0-9]{5}$"))throw new ArgumentException("KANUN beş rakam olmalıdır; SGK ekranındaki kanun kodunu girin.");
        if(w.DocumentNature is not ("A" or "E" or "İ"))throw new ArgumentException("Belge mahiyetini seçin: A (Asıl), E (Ek) veya İ (İptal). İptal kodu Türkçe büyük İ harfidir.");
        if(w.DefaultMissingReason!="")_=Code(w.DefaultMissingReason,1,26,"Varsayılan eksik gün nedeni");
        if(w.TaxNumber!="" && !Regex.IsMatch(w.TaxNumber,"^[0-9]{10}$"))throw new ArgumentException("Vergi numarası boş veya 10 rakam olmalıdır.");
    }
    public static byte[] Build(PaymentSnapshot snapshot,SgkWorkplace workplace,IReadOnlyList<SgkPerson> people)
    {
        ValidateWorkplace(workplace);
        if(snapshot.Year is <2003 or >2050 || snapshot.Month is <1 or >12 || snapshot.Rows.Length is 0 or >99999)
            throw new ArgumentException("SGK şeması 2003–2050 dönemi ve en az bir sigortalı kabul eder.");
        if(people.Select(x=>x.TeacherId).Distinct().Count()!=people.Count || !people.Select(x=>x.TeacherId).ToHashSet().SetEquals(snapshot.Rows.Select(x=>x.Teacher.Id)))
            throw new ArgumentException("SGK kişi listesi kayıtlı hakedişle birebir aynı olmalıdır.");
        if(snapshot.Rows.Select(r=>r.Teacher.Id).Distinct().Count()!=snapshot.Rows.Length || snapshot.Rows.Select(r=>r.Teacher.IdentityNumber).Distinct().Count()!=snapshot.Rows.Length)
            throw new ArgumentException("SGK listesinde yinelenen öğretmen veya T.C. kimlik numarası var.");
        var w=new XElement("ISYERI",new XAttribute("ISYERISICIL",workplace.RegistrationNumber[..21]),new XAttribute("KONTROLNO",workplace.RegistrationNumber.Substring(21,2)),
            new XAttribute("ISYERIARACINO",workplace.RegistrationNumber[23..]),new XAttribute("ISYERIUNVAN",workplace.Title),new XAttribute("ISYERIADRES",workplace.Address));
        if(workplace.TaxNumber!="")w.Add(new XAttribute("ISYERIVERGINO",workplace.TaxNumber));
        var insured=new XElement("SIGORTALILAR");
        for(var i=0;i<snapshot.Rows.Length;i++)
        {
            var row=snapshot.Rows[i];var person=people.Single(p=>p.TeacherId==row.Teacher.Id);var t=row.Teacher;
            if(!Regex.IsMatch(t.IdentityNumber,"^[1-9][0-9]{10}$"))throw new ArgumentException(t.FullName+": TCKNO için 11 rakamlı T.C. kimlik numarası gerekli.");
            Text(person.GivenName,18,t.FullName+" / AD");Text(person.FamilyName,18,t.FullName+" / SOYAD");
            if(row.SgkError!="")throw new ArgumentException(t.FullName+": SGK gün bilgisi hatalı. "+row.SgkError);
            if(t.ModuleId!=ModuleId.TemelEgitim)throw new ArgumentException("Seçilen modülün SGK gün çekirdeği henüz eklenmedi.");
            var days=TeacherCalendar.CalculateMonth(snapshot.Year,snapshot.Month,t.StartDate,t.EndDate,row.Input.DayHours,row.Input.DykHours);
            if(row.SgkInformation is not null && snapshot.SgkRuleVersion==TeacherCalendar.RuleVersion && row.SgkInformation!=days)
                throw new ArgumentException(t.FullName+": kayıtlı SGK gün bilgisi tarihlerle uyuşmuyor; hakedişi düzenleyin.");
            var element=new XElement("SIGORTALI",new XAttribute("SIRA",i+1),new XAttribute("TCKNO",t.IdentityNumber),new XAttribute("AD",person.GivenName),new XAttribute("SOYAD",person.FamilyName));
            if(person.FirstSurname!=""){Text(person.FirstSurname,18,"ILKSOYAD");element.Add(new XAttribute("ILKSOYAD",person.FirstSurname));}
            if(row.Result.Gross<0)throw new ArgumentException(t.FullName+": PEK negatif olamaz.");
            element.Add(new XAttribute("PEK",row.Result.Gross.ToString("0.00",CultureInfo.InvariantCulture)));
            DecimalAttribute(element,"PRIM_IKRAMIYE",person.Bonus,row.Result.Gross);
            element.Add(new XAttribute("GUN",days.PremiumDays));
            bool InMonth(DateTime? d)=>d is DateTime date && date.Year==snapshot.Year && date.Month==snapshot.Month;
            if(InMonth(t.StartDate))element.Add(new XAttribute("GIRISGUN",t.StartDate!.Value.ToString("ddMM",CultureInfo.InvariantCulture)));
            if(InMonth(t.EndDate))element.Add(new XAttribute("CIKISGUN",t.EndDate!.Value.ToString("ddMM",CultureInfo.InvariantCulture)));
            if(person.PaidLeaveDays!="")element.Add(new XAttribute("UCRETLIIZINGUN",Code(person.PaidLeaveDays,0,30,"UCRETLIIZINGUN")));
            DecimalAttribute(element,"UIPEK",person.PaidLeaveGross,99999999999m);
            if(days.MissingDays>0)
            {
                var reason=person.MissingReason!=""?person.MissingReason:workplace.DefaultMissingReason;
                element.Add(new XAttribute("EKSIKGUNNEDENI",Code(reason,1,26,t.FullName+" / Eksik gün nedeni").ToString("D2",CultureInfo.InvariantCulture)),new XAttribute("EKSIKGUNSAYISI",days.MissingDays));
            }
            if(InMonth(t.EndDate))element.Add(new XAttribute("ISTENCIKISNEDENI",Code(person.ExitReason,1,60,t.FullName+" / İşten çıkış nedeni").ToString("D2",CultureInfo.InvariantCulture)));
            Text(t.OccupationCode,9,t.FullName+" / MESLEKKOD");element.Add(new XAttribute("MESLEKKOD",t.OccupationCode));
            if(person.WorkedOnReport!="")element.Add(new XAttribute("RAPCALISTI",Code(person.WorkedOnReport,0,9,t.FullName+" / RAPCALISTI")));
            insured.Add(element);
        }
        var doc=new XDocument(new XDeclaration("1.0","utf-8",null),new XElement("AYLIKBILDIRGELER",w,
            new XElement("BORDRO",new XAttribute("DONEMAY",snapshot.Month),new XAttribute("DONEMYIL",snapshot.Year),new XAttribute("BELGEMAHIYET",workplace.DocumentNature)),
            new XElement("BILDIRGELER",new XAttribute("BELGETURU",workplace.DocumentType),new XAttribute("KANUN",workplace.LawCode),insured)));
        using var stream=new MemoryStream();
        using(var writer=XmlWriter.Create(stream,new XmlWriterSettings{Encoding=new UTF8Encoding(false),Indent=true,CloseOutput=false,NewLineChars="\r\n"}))doc.Save(writer);
        var bytes=stream.ToArray();Validate(bytes);return bytes;
    }
    public static void Validate(byte[] bytes)
    {
        var schemas=new XmlSchemaSet{XmlResolver=null};using var schema=OpenSchema();
        using(var reader=XmlReader.Create(schema,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null}))schemas.Add(null,reader);
        schemas.Compile();
        var settings=new XmlReaderSettings{Schemas=schemas,ValidationType=ValidationType.Schema,DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null};
        settings.ValidationEventHandler+=(_,e)=>throw new ArgumentException("SGK şema doğrulaması başarısız: "+e.Message);
        using var memory=new MemoryStream(bytes);using var validated=XmlReader.Create(memory,settings);while(validated.Read()){}
    }
    private static void Text(string value,int max,string name)
    {if(string.IsNullOrWhiteSpace(value) || value.Length>max)throw new ArgumentException($"{name}: 1–{max} karakter gerekli; değer kısaltılmaz.");}
    private static int Code(string value,int min,int max,string name)
    {if(!Regex.IsMatch(value,"^[0-9]+$") || !int.TryParse(value,NumberStyles.None,CultureInfo.InvariantCulture,out var n) || n<min || n>max)throw new ArgumentException($"{name}: {min}–{max} aralığında SGK kodu girin.");return n;}
    private static void DecimalAttribute(XElement e,string key,string value,decimal max)
    {
        if(value=="")return;
        if(!decimal.TryParse(value.Replace(',','.'),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var amount) || amount<0 || amount>max || decimal.Round(amount,2)!=amount)
            throw new ArgumentException(key+": geçerli ve en fazla iki ondalıklı tutar girin (binlik ayırıcı kullanmayın).");
        e.Add(new XAttribute(key,amount.ToString("0.00",CultureInfo.InvariantCulture)));
    }
}
