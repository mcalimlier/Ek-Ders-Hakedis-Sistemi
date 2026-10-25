namespace Hakedis.Contracts;

public enum ModuleId { TemelEgitim, ImamHatipLisesi, Ortaogretim, MeslekiEgitim, HalkEgitimUstaOgretici, DinOgretimi, HalkEgitimEmekliUstaOgretici }

// Ortak sözleşme hesaplama alanlarını dayatmaz. Her çekirdek kendi girdi ve sonucuna sahiptir.
public interface ICalculationModule
{
    ModuleId Id { get; }
    string RuleVersion { get; }
}

public sealed record ModuleDescriptor(ModuleId Id, string Name, bool IsAvailable)
{
    public string DisplayName => IsAvailable ? Name : $"{Name} · Henüz eklenmedi";
}

public sealed class ModuleRegistry
{
    private readonly Dictionary<ModuleId, ICalculationModule> modules;
    public ModuleRegistry(IEnumerable<ICalculationModule> implementations)
    {
        modules = implementations.ToDictionary(m => m.Id);
    }
    public T Resolve<T>(ModuleId id) where T : class, ICalculationModule
    {
        if (!modules.TryGetValue(id, out var module))
            throw new NotSupportedException("Seçilen modülün hesaplama çekirdeği henüz eklenmedi.");
        return module as T ?? throw new InvalidOperationException("Modül ile hesaplama sözleşmesi uyuşmuyor.");
    }
    // Eski enum değeri dosya uyumluluğu için ayrılmıştır; Din Öğretimi bu kapsamı karşılar.
    public IReadOnlyList<ModuleDescriptor> Catalog => Enum.GetValues<ModuleId>().Where(id=>id!=ModuleId.ImamHatipLisesi).Select(id =>
        new ModuleDescriptor(id, id switch
        {
            ModuleId.TemelEgitim => "Temel Eğitim",
            ModuleId.DinOgretimi => "Din Öğretimi",
            ModuleId.ImamHatipLisesi => "İmam Hatip Lisesi",
            ModuleId.Ortaogretim => "Ortaöğretim",
            ModuleId.MeslekiEgitim => "Mesleki Eğitim",
            ModuleId.HalkEgitimEmekliUstaOgretici => "Halk Eğitim Merkezi Emekli Usta Öğretici",
            _ => "Halk Eğitim Merkezi Usta Öğretici"
        }, modules.ContainsKey(id))).ToArray();
}

public static class ModuleLabels
{
    public static string Role(ModuleId module)=>module==ModuleId.HalkEgitimEmekliUstaOgretici?"Emekli Usta Öğretici":module==ModuleId.HalkEgitimUstaOgretici?"Usta Öğretici":"Ücretli Öğretmen";
    public static string PayrollTitle(ModuleId module)=>module==ModuleId.HalkEgitimEmekliUstaOgretici
        ?"DERS ÜCRETİ KARŞILIĞINDA GÖREVLENDİRİLENLER BORDROSU ( EMEKLİ USTA ÖĞRETİCİ)"
        :module==ModuleId.HalkEgitimUstaOgretici
        ?"DERS ÜCRETİ KARŞILIĞINDA GÖREVLENDİRİLENLER BORDROSU (USTA ÖĞRETİCİ)"
        :"DERS ÜCRETİ KARŞILIĞINDA GÖREVLENDİRİLENLER BORDROSU (ÜCRETLİ ÖĞRETMEN)";
    private static string Percent(decimal rate)=>(rate*100).ToString("0.####",System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
    public static string EmployeeHeader(decimal rate)=>rate==.14m?ExcelHeadings.BordroW6:rate==.075m?"S G K \n(% 7,5)\nİŞTİRAKÇİ":$"S G K \n(%{Percent(rate)})\nİŞTİRAKÇİ";
    public static string EmployerHeader(decimal rate)=>rate==.2175m?ExcelHeadings.BordroX6:$"S G K (%{Percent(rate)})\nİŞVEREN\n";
    public static string EmployerContributionHeader(decimal rate)=>rate==.2175m?ExcelHeadings.BordroO6:$"Devlet Sigorta Katk.%{Percent(rate)}";
}
