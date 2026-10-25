using Hakedis.Contracts;

namespace Hakedis.HalkEgitim.Core;

public sealed record HalkEgitimInput(decimal DayHours,decimal DykHours,decimal Garnishment);
public sealed record HalkEgitimParameters(decimal Coefficient,decimal Indicator,decimal DykMultiplier,
    decimal EmployeeRate,decimal EmployerRate,decimal IncomeRate,decimal StampRate,
    decimal IncomeExemptBase,decimal StampExemptBase);
public sealed record HalkEgitimResult(decimal DayRate,decimal DykRate,decimal DayGross,decimal DykGross,
    decimal Gross,int PremiumDays,decimal EmployeePremium,decimal EmployerPremium,decimal Accrual,
    decimal IncomeBase,decimal IncomeTax,decimal StampTax,decimal Garnishment,decimal Deductions,decimal Net);

// Kaynak usta öğretici dosyası Bordro!I7:AA36; dönem hücresi Eylül 2026'dır.
// Başka çekirdeği çağırmaz. SGK takvim yan bilgisi parasal hesaba girdi değildir.
public sealed class HalkEgitimPayrollModule : ICalculationModule
{
    public ModuleId Id=>ModuleId.HalkEgitimUstaOgretici;
    public string RuleVersion=>"halk-egitim.excel-2026-09.v1";
    public HalkEgitimResult Calculate(HalkEgitimInput input,HalkEgitimParameters p)
    {
        ArgumentNullException.ThrowIfNull(input);ArgumentNullException.ThrowIfNull(p);
        if(input.DayHours<0 || input.DykHours<0 || input.Garnishment<0)
            throw new ArgumentOutOfRangeException(nameof(input),"Saat ve icra kesintisi negatif olamaz.");
        if(p.Coefficient<=0 || p.Indicator<=0 || p.DykMultiplier<=0 || p.IncomeExemptBase<0 || p.StampExemptBase<0 ||
           new[]{p.EmployeeRate,p.EmployerRate,p.IncomeRate,p.StampRate}.Any(r=>r<0 || r>1))
            throw new ArgumentOutOfRangeException(nameof(p),"Katsayı, gösterge, oran veya istisna geçersiz.");
        var dayRate=p.Coefficient*p.Indicator;var dykRate=dayRate*p.DykMultiplier;
        var dayGross=Round(input.DayHours*dayRate);var dykGross=input.DykHours*dykRate;
        var gross=Round(dayGross+dykGross);
        var employee=Round(gross*p.EmployeeRate);var employer=Round(gross*p.EmployerRate);
        var accrual=Round(gross+employer);var incomeBase=Round(accrual-(employer+employee));
        var incomeTax=Round(Math.Max(0,incomeBase-p.IncomeExemptBase)*p.IncomeRate);
        var stampTax=Round(Math.Max(0,gross-p.StampExemptBase)*p.StampRate);
        var deductions=Round(incomeTax+stampTax+employee+employer+input.Garnishment);
        return new(dayRate,dykRate,dayGross,dykGross,gross,
            checked((int)decimal.Ceiling((input.DayHours+input.DykHours)/7.5m)),employee,employer,
            accrual,incomeBase,incomeTax,stampTax,input.Garnishment,deductions,accrual-deductions);
    }
    private static decimal Round(decimal value)=>decimal.Round(value,2,MidpointRounding.AwayFromZero);
}
