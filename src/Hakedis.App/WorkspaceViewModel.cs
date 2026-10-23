using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Hakedis.Contracts;
using Hakedis.Storage;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.App;

public sealed record TeacherDraft(int Year, int Month, string Person, TeacherInput Input, TeacherParameters Parameters);
public sealed record Detail(string Name, string Value);

public sealed class WorkspaceViewModel : INotifyPropertyChanged
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private readonly ModuleRegistry registry = new([new TeacherPayrollModule()]);
    private readonly JsonDraftStore store = new();
    private readonly Dictionary<string, string> fields = [];
    private ModuleDescriptor? selected;
    public event PropertyChangedEventHandler? PropertyChanged;
    public IReadOnlyList<ModuleDescriptor> Modules => registry.Catalog;
    public ObservableCollection<Detail> Details { get; } = [];
    public ModuleDescriptor? SelectedModule
    {
        get => selected;
        set { selected = value; Clear(); Status = IsAvailable ? "Temel Eğitim seçili. Ödeme dönemini ve ders bilgilerini girin." : "Bu modül henüz kullanıma açılmadı. Hesaplama yapılamaz."; Notify(); Notify(nameof(IsAvailable)); Notify(nameof(Status)); }
    }
    public bool IsAvailable => selected?.IsAvailable == true;
    public string Status { get; private set; } = "";
    public string GrossText { get; private set; } = "—";
    public string AccrualText { get; private set; } = "—";
    public string NetText { get; private set; } = "—";
    public string Year { get => Get(); set => Set(value); }
    public string Month { get => Get(); set => Set(value); }
    public string Person { get => Get(); set => Set(value); }
    public string DayHours { get => Get(); set => Set(value); }
    public string DykHours { get => Get(); set => Set(value); }
    public string Garnishment { get => Get(); set => Set(value); }
    public string Coefficient { get => Get(); set => Set(value); }
    public string Indicator { get => Get(); set => Set(value); }
    public string IncomeExemption { get => Get(); set => Set(value); }
    public string StampExemption { get => Get(); set => Set(value); }
    public WorkspaceViewModel()
    {
        Year = "2026"; Month = "9"; Person = ""; DayHours = "0"; DykHours = "0"; Garnishment = "0";
        Coefficient = "1,575512"; Indicator = "140"; IncomeExemption = "28075,50"; StampExemption = "33030";
        SelectedModule = Modules.Single(m => m.Id == ModuleId.TemelEgitim);
    }
    private string Get([CallerMemberName] string key = "") => fields.GetValueOrDefault(key, "");
    private void Set(string value, [CallerMemberName] string key = "")
    {
        if (Get(key) == value) return;
        fields[key] = value; Clear(); Notify(key);
        Status = "Girdiler değişti. Sonuç için yeniden hesaplayın."; Notify(nameof(Status));
    }
    private void Notify([CallerMemberName] string key = "") => PropertyChanged?.Invoke(this, new(key));
    private void Clear()
    {
        GrossText = AccrualText = NetText = "—"; Details.Clear();
        Notify(nameof(GrossText)); Notify(nameof(AccrualText)); Notify(nameof(NetText));
    }
    public void ShowError(string message) { Clear(); Status = message; Notify(nameof(Status)); }
    private TeacherPayrollModule CurrentModule()
        => registry.Resolve<TeacherPayrollModule>(selected?.Id ?? throw new InvalidOperationException("Modül seçin."));
    private static decimal Number(string text, string label)
    {
        // Binlik ayırıcı kabul edilmez: 1.575512'nin 1575512 olarak okunmasını önler.
        if (!decimal.TryParse(text.Trim(), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, Tr, out var value))
            throw new ArgumentException($"{label}: geçerli bir sayı girin; ondalık ayırıcı olarak virgül kullanın, binlik ayırıcı kullanmayın.");
        return value;
    }
    private TeacherDraft ReadDraft()
    {
        _ = CurrentModule();
        if (!int.TryParse(Year, out var year) || year < 1900 || year > 9999 || !int.TryParse(Month, out var month) || month < 1 || month > 12)
            throw new ArgumentException("Geçerli yıl ve 1–12 arasında ay girin.");
        if (string.IsNullOrWhiteSpace(Person)) throw new ArgumentException("Personel adı veya kayıt etiketi girin.");
        var parameters = TeacherParameters.SourceSeptember2026 with
        {
            Coefficient = Number(Coefficient, "Katsayı"), Indicator = Number(Indicator, "Gösterge"),
            IncomeExemptBase = Number(IncomeExemption, "GV istisnası"), StampExemptBase = Number(StampExemption, "DV istisnası")
        };
        return new(year, month, Person.Trim(), new(Number(DayHours, "Gündüz saati"), Number(DykHours, "DYK saati"), Number(Garnishment, "İcra")), parameters);
    }
    public void Calculate()
    {
        Clear(); var draft = ReadDraft(); var module = CurrentModule();
        var result = module.Calculate(draft.Input, draft.Parameters);
        GrossText = Money(result.Gross); AccrualText = Money(result.Accrual); NetText = Money(result.Net);
        Notify(nameof(GrossText)); Notify(nameof(AccrualText)); Notify(nameof(NetText));
        Details.Add(new("Gündüz saat ücreti", result.DayRate.ToString("N5", Tr)));
        Details.Add(new("DYK saat ücreti", result.DykRate.ToString("N5", Tr)));
        Details.Add(new("Gündüz brüt", Money(result.DayGross))); Details.Add(new("DYK brüt", Money(result.DykGross)));
        Details.Add(new("Prim gün sayısı", result.PremiumDays.ToString(Tr)));
        Details.Add(new("Çalışan SGK", Money(result.EmployeePremium))); Details.Add(new("İşveren SGK", Money(result.EmployerPremium)));
        Details.Add(new("GV matrahı", Money(result.IncomeBase))); Details.Add(new("Gelir vergisi", Money(result.IncomeTax)));
        Details.Add(new("Damga vergisi", Money(result.StampTax))); Details.Add(new("İcra", Money(result.Garnishment)));
        Details.Add(new("Bordro kesinti toplamı (işveren dahil)", Money(result.Deductions)));
        Status = $"{draft.Person} · {draft.Month:D2}/{draft.Year} dönemi hakedişi hesaplandı. Ödeme öncesinde dönem parametrelerini kontrol edin.";
        if (result.Net < 0) Status += " UYARI: Net tutar negatif.";
        Notify(nameof(Status));
    }
    private static string Money(decimal value) => value.ToString("N2", Tr) + " TL";
    public void Save(string path)
    {
        var draft = ReadDraft(); _ = CurrentModule().Calculate(draft.Input, draft.Parameters);
        store.Save(path, new DraftDocument<TeacherDraft>(1, selected!.Id, CurrentModule().RuleVersion, DateTimeOffset.UtcNow, draft));
        Status = "Taslak kaydedildi. Önceki sağlam kayıt varsa .bak dosyasında korundu."; Notify(nameof(Status));
    }
    public void Open(string path)
    {
        var module = registry.Resolve<TeacherPayrollModule>(ModuleId.TemelEgitim);
        var draft = store.Load<TeacherDraft>(path, ModuleId.TemelEgitim, module.RuleVersion).Data;
        if (draft.Input is null || draft.Parameters is null || string.IsNullOrWhiteSpace(draft.Person) || draft.Year is < 1900 or > 9999 || draft.Month is < 1 or > 12)
            throw new System.IO.InvalidDataException("Taslakta dönem, kişi veya hesap girdileri eksik.");
        var p = draft.Parameters;
        var expected = TeacherParameters.SourceSeptember2026 with { Coefficient = p.Coefficient, Indicator = p.Indicator, IncomeExemptBase = p.IncomeExemptBase, StampExemptBase = p.StampExemptBase };
        if (p != expected) throw new System.IO.InvalidDataException("Taslağın vergi/SGK oranları bu kural sürümüyle uyuşmuyor.");
        _ = module.Calculate(draft.Input, p);
        SelectedModule = Modules.Single(m => m.Id == ModuleId.TemelEgitim);
        Year = draft.Year.ToString(Tr); Month = draft.Month.ToString(Tr); Person = draft.Person;
        DayHours = draft.Input.DayHours.ToString(Tr); DykHours = draft.Input.DykHours.ToString(Tr); Garnishment = draft.Input.Garnishment.ToString(Tr);
        Coefficient = p.Coefficient.ToString(Tr); Indicator = p.Indicator.ToString(Tr);
        IncomeExemption = p.IncomeExemptBase.ToString(Tr); StampExemption = p.StampExemptBase.ToString(Tr);
        Calculate();
    }
}
