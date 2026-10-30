using System.Globalization;
using System.Security.Cryptography;
using Hakedis.Contracts;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.Storage;

public sealed record ArchivedPayment(string Path,string Version,DraftDocument<PaymentSnapshot> Document)
{
    public string Period=>$"{CultureInfo.GetCultureInfo("tr-TR").DateTimeFormat.GetMonthName(Document.Data.Month)}-{Document.Data.Year}";
    private static readonly IReadOnlyDictionary<ModuleId,string> ModuleNames=PayrollCores.Catalog.ToDictionary(m=>m.Id,m=>m.Name);
    public string Module=>ModuleNames[Document.ModuleId];
    public int Count=>Document.Data.Rows.Length;
    public decimal Net=>Document.Data.Rows.Sum(r=>r.Result.Net);
    public DateTimeOffset SavedAt=>Document.SavedAt;
}

public sealed class PaymentArchiveStore(string root)
{
    // Tüm modüller için kişi/yıl/ay tek bordro kontrolü aynı yazma kilidi altında yapılır.
    public IDisposable BeginIdentityWrite(PaymentSnapshot data,string? excludingPath=null)
    {
        _=new DateTime(data.Year,data.Month,1);
        var folder=System.IO.Path.Combine(root,"AylikKayitlar");Directory.CreateDirectory(folder);
        FileStream guard;
        try{guard=new FileStream(System.IO.Path.Combine(folder,$"bordro-kisiler-{data.Year:D4}-{data.Month:D2}.guard"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
        catch(IOException ex){throw new InvalidOperationException("Bu dönemin bordro kişi kontrolü başka bir pencerede yapılıyor. Tekrar deneyin.",ex);}
        try
        {
            var identities=data.Rows.Where(r=>!string.IsNullOrWhiteSpace(r.Teacher.IdentityNumber)).Select(r=>r.Teacher.IdentityNumber).ToHashSet();
            var ids=data.Rows.Select(r=>r.Teacher.Id).ToHashSet();
            var duplicate=ReadAll().Where(r=>r.Document.Data.Year==data.Year && r.Document.Data.Month==data.Month &&
                !string.Equals(r.Path,excludingPath,StringComparison.OrdinalIgnoreCase))
                .SelectMany(record=>record.Document.Data.Rows.Select(row=>(record,row)))
                .FirstOrDefault(item=>ids.Contains(item.row.Teacher.Id) || identities.Contains(item.row.Teacher.IdentityNumber));
            if(duplicate.record is not null)
                throw new InvalidOperationException($"{duplicate.row.Teacher.FullName} aynı dönemde {duplicate.record.Module} bordrosunda kayıtlı. Bir kişi için ayda yalnız bir bordro oluşturulabilir. Önce kayıtlı bordroyu kontrol edin.");
            return guard;
        }
        catch{guard.Dispose();throw;}
    }
    private FileStream LockPeriod(ModuleId module,int year,int month)
    {
        if(!Enum.IsDefined(module))throw new ArgumentException("Hakediş modülü geçersiz.");
        var folder=new MonthlyPayrollStore(root).Folder(module);
        _=new DateTime(year,month,1);Directory.CreateDirectory(folder);
        FileStream guard;
        try{guard=new FileStream(System.IO.Path.Combine(folder,$"hakediş-{year:D4}-{month:D2}.guard"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
        catch(IOException ex){throw new InvalidOperationException("Bu dönem başka bir pencerede kaydediliyor. Çıktı listesini yenileyin.",ex);}
        return guard;
    }
    public IDisposable BeginNewPeriod(ModuleId module,int year,int month)
    {
        var guard=LockPeriod(module,year,month);
        try
        {
            if(ReadAll().Any(r=>r.Document.ModuleId==module && r.Document.Data.Year==year && r.Document.Data.Month==month))
                throw new InvalidOperationException("Bu modül ve dönem için kayıtlı hakediş var. İkinci kayıt oluşturulamaz; Çıktı panelindeki Düzenle düğmesini kullanın.");
            return guard;
        }
        catch{guard.Dispose();throw;}
    }
    public bool Delete(ArchivedPayment original,bool confirmed)
    {
        if(!confirmed)return false;
        var record=Read(original.Path);
        using var period=LockPeriod(record.Document.ModuleId,record.Document.Data.Year,record.Document.Data.Month);
        using var write=new FileStream(record.Path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        record=Read(record.Path);
        if(record.Version!=original.Version)throw new InvalidOperationException("Hakediş kaydı değişmiş. Silmeden önce Çıktı listesini yenileyin.");
        // Aktif kayıttan tek hareketle çıkarılır. Kurtarma kopyaları Çıktı listesinde taranmaz.
        var recovery=System.IO.Path.Combine(root,"SilinenHakedisler",record.Document.ModuleId.ToString());Directory.CreateDirectory(recovery);
        File.Move(record.Path,System.IO.Path.Combine(recovery,$"{System.IO.Path.GetFileNameWithoutExtension(record.Path)}-{Guid.NewGuid():N}.json"));
        return true;
    }
    public IReadOnlyList<ArchivedPayment> ReadAll()
    {
        var folder=System.IO.Path.Combine(root,"AylikKayitlar");if(!Directory.Exists(folder))return [];
        return Directory.EnumerateFiles(folder,"hakediş-*.json",SearchOption.AllDirectories).Select(Read).OrderByDescending(x=>x.SavedAt).ToArray();
    }
    // For display only. Identity and period checks must keep using the strict ReadAll.
    public IReadOnlyList<ArchivedPayment> ReadAvailable(out IReadOnlyList<string> errors)
    {
        var folder=System.IO.Path.Combine(root,"AylikKayitlar");
        var records=new List<ArchivedPayment>();var failures=new List<string>();
        void Report(string path,Exception error)
            => failures.Add(System.IO.Path.GetRelativePath(root,path)+": "+error.Message);
        void Scan(string directory)
        {
            string[] files;
            try{files=Directory.GetFiles(directory,"hakediş-*.json",SearchOption.TopDirectoryOnly);}
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){Report(directory,ex);return;}
            foreach(var file in files)
            {
                try{records.Add(Read(file));}
                catch(Exception ex) when(ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException){Report(file,ex);}
            }
            string[] children;
            try{children=Directory.GetDirectories(directory);}
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){Report(directory,ex);return;}
            foreach(var child in children)
            {
                try{if((File.GetAttributes(child)&FileAttributes.ReparsePoint)==0)Scan(child);}
                catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){Report(child,ex);}
            }
        }
        if(Directory.Exists(folder))Scan(folder);
        errors=failures.ToArray();
        return records.OrderByDescending(x=>x.SavedAt).ToArray();
    }
    public ArchivedPayment Read(string path)
    {
        var full=System.IO.Path.GetFullPath(path);var allowed=System.IO.Path.GetFullPath(System.IO.Path.Combine(root,"AylikKayitlar"))+System.IO.Path.DirectorySeparatorChar;
        if(!full.StartsWith(allowed,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Hakediş kayıt yolu geçersiz.");
        var moduleName=new DirectoryInfo(System.IO.Path.GetDirectoryName(full)!).Name;
        if(!Enum.TryParse<ModuleId>(moduleName,out var module) || !Enum.IsDefined(module))throw new InvalidDataException("Hakediş modülü geçersiz.");
        // Hash ve içerik aynı dosya sürümünden okunur; başka süreç arada değiştiremez.
        using var held=new FileStream(full,FileMode.Open,FileAccess.Read,FileShare.Read);
        var doc=new JsonDraftStore().Load<PaymentSnapshot>(full,module,PayrollCores.RuleVersion(module));
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
            row.Input.ValidateSchoolHours();
            if(row.Input.DayHours<0 || row.Input.DykHours<0 || row.Input.Garnishment<0 || row.Result.Net!=row.Result.Accrual-row.Result.Deductions)
                throw new InvalidDataException("Hakediş satırının tutarları tutarsız.");
            if(PayrollCores.IsAvailable(module))
            {
                try
                {
                    var p=data.Constants;
                    var calculated=PayrollCores.Calculate(module,new(row.Input.DayHours,row.Input.DykHours,row.Input.Garnishment),
                        new(p.Coefficient,p.Indicator,p.DykMultiplier,p.EmployeeRate,p.EmployerRate,p.IncomeRate,p.StampRate,p.IncomeExemption,p.StampExemption));
                    if(calculated!=row.Result)throw new InvalidDataException("Hakediş tutarları kayıtlı girdiler ve dönem sabitleriyle uyuşmuyor; kaynak dosya korunuyor.");
                }
                catch(Exception ex) when(ex is ArgumentException or OverflowException)
                {throw new InvalidDataException("Hakediş girdileri veya dönem sabitleri geçersiz; kaynak dosya korunuyor.",ex);}
            }
        }
        if(data.Rows.Where(r=>r.Teacher.IdentityNumber!="").GroupBy(r=>r.Teacher.IdentityNumber).Any(g=>g.Count()>1))
            throw new InvalidDataException("Hakedişte yinelenen T.C. kimlik numarası var.");
    }
    public bool Replace(ArchivedPayment original,PaymentSnapshot data,bool confirmed)
    {
        if(!confirmed)return false;Validate(data,original.Document.ModuleId);
        if(data.Year!=original.Document.Data.Year || data.Month!=original.Document.Data.Month)throw new InvalidOperationException("Geçmiş kaydın dönemi değiştirilemez.");
        using var identity=BeginIdentityWrite(data,original.Path);
        new JsonDraftStore().SaveChecked(original.Path,original.Document with {Data=data,SavedAt=DateTimeOffset.UtcNow},original.Version);return true;
    }
    public PersonnelMovement Movement(ModuleId module,int year,int month,IReadOnlyList<PaymentRow> rows)
    {
        var prior=new DateTime(year,month,1).AddMonths(-1);
        var previous=ReadAll().FirstOrDefault(x=>x.Document.ModuleId==module && x.Document.Data.Year==prior.Year && x.Document.Data.Month==prior.Month)?.Document.Data.Rows??[];
        // Listeye yeni eklenmek işe yeni başlamak anlamına gelmez. Tarih ve yıl birlikte eşleşir.
        var teachers=previous.Select(r=>r.Teacher).Concat(new TeacherDirectory(System.IO.Path.Combine(root,"ogretmenler.json"),module).ReadForPeriod(year,month))
            .Concat(rows.Select(r=>r.Teacher))
            .GroupBy(t=>t.Id).Select(g=>g.Last()).Where(t=>t.ModuleId==module).ToArray();
        bool InMonth(DateTime? date)=>date is DateTime d && d.Year==year && d.Month==month;
        var entered=teachers.Where(t=>InMonth(t.StartDate)).Select(t=>t.Id).ToArray();
        return new(previous.Length,entered.Length,teachers.Count(t=>InMonth(t.EndDate)),entered,"employment-dates.v2");
    }
}
