using System.Globalization;
using System.Security.Cryptography;
using Hakedis.Contracts;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.Storage;

public sealed record ArchivedPayment(string Path,string Version,DraftDocument<PaymentSnapshot> Document)
{
    public string Period=>$"{CultureInfo.GetCultureInfo("tr-TR").DateTimeFormat.GetMonthName(Document.Data.Month)}-{Document.Data.Year}";
    private static readonly IReadOnlyDictionary<ModuleId,string> ModuleNames=new ModuleRegistry([new TeacherPayrollModule()]).Catalog.ToDictionary(m=>m.Id,m=>m.Name);
    public string Module=>ModuleNames[Document.ModuleId];
    public int Count=>Document.Data.Rows.Length;
    public decimal Net=>Document.Data.Rows.Sum(r=>r.Result.Net);
    public DateTimeOffset SavedAt=>Document.SavedAt;
}

public sealed class PaymentArchiveStore(string root)
{
    public IReadOnlyList<ArchivedPayment> ReadAll()
    {
        var folder=System.IO.Path.Combine(root,"AylikKayitlar");if(!Directory.Exists(folder))return [];
        return Directory.EnumerateFiles(folder,"hakediş-*.json",SearchOption.AllDirectories).Select(Read).OrderByDescending(x=>x.SavedAt).ToArray();
    }
    public ArchivedPayment Read(string path)
    {
        var full=System.IO.Path.GetFullPath(path);var allowed=System.IO.Path.GetFullPath(System.IO.Path.Combine(root,"AylikKayitlar"))+System.IO.Path.DirectorySeparatorChar;
        if(!full.StartsWith(allowed,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Hakediş kayıt yolu geçersiz.");
        var moduleName=new DirectoryInfo(System.IO.Path.GetDirectoryName(full)!).Name;
        if(!Enum.TryParse<ModuleId>(moduleName,out var module) || !Enum.IsDefined(module))throw new InvalidDataException("Hakediş modülü geçersiz.");
        // Hash ve içerik aynı dosya sürümünden okunur; başka süreç arada değiştiremez.
        using var held=new FileStream(full,FileMode.Open,FileAccess.Read,FileShare.Read);
        var doc=new JsonDraftStore().Load<PaymentSnapshot>(full,module,new TeacherPayrollModule().RuleVersion);
        Validate(doc.Data,module);
        return new(full,Convert.ToHexString(SHA256.HashData(held)),doc);
    }
    public static void Validate(PaymentSnapshot data,ModuleId module)
    {
        if(data is null || data.Year is <1900 or >9999 || data.Month is <1 or >12 || data.Constants is null || data.Rows is null || data.Rows.Length==0 ||
           data.Rows.Any(r=>r is null || r.Teacher is null || r.Input is null || r.Result is null || r.Teacher.ModuleId!=module || r.Teacher.Id!=r.Input.TeacherId) ||
           data.Rows.Select(r=>r.Teacher.Id).Distinct().Count()!=data.Rows.Length)
            throw new InvalidDataException("Hakediş kaydı eksik veya tutarsız.");
        foreach(var row in data.Rows)
        {
            _=TeacherDirectory.Normalize(row.Teacher);
            if(row.Input.DayHours<0 || row.Input.DykHours<0 || row.Input.Garnishment<0 || row.Result.Net!=row.Result.Accrual-row.Result.Deductions)
                throw new InvalidDataException("Hakediş satırının tutarları tutarsız.");
        }
    }
    public bool Replace(ArchivedPayment original,PaymentSnapshot data,bool confirmed)
    {
        if(!confirmed)return false;Validate(data,original.Document.ModuleId);
        if(data.Year!=original.Document.Data.Year || data.Month!=original.Document.Data.Month)throw new InvalidOperationException("Geçmiş kaydın dönemi değiştirilemez.");
        new JsonDraftStore().SaveChecked(original.Path,original.Document with {Data=data,SavedAt=DateTimeOffset.UtcNow},original.Version);return true;
    }
    public PersonnelMovement Movement(ModuleId module,int year,int month,IReadOnlyList<PaymentRow> rows)
    {
        var prior=new DateTime(year,month,1).AddMonths(-1);
        var previous=ReadAll().FirstOrDefault(x=>x.Document.ModuleId==module && x.Document.Data.Year==prior.Year && x.Document.Data.Month==prior.Month)?.Document.Data.Rows??[];
        var old=previous.Select(r=>r.Teacher.Id).ToHashSet();var current=rows.Select(r=>r.Teacher.Id).ToHashSet();
        return new(old.Count,current.Except(old).Count(),old.Except(current).Count(),current.Except(old).ToArray());
    }
}
