using Hakedis.Contracts;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.Storage;

public sealed class CommonConstantsStore(string root,ModuleId module=ModuleId.TemelEgitim)
{
    private ModuleId Group=>module==ModuleId.HalkEgitimEmekliUstaOgretici?module:ModuleId.TemelEgitim;
    private string PathName=>Path.Combine(root,Group==ModuleId.TemelEgitim?"OrtakSabitler.json":"EmekliUstaSabitleri.json");
    private const string Rule="ortak-sabitler.v1";
    public PeriodValues Read()=>File.Exists(PathName)?new JsonDraftStore().Load<PeriodValues>(PathName,Group,Rule).Data:PeriodValues.ForModule(Group);
    public void Save(PeriodValues values)
    {
        try
        {
            _=PayrollCores.Calculate(Group,new(0,0,0),new(values.Coefficient,values.Indicator,values.DykMultiplier,
                values.EmployeeRate,values.EmployerRate,values.IncomeRate,values.StampRate,values.IncomeExemption,values.StampExemption));
        }
        catch(OverflowException ex){throw new ArgumentException("Dönem sabitleri hesaplama sınırlarını aşıyor; kayıtlı sabitler değiştirilmedi.",ex);}
        new JsonDraftStore().Save(PathName,new DraftDocument<PeriodValues>(1,Group,Rule,DateTimeOffset.UtcNow,values));
    }
}

public sealed class ReportDetailsStore(string root)
{
    private string PathName=>Path.Combine(root,"BelgeBilgileri.json");
    public ReportDetails Read()=>File.Exists(PathName)?new JsonDraftStore().Load<ReportDetails>(PathName,ModuleId.TemelEgitim,"belge-bilgileri.v1").Data:new();
    public void Save(ReportDetails value)=>new JsonDraftStore().Save(PathName,new DraftDocument<ReportDetails>(1,ModuleId.TemelEgitim,"belge-bilgileri.v1",DateTimeOffset.UtcNow,value));
}
