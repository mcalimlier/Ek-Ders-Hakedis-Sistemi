using Hakedis.Contracts;

namespace Hakedis.TemelEgitim.Core;

public sealed record TeacherInput(decimal DayHours, decimal DykHours, decimal Garnishment);
public sealed record TeacherParameters(decimal Coefficient, decimal Indicator, decimal DykMultiplier,
    decimal EmployeeRate, decimal EmployerRate, decimal IncomeRate, decimal StampRate,
    decimal IncomeExemptBase, decimal StampExemptBase)
{
    // Kaynak Excel örneği; mevzuat doğrulaması yapılmış üretim parametresi değildir.
    public static TeacherParameters SourceSeptember2026 => new(1.575512m, 140m, 2m,
        .14m, .2175m, .15m, .00759m, 28075.5m, 33030m);
}
public sealed record TeacherResult(decimal DayRate, decimal DykRate, decimal DayGross,
    decimal DykGross, decimal Gross, int PremiumDays, decimal EmployeePremium,
    decimal EmployerPremium, decimal Accrual, decimal IncomeBase, decimal IncomeTax,
    decimal StampTax, decimal Garnishment, decimal Deductions, decimal Net);

public sealed class TeacherPayrollModule : ICalculationModule
{
    public ModuleId Id => ModuleId.TemelEgitim;
    public string RuleVersion => "temel-egitim.excel-2026-09.v1";
    public TeacherResult Calculate(TeacherInput input, TeacherParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(parameters);
        if (input.DayHours < 0 || input.DykHours < 0 || input.Garnishment < 0)
            throw new ArgumentOutOfRangeException(nameof(input), "Saat ve icra kesintisi negatif olamaz.");
        var p = parameters;
        if (p.Coefficient <= 0 || p.Indicator <= 0 || p.DykMultiplier <= 0 ||
            p.IncomeExemptBase < 0 || p.StampExemptBase < 0 ||
            new[] { p.EmployeeRate, p.EmployerRate, p.IncomeRate, p.StampRate }.Any(r => r < 0 || r > 1))
            throw new ArgumentOutOfRangeException(nameof(parameters), "Katsayı, gösterge, oran veya istisna geçersiz.");
        var dayRate = p.Coefficient * p.Indicator;
        var dykRate = dayRate * p.DykMultiplier;
        var dayGross = Round(input.DayHours * dayRate);
        var dykGross = input.DykHours * dykRate;
        var gross = Round(dayGross + dykGross);
        var employee = Round(gross * p.EmployeeRate);
        var employer = Round(gross * p.EmployerRate);
        var accrual = Round(gross + employer);
        var incomeBase = Round(accrual - employer - employee);
        var incomeTax = Round(Math.Max(0, incomeBase - p.IncomeExemptBase) * p.IncomeRate);
        var stampTax = Round(Math.Max(0, gross - p.StampExemptBase) * p.StampRate);
        var garnishment = Round(input.Garnishment);
        var deductions = Round(incomeTax + stampTax + employee + employer + garnishment);
        return new(dayRate, dykRate, dayGross, dykGross, gross,
            checked((int)decimal.Ceiling((input.DayHours + input.DykHours) / 7.5m)),
            employee, employer, accrual, incomeBase, incomeTax, stampTax, garnishment,
            deductions, accrual - deductions);
    }
    private static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
