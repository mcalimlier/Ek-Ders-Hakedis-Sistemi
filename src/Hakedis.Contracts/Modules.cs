namespace Hakedis.Contracts;

public enum ModuleId { TemelEgitim, ImamHatipLisesi, Ortaogretim, MeslekiEgitim, HalkEgitimUstaOgretici }

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
    public IReadOnlyList<ModuleDescriptor> Catalog => Enum.GetValues<ModuleId>().Select(id =>
        new ModuleDescriptor(id, id switch
        {
            ModuleId.TemelEgitim => "Temel Eğitim",
            ModuleId.ImamHatipLisesi => "İmam Hatip Lisesi",
            ModuleId.Ortaogretim => "Ortaöğretim",
            ModuleId.MeslekiEgitim => "Mesleki Eğitim",
            _ => "Halk Eğitim Usta Öğreticileri"
        }, modules.ContainsKey(id))).ToArray();
}
