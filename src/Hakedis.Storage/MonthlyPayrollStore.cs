using Hakedis.Contracts;

namespace Hakedis.Storage;

public sealed class MonthlyPayrollStore(string root)
{
    public const string Rule = "aylik-girdiler.v1";
    private readonly JsonDraftStore store = new();
    public string Folder(ModuleId module) => Path.Combine(root, "AylikKayitlar", module.ToString());
    public string FilePath(ModuleId module, int year, int month)
    {
        if (!Enum.IsDefined(module) || year is < 1900 or > 9999 || month is < 1 or > 12) throw new ArgumentException("Dönem veya modül geçersiz.");
        return Path.Combine(Folder(module), $"{year:D4}-{month:D2}.json");
    }
    public MonthlyDocument Read(ModuleId module, int year, int month)
    {
        var path = FilePath(module,year,month);
        if (!File.Exists(path)) return new(year,month,PeriodValues.ForModule(module),[]);
        var doc = store.Load<MonthlyDocument>(path,module,Rule).Data;
        Validate(doc,year,month); return doc;
    }
    public void Save(ModuleId module, MonthlyDocument data) => Update(module,data.Year,data.Month,_=>data);
    private void Write(ModuleId module, MonthlyDocument data)
    {
        Validate(data,data.Year,data.Month);
        store.Save(FilePath(module,data.Year,data.Month), new DraftDocument<MonthlyDocument>(1,module,Rule,DateTimeOffset.UtcNow,data));
    }
    public void Update(ModuleId module,int year,int month,Func<MonthlyDocument,MonthlyDocument> update)
    {
        var path=FilePath(module,year,month);Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var held=new FileStream(path+".transaction.lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var document=update(Read(module,year,month));Validate(document,year,month);Write(module,document);
    }
    private static void Validate(MonthlyDocument data,int year,int month)
    {
        if (data.Year != year || data.Month != month || data.Constants is null || data.Entries is null ||
            data.Entries.Any(e => e is null || e.TeacherId == Guid.Empty || e.DayHours < 0 || e.DykHours < 0 || e.Garnishment < 0) ||
            data.Entries.Select(e => e.TeacherId).Distinct().Count() != data.Entries.Length)
            throw new InvalidDataException("Aylık kayıt dönemi veya girdileri geçersiz; dosya değiştirilmedi.");
        foreach(var entry in data.Entries)entry.ValidateSchoolHours();
        var p=data.Constants;
        if(p.Coefficient<=0 || p.Indicator<=0 || p.DykMultiplier<=0 || p.IncomeExemption<0 || p.StampExemption<0 ||
            new[]{p.EmployeeRate,p.EmployerRate,p.IncomeRate,p.StampRate}.Any(x=>x<0 || x>1))
            throw new InvalidDataException("Dönem sabitleri geçersiz; dosya değiştirilmedi.");
    }
}
