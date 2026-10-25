using Hakedis.App;
using Hakedis.Contracts;
using Hakedis.Storage;
using Hakedis.TemelEgitim.Core;
using System.Text.Json;
using Hakedis.MeslekiEgitim.Core;

namespace Hakedis.Tests;
public sealed class MeslekiEgitimTests : IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"EdhsMesleki",Guid.NewGuid().ToString("N"));
    private string Catalog=>Path.Combine(root,"ogretmenler.json");
    private const ModuleId Module=ModuleId.MeslekiEgitim;
    private ShellViewModel Setup()
    {
        new TeacherDirectory(Catalog,Module).Save(new(Guid.NewGuid(),"Mesleki Deneme","10000000146","TR330006100519786457841326","Örnek Mesleki Eğitim Merkezi","2341.04",new(2026,9,1),null,Module));
        var s=new ShellViewModel(Catalog);s.Calculation.SelectedModule=s.Calculation.Modules.Single(m=>m.Id==Module);return s;
    }
    [Fact] public void Module_is_available_with_its_own_rules_and_full_name()
    {
        Assert.True(PayrollCores.IsAvailable(Module));Assert.Equal("Mesleki Eğitim Merkezi Ücretli Öğretmen",PayrollCores.Catalog.Single(m=>m.Id==Module).Name);
        Assert.StartsWith("mesleki-egitim.",PayrollCores.RuleVersion(Module));Assert.StartsWith("mesleki-egitim.",PayrollCores.CalendarRuleVersion(Module));
    }
    [Fact] public void Fifty_excel_scenarios_match_with_the_sources_old_parameters()
    {
        using var json=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","mesleki-excel-recalculated.json")));
        var values=json.RootElement.GetProperty("Parameters");
        var p=TeacherParameters.SourceSeptember2026 with {Coefficient=values.GetProperty("Coefficient").GetDecimal(),Indicator=values.GetProperty("Indicator").GetDecimal(),IncomeExemptBase=values.GetProperty("IncomeExemption").GetDecimal(),StampExemptBase=values.GetProperty("StampExemption").GetDecimal()};
        Assert.Equal(1.387871m,p.Coefficient);var cases=json.RootElement.GetProperty("Cases");Assert.Equal(50,cases.GetArrayLength());
        foreach(var c in cases.EnumerateArray())
        {
            var r=PayrollCores.Calculate(Module,new(c.GetProperty("DayHours").GetDecimal(),c.GetProperty("DykHours").GetDecimal(),c.GetProperty("Garnishment").GetDecimal()),p);
            foreach(var f in c.EnumerateObject().Where(f=>f.Name is not ("DayHours" or "DykHours" or "Garnishment")))
                Assert.True(Math.Abs(Convert.ToDecimal(typeof(TeacherResult).GetProperty(f.Name)!.GetValue(r))-f.Value.GetDecimal())<.00000001m,f.Name);
        }
    }
    [Fact] public void Current_work_uses_common_constants_not_the_old_excel_coefficient()
    {
        var s=Setup();var w=s.Calculation;Assert.Same(s.CommonConstants.Editor,w.Constants);Assert.Equal(1.575512m,w.Constants.Read().Coefficient);
        w.Rows.Single().DayHours="51";Assert.Equal(9674.28m,w.Rows.Single().Result!.Net);
        w.CreatePayment();var paid=s.Outputs.Records.Single();var bytes=File.ReadAllBytes(paid.Path);
        Assert.Equal(Module,paid.Document.ModuleId);Assert.Equal(PayrollCores.RuleVersion(Module),paid.Document.RuleVersion);Assert.Empty(new TeacherDirectory(Catalog).Read());
        Assert.Throws<InvalidOperationException>(()=>w.CreatePayment());
        s.CommonConstants.ToggleSave();s.CommonConstants.Editor.Coefficient="2";s.CommonConstants.ToggleSave();Assert.Equal(2,w.Constants.Read().Coefficient);
        Assert.Equal(bytes,File.ReadAllBytes(paid.Path));
    }
    [Fact] public void Calendar_and_xml_use_current_inclusive_day_rules_and_shared_teacher_profile()
    {
        Assert.Equal(new CalendarResult(31,7,24),PayrollCores.Calendar(Module,2026,1,null,null,51,0));
        var s=Setup();s.Calculation.Rows.Single().DayHours="51";s.Calculation.CreatePayment();
        var store=new SgkXmlStore(root);var workplace=new SgkWorkplace("12345678901234567890120000","TEST KURUM","TEST ADRES","13","00000","A","07");
        store.Save(ModuleId.TemelEgitim,new(workplace,[]));Assert.Equal(workplace,store.Read(Module).Workplace);
        var editor=new SgkXmlEditor(s.Outputs.Records.Single(),store.Read(Module)){Confirmed=true};var bytes=editor.Build();SgkXmlBuilder.Validate(bytes);
        Assert.Contains("BELGETURU=\"13\"",System.Text.Encoding.UTF8.GetString(bytes));
    }
    [Fact] public void Personal_and_archive_edit_keep_the_module_and_frozen_parameters()
    {
        var s=Setup();var w=s.Calculation;w.Rows.Single().DayHours="51";w.CreatePayment();var paid=s.Outputs.Records.Single();
        s.Personal.Year=2026;s.Personal.Month=9;s.Personal.SelectedModule=s.Personal.Modules.Single(m=>m.Id==Module);s.Personal.SelectedTeacher=Assert.Single(s.Personal.Teachers);Assert.Equal(paid.Path,s.Personal.ReadSelection().Path);
        s.ClearCurrentWork();s.Outputs.Selected=paid;s.BeginArchiveEdit();w.Rows.Single().DayHours="60";
        Assert.False(s.SaveArchiveEdit(false));Assert.True(s.SaveArchiveEdit(true));Assert.Single(s.Outputs.Records);
        Assert.Equal(11381.50m,s.Outputs.Records.Single().Document.Data.Rows.Single().Result.Net);
    }
    [Fact] public void Core_is_independent_and_rejects_invalid_parameters()
    {
        Assert.DoesNotContain(typeof(MeslekiEgitimPayrollModule).Assembly.GetReferencedAssemblies(),a=>a.Name is "Hakedis.TemelEgitim.Core" or "Hakedis.DinOgretimi.Core" or "Hakedis.HalkEgitim.Core" or "Hakedis.EmekliHalkEgitim.Core");
        var p=new MeslekiEgitimParameters(1.575512m,140,2,.14m,.2175m,.15m,.00759m,28075.5m,33030);
        var core=new MeslekiEgitimPayrollModule();Assert.Throws<ArgumentOutOfRangeException>(()=>core.Calculate(new(-1,0,0),p));
        Assert.Throws<ArgumentOutOfRangeException>(()=>core.Calculate(new(1,0,0),p with {EmployerRate=2}));
    }
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
}
