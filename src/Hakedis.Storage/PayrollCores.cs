using Hakedis.Contracts;
using Hakedis.DinOgretimi.Core;
using Hakedis.HalkEgitim.Core;
using Hakedis.EmekliHalkEgitim.Core;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.Storage;

// Uygulama bileşimi: mevcut bordro veri biçimini, her çekirdeğin kendi sözleşmesine eşler.
// Formüller burada bulunmaz. Her modül yalnız kendi hesaplama çekirdeğine yönlendirilir.
public static class PayrollCores
{
    public static ModuleRegistry Registry {get;}=new([new TeacherPayrollModule(),new DinOgretimiPayrollModule(),new HalkEgitimPayrollModule(),new EmekliHalkEgitimPayrollModule()]);
    public static IReadOnlyList<ModuleDescriptor> Catalog=>Registry.Catalog;
    public static bool IsAvailable(ModuleId id)=>Catalog.Any(m=>m.Id==id && m.IsAvailable);
    public static string RuleVersion(ModuleId id)=>id switch
    {
        ModuleId.TemelEgitim=>Registry.Resolve<TeacherPayrollModule>(id).RuleVersion,
        ModuleId.DinOgretimi=>Registry.Resolve<DinOgretimiPayrollModule>(id).RuleVersion,
        ModuleId.HalkEgitimUstaOgretici=>Registry.Resolve<HalkEgitimPayrollModule>(id).RuleVersion,
        ModuleId.HalkEgitimEmekliUstaOgretici=>Registry.Resolve<EmekliHalkEgitimPayrollModule>(id).RuleVersion,
        _=>throw new NotSupportedException("Seçilen modülün hesaplama çekirdeği henüz eklenmedi.")
    };
    public static string CalendarRuleVersion(ModuleId id)=>id switch
    {
        ModuleId.TemelEgitim=>TeacherCalendar.RuleVersion,
        ModuleId.DinOgretimi=>DinOgretimiCalendar.RuleVersion,
        ModuleId.HalkEgitimUstaOgretici=>HalkEgitimCalendar.RuleVersion,
        ModuleId.HalkEgitimEmekliUstaOgretici=>EmekliHalkEgitimCalendar.RuleVersion,
        _=>throw new NotSupportedException("Seçilen modülün SGK gün çekirdeği henüz eklenmedi.")
    };
    public static TeacherResult Calculate(ModuleId id,TeacherInput input,TeacherParameters p)
    {
        if(id==ModuleId.TemelEgitim)return Registry.Resolve<TeacherPayrollModule>(id).Calculate(input,p);
        if(id==ModuleId.HalkEgitimEmekliUstaOgretici)
        {
            var e=Registry.Resolve<EmekliHalkEgitimPayrollModule>(id).Calculate(new(input.DayHours,input.DykHours,input.Garnishment),
                new(p.Coefficient,p.Indicator,p.DykMultiplier,p.EmployeeRate,p.EmployerRate,p.IncomeRate,p.StampRate,p.IncomeExemptBase,p.StampExemptBase));
            return new(e.DayRate,e.DykRate,e.DayGross,e.DykGross,e.Gross,e.PremiumDays,e.EmployeePremium,e.EmployerPremium,
                e.Accrual,e.IncomeBase,e.IncomeTax,e.StampTax,e.Garnishment,e.Deductions,e.Net);
        }
        if(id==ModuleId.HalkEgitimUstaOgretici)
        {
            var u=Registry.Resolve<HalkEgitimPayrollModule>(id).Calculate(new(input.DayHours,input.DykHours,input.Garnishment),
                new(p.Coefficient,p.Indicator,p.DykMultiplier,p.EmployeeRate,p.EmployerRate,p.IncomeRate,p.StampRate,p.IncomeExemptBase,p.StampExemptBase));
            return new(u.DayRate,u.DykRate,u.DayGross,u.DykGross,u.Gross,u.PremiumDays,u.EmployeePremium,u.EmployerPremium,
                u.Accrual,u.IncomeBase,u.IncomeTax,u.StampTax,u.Garnishment,u.Deductions,u.Net);
        }
        if(id!=ModuleId.DinOgretimi)throw new NotSupportedException("Seçilen modülün hesaplama çekirdeği henüz eklenmedi.");
        var r=Registry.Resolve<DinOgretimiPayrollModule>(id).Calculate(new(input.DayHours,input.DykHours,input.Garnishment),
            new(p.Coefficient,p.Indicator,p.DykMultiplier,p.EmployeeRate,p.EmployerRate,p.IncomeRate,p.StampRate,p.IncomeExemptBase,p.StampExemptBase));
        return new(r.DayRate,r.DykRate,r.DayGross,r.DykGross,r.Gross,r.PremiumDays,r.EmployeePremium,r.EmployerPremium,
            r.Accrual,r.IncomeBase,r.IncomeTax,r.StampTax,r.Garnishment,r.Deductions,r.Net);
    }
    public static CalendarResult Calendar(ModuleId id,int year,int month,DateTime? start,DateTime? end,decimal day,decimal dyk)
    {
        if(id==ModuleId.TemelEgitim)return TeacherCalendar.CalculateMonth(year,month,start,end,day,dyk);
        if(id==ModuleId.HalkEgitimEmekliUstaOgretici)
        {
            var e=EmekliHalkEgitimCalendar.CalculateMonth(year,month,start,end,day,dyk);
            return new(e.CalendarDays,e.PremiumDays,e.MissingDays);
        }
        if(id==ModuleId.HalkEgitimUstaOgretici)
        {
            var u=HalkEgitimCalendar.CalculateMonth(year,month,start,end,day,dyk);
            return new(u.CalendarDays,u.PremiumDays,u.MissingDays);
        }
        if(id!=ModuleId.DinOgretimi)throw new NotSupportedException("Seçilen modülün SGK gün çekirdeği henüz eklenmedi.");
        var r=DinOgretimiCalendar.CalculateMonth(year,month,start,end,day,dyk);
        return new(r.CalendarDays,r.PremiumDays,r.MissingDays);
    }
}
