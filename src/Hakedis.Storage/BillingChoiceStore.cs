using Hakedis.Contracts;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.Storage;

public sealed class BillingChoiceStore(string teacherPath)
{
    private string Root=>Path.GetDirectoryName(Path.GetFullPath(teacherPath))!;
    public TeacherRecord Save(TeacherRecord expected,int year,int month,ModuleId module,MonthlyInput total,PeriodValues constants)
    {
        if(total.TeacherId!=expected.Id || expected.Assignments is null || !TeacherRouting.Candidates(expected,year,month).Contains(module))
            throw new ArgumentException("Ödeme modülü bu kişinin seçilen aydaki görevleriyle uyuşmuyor.");
        var target=TeacherDirectory.Normalize(TeacherRouting.ProjectForPeriod(expected,module,year,month));
        var replacement=target with {Assignments=expected.Assignments.WithChoice(year,month,module)};
        _=TeacherDirectory.Normalize(replacement);
        var p=constants;
        var result=PayrollCores.Calculate(module,new(total.DayHours,total.DykHours,total.Garnishment),
            new(p.Coefficient,p.Indicator,p.DykMultiplier,p.EmployeeRate,p.EmployerRate,p.IncomeRate,p.StampRate,p.IncomeExemption,p.StampExemption));
        var archiveStore=new PaymentArchiveStore(Root);
        var paid=archiveStore.ReadAll().Where(a=>a.Document.Data.Year==year && a.Document.Data.Month==month)
            .Where(a=>a.Document.Data.Rows.Any(r=>r.Teacher.Id==expected.Id || (!string.IsNullOrEmpty(expected.IdentityNumber) && r.Teacher.IdentityNumber==expected.IdentityNumber))).ToArray();
        if(paid.Any(a=>a.Document.ModuleId!=module))throw new InvalidOperationException("Bu kişi bu ay başka modülün kayıtlı bordrosunda. Önce mevcut bordroyu kontrol edin; ödeme tercihi değiştirilmedi.");
        if(paid.Length>1)throw new InvalidOperationException("Bu kişi için birden fazla kayıtlı bordro var. Önce kayıtları kontrol edin.");
        var snapshot=new PaymentSnapshot(year,month,constants,[new(replacement,total,result)],"");
        using var identity=archiveStore.BeginIdentityWrite(snapshot,paid.SingleOrDefault()?.Path);
        var existing=new MonthlyPayrollStore(Root).Read(module,year,month).Entries.SingleOrDefault(i=>i.TeacherId==expected.Id);
        new PayrollEditStore(teacherPath).Save(module,year,month,[target],[replacement],[total],constants,
            new Dictionary<Guid,MonthlyInput?>{{expected.Id,existing}});
        return new TeacherDirectory(teacherPath,module).ReadForPeriod(year,month).Single(t=>t.Id==expected.Id);
    }
}
