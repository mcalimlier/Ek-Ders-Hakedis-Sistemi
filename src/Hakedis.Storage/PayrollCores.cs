using Hakedis.Contracts;
using Hakedis.DinOgretimi.Core;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.Storage;

// Uygulama bileşimi: mevcut bordro veri biçimini, her çekirdeğin kendi sözleşmesine eşler.
// Formüller burada bulunmaz. Din Öğretimi hiçbir zaman Temel Eğitim'e yönlendirilmez.
public static class PayrollCores
{
    public static ModuleRegistry Registry {get;}=new([new TeacherPayrollModule(),new DinOgretimiPayrollModule()]);
    public static IReadOnlyList<ModuleDescriptor> Catalog=>Registry.Catalog;
    public static bool IsAvailable(ModuleId id)=>Catalog.Any(m=>m.Id==id && m.IsAvailable);
    public static string RuleVersion(ModuleId id)=>id switch
    {
        ModuleId.TemelEgitim=>Registry.Resolve<TeacherPayrollModule>(id).RuleVersion,
        ModuleId.DinOgretimi=>Registry.Resolve<DinOgretimiPayrollModule>(id).RuleVersion,
        _=>throw new NotSupportedException("Seçilen modülün hesaplama çekirdeği henüz eklenmedi.")
    };
    public static string CalendarRuleVersion(ModuleId id)=>id switch
    {
        ModuleId.TemelEgitim=>TeacherCalendar.RuleVersion,
        ModuleId.DinOgretimi=>DinOgretimiCalendar.RuleVersion,
        _=>throw new NotSupportedException("Seçilen modülün SGK gün çekirdeği henüz eklenmedi.")
    };
    public static TeacherResult Calculate(ModuleId id,TeacherInput input,TeacherParameters p)
    {
        if(id==ModuleId.TemelEgitim)return Registry.Resolve<TeacherPayrollModule>(id).Calculate(input,p);
        if(id!=ModuleId.DinOgretimi)throw new NotSupportedException("Seçilen modülün hesaplama çekirdeği henüz eklenmedi.");
        var r=Registry.Resolve<DinOgretimiPayrollModule>(id).Calculate(new(input.DayHours,input.DykHours,input.Garnishment),
            new(p.Coefficient,p.Indicator,p.DykMultiplier,p.EmployeeRate,p.EmployerRate,p.IncomeRate,p.StampRate,p.IncomeExemptBase,p.StampExemptBase));
        return new(r.DayRate,r.DykRate,r.DayGross,r.DykGross,r.Gross,r.PremiumDays,r.EmployeePremium,r.EmployerPremium,
            r.Accrual,r.IncomeBase,r.IncomeTax,r.StampTax,r.Garnishment,r.Deductions,r.Net);
    }
    public static CalendarResult Calendar(ModuleId id,int year,int month,DateTime? start,DateTime? end,decimal day,decimal dyk)
    {
        if(id==ModuleId.TemelEgitim)return TeacherCalendar.CalculateMonth(year,month,start,end,day,dyk);
        if(id!=ModuleId.DinOgretimi)throw new NotSupportedException("Seçilen modülün SGK gün çekirdeği henüz eklenmedi.");
        var r=DinOgretimiCalendar.CalculateMonth(year,month,start,end,day,dyk);
        return new(r.CalendarDays,r.PremiumDays,r.MissingDays);
    }
}
