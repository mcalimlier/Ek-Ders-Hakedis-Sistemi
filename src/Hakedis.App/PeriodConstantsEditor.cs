using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Hakedis.Contracts;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.App;

public sealed class PeriodConstantsEditor : INotifyPropertyChanged
{
    private readonly Dictionary<string,string> values = [];
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Changed;
    private string Get([CallerMemberName] string key = "") => values.GetValueOrDefault(key, "");
    private void Set(string value,[CallerMemberName] string key = "")
    { if (Get(key) == value) return; values[key] = value; PropertyChanged?.Invoke(this,new(key)); Changed?.Invoke(); }
    public string Coefficient { get => Get(); set => Set(value); }
    public string Indicator { get => Get(); set => Set(value); }
    public string IncomeExemption { get => Get(); set => Set(value); }
    public string StampExemption { get => Get(); set => Set(value); }
    public string EmployeeRate { get => Get(); set => Set(value); }
    public string EmployerRate { get => Get(); set => Set(value); }
    public string IncomeRate { get => Get(); set => Set(value); }
    public string StampRate { get => Get(); set => Set(value); }
    public string DykMultiplier { get => Get(); set => Set(value); }
    public PeriodConstantsEditor(PeriodValues source)
    {
        foreach (var p in typeof(PeriodValues).GetProperties().Where(p => p.PropertyType == typeof(decimal)))
            values[p.Name] = ((decimal)p.GetValue(source)!).ToString(CultureInfo.GetCultureInfo("tr-TR"));
    }
    public static decimal Number(string text)
    {
        if (!decimal.TryParse(text,NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.GetCultureInfo("tr-TR"),out var value))
            throw new ArgumentException("Geçerli bir sayı girin. Ondalık ayırıcı virgüldür; binlik ayırıcı kullanmayın.");
        return value;
    }
    public PeriodValues Read() => new(Number(Coefficient),Number(Indicator),Number(IncomeExemption),Number(StampExemption),
        Number(EmployeeRate),Number(EmployerRate),Number(IncomeRate),Number(StampRate),Number(DykMultiplier));
    public TeacherParameters Parameters()
    {
        var p = Read(); return new(p.Coefficient,p.Indicator,p.DykMultiplier,p.EmployeeRate,p.EmployerRate,
            p.IncomeRate,p.StampRate,p.IncomeExemption,p.StampExemption);
    }
}
