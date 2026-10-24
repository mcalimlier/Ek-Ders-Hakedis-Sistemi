using System.Text.Json;
using Hakedis.Contracts;

namespace Hakedis.Storage;

// İki dosya önce ayrı alanda doğrulanır. Kalıcı günlük yarım kalan işlemi yeniden açılışta tamamlar.
public sealed class PayrollEditStore(string teacherPath)
{
    private readonly string catalog=Path.GetFullPath(teacherPath);
    private string Root=>Path.GetDirectoryName(catalog)!;
    private string Journal=>Path.Combine(Root,"hibrit-islem.json");
    public sealed record BatchJournal(bool Rollback,FileChange[] Files);
    public sealed record FileChange(string Path, byte[]? Before, byte[] After);
    private static FileStream Hold(string path)
    {Directory.CreateDirectory(Path.GetDirectoryName(path)!);return new(path,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
    private static void AtomicWrite(string path,byte[] bytes,bool backup)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);var tmp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            using(var stream=new FileStream(tmp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {stream.Write(bytes);stream.Flush(true);}
            if(File.Exists(path))File.Replace(tmp,path,backup?path+".bak":null);else File.Move(tmp,path);
        }
        finally{if(File.Exists(tmp))File.Delete(tmp);}
    }
    public void Recover()
    {
        if(!File.Exists(Journal))return;
        using var global=Hold(Journal+".lock");RecoverCore();
    }
    private void RecoverCore()
    {
        if(!File.Exists(Journal))return;
        BatchJournal journal;
        try{journal=JsonSerializer.Deserialize<BatchJournal>(File.ReadAllBytes(Journal))??throw new JsonException();
            if(journal.Files is null || journal.Files.Length!=2 || journal.Files.Any(c=>c is null || string.IsNullOrWhiteSpace(c.Path) || c.After is null))throw new JsonException();}
        catch(JsonException ex){throw new InvalidDataException("Yarım kalan kayıt günlüğü okunamadı; dosyalar korunuyor.",ex);}
        var changes=journal.Files;
        foreach(var c in changes)
        {
            var target=Path.GetFullPath(c.Path);
            if(!target.StartsWith(Root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase) ||
               (target!=catalog && !target.StartsWith(Path.Combine(Root,"AylikKayitlar")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("İşlem günlüğünün dosya yolu geçersiz.");
        }
        using var teacherLock=Hold(catalog+".lock");
        var held=new List<FileStream>();
        try
        {
            foreach(var c in changes.Where(c=>c.Path!=catalog))
            {held.Add(Hold(c.Path+".transaction.lock"));held.Add(Hold(c.Path+".lock"));}
            foreach(var c in changes)
                if(journal.Rollback)
                {if(c.Before is null){if(File.Exists(c.Path))File.Delete(c.Path);}else AtomicWrite(c.Path,c.Before,false);}
                else AtomicWrite(c.Path,c.After,File.Exists(c.Path) && c.Before is not null && File.ReadAllBytes(c.Path).SequenceEqual(c.Before));
            File.Delete(Journal);
        }
        finally{foreach(var h in held)h.Dispose();}
    }
    public void Save(ModuleId module,int year,int month,IReadOnlyList<TeacherRecord> expected,
        IReadOnlyList<TeacherRecord> replacements,IReadOnlyList<MonthlyInput> inputs,PeriodValues constants,
        IReadOnlyDictionary<Guid,MonthlyInput?>? expectedInputs=null)
    {
        using var global=Hold(Journal+".lock");RecoverCore();
        var live=new MonthlyPayrollStore(Root);var monthly=live.FilePath(module,year,month);
        using var teacherLock=Hold(catalog+".lock");
        using var monthTransaction=Hold(monthly+".transaction.lock");using var monthLock=Hold(monthly+".lock");
        var current=live.Read(module,year,month);
        if(expectedInputs is not null)
            foreach(var pair in expectedInputs)
                if(current.Entries.SingleOrDefault(e=>e.TeacherId==pair.Key)!=pair.Value)
                    throw new InvalidOperationException("Aylık kayıt başka bir ekranda değişmiş. Kayıtları yenileyin.");
        var stage=Path.Combine(Root,".hibrit-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);
        try
        {
            var stagedCatalog=Path.Combine(stage,"ogretmenler.json");
            if(File.Exists(catalog))File.Copy(catalog,stagedCatalog);
            var teachers=new TeacherDirectory(stagedCatalog,module).UpdateBatch(expected,replacements);
            if(inputs.Any(i=>!teachers.Any(t=>t.Id==i.TeacherId)))throw new InvalidOperationException("Öğretmen kaydı artık bulunmuyor.");
            var stagedStore=new MonthlyPayrollStore(stage);
            stagedStore.Save(module,new(year,month,constants,current.Entries.Where(e=>inputs.All(i=>i.TeacherId!=e.TeacherId)).Concat(inputs).ToArray()));
            var changes=new[]{new FileChange(catalog,File.Exists(catalog)?File.ReadAllBytes(catalog):null,File.ReadAllBytes(stagedCatalog)),
                new FileChange(monthly,File.Exists(monthly)?File.ReadAllBytes(monthly):null,File.ReadAllBytes(stagedStore.FilePath(module,year,month)))};
            AtomicWrite(Journal,JsonSerializer.SerializeToUtf8Bytes(new BatchJournal(false,changes)),false);
            try
            {
                foreach(var c in changes)AtomicWrite(c.Path,c.After,true);
                File.Delete(Journal);
            }
            catch
            {
                // Geri alma kararı da kalıcıdır; ikinci bir kesinti açılışta aynı kararı sürdürür.
                AtomicWrite(Journal,JsonSerializer.SerializeToUtf8Bytes(new BatchJournal(true,changes)),false);
                foreach(var c in changes)
                    if(c.Before is null){if(File.Exists(c.Path))File.Delete(c.Path);}else AtomicWrite(c.Path,c.Before,false);
                File.Delete(Journal);throw;
            }
        }
        finally
        {
            // Üretilen dizin yalnız bu işlemindir; mutlak sınır denetlenir.
            var full=Path.GetFullPath(stage);
            if(full.StartsWith(Root+Path.DirectorySeparatorChar+".hibrit-",StringComparison.OrdinalIgnoreCase))Directory.Delete(full,true);
        }
    }
}
