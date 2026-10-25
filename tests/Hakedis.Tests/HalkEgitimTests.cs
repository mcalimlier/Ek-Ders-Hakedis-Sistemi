using System.Text.Json;
using Hakedis.App;
using Hakedis.Contracts;
using Hakedis.Storage;
using Hakedis.TemelEgitim.Core;
using Hakedis.HalkEgitim.Core;

namespace Hakedis.Tests;

public sealed class HalkEgitimTests : IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"EdhsHalk",Guid.NewGuid().ToString("N"));
    private string Catalog=>Path.Combine(root,"ogretmenler.json");
    private const ModuleId Halk=ModuleId.HalkEgitimUstaOgretici;
    private ShellViewModel Setup()
    {
        var t=new TeacherRecord(Guid.NewGuid(),"Deneme Usta Öğretici","10000000146","TR330006100519786457841326",
            "Örnek Halk Eğitim Merkezi","2360.43",new(2026,9,1),null,Halk);
        new TeacherDirectory(Catalog,Halk).Save(t);
        var s=new ShellViewModel(Catalog);s.Calculation.SelectedModule=s.Calculation.Modules.Single(m=>m.Id==Halk);return s;
    }
    [Fact] public void Module_is_available_with_its_own_rule_versions()
    {
        Assert.True(PayrollCores.Catalog.Single(m=>m.Id==Halk).IsAvailable);
        Assert.Equal("Halk Eğitim Merkezi Usta Öğretici",PayrollCores.Catalog.Single(m=>m.Id==Halk).Name);
        Assert.StartsWith("halk-egitim.",PayrollCores.RuleVersion(Halk));
        Assert.StartsWith("halk-egitim.",PayrollCores.CalendarRuleVersion(Halk));
        Assert.NotEqual(PayrollCores.RuleVersion(ModuleId.TemelEgitim),PayrollCores.RuleVersion(Halk));
    }
    [Fact] public void Independent_core_matches_fifty_recalculated_excel_scenarios()
    {
        using var json=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","halk-excel-recalculated.json")));
        var cases=json.RootElement.GetProperty("Cases");Assert.Equal(50,cases.GetArrayLength());
        foreach(var sample in cases.EnumerateArray())
        {
            var result=PayrollCores.Calculate(Halk,new(sample.GetProperty("DayHours").GetDecimal(),sample.GetProperty("DykHours").GetDecimal(),sample.GetProperty("Garnishment").GetDecimal()),TeacherParameters.SourceSeptember2026);
            foreach(var field in sample.EnumerateObject().Where(f=>f.Name is not ("DayHours" or "DykHours" or "Garnishment")))
            {
                var actual=Convert.ToDecimal(typeof(TeacherResult).GetProperty(field.Name)!.GetValue(result));
                Assert.True(Math.Abs(actual-field.Value.GetDecimal())<.00000001m,$"Halk Eğitim {field.Name}: Excel={field.Value}; C#={actual}");
            }
        }
    }
    [Fact] public void Module_records_archive_edit_personal_selection_and_constants_remain_separate()
    {
        var s=Setup();var w=s.Calculation;w.Month=10;
        w.SelectedTeacher=w.Teachers.Single();w.DayHours="60";w.Save(false);Assert.Equal(11381.50m,w.Rows.Single().Result!.Net);
        Assert.Equal("23",w.Rows.Single().MissingDays);w.CreatePayment();
        var record=s.Outputs.Records.Single();var original=File.ReadAllBytes(record.Path);
        Assert.Equal(Halk,record.Document.ModuleId);Assert.Equal(PayrollCores.RuleVersion(Halk),record.Document.RuleVersion);
        Assert.Empty(new TeacherDirectory(Catalog).Read());Assert.Empty(new MonthlyPayrollStore(root).Read(ModuleId.TemelEgitim,2026,10).Entries);
        Assert.Throws<InvalidOperationException>(()=>w.CreatePayment());
        s.Personal.Month=10;s.Personal.SelectedModule=s.Personal.Modules.Single(m=>m.Id==Halk);
        s.Personal.SelectedTeacher=Assert.Single(s.Personal.Teachers);Assert.Equal(record.Path,s.Personal.ReadSelection().Path);
        new CommonConstantsStore(root).Save(PeriodValues.Source with {Coefficient=2});
        Assert.Equal(original,File.ReadAllBytes(record.Path));
        s.ClearCurrentWork();s.Outputs.Selected=record;s.BeginArchiveEdit();w.Rows.Single().DayHours="27";
        Assert.False(s.SaveArchiveEdit(false));Assert.Equal(original,File.ReadAllBytes(record.Path));
        Assert.True(s.SaveArchiveEdit(true));Assert.Single(s.Outputs.Records);
        Assert.Equal(5121.68m,s.Outputs.Records.Single().Document.Data.Rows.Single().Result.Net);
    }
    [Fact] public void Sgk_calendar_uses_selected_month_and_normal_employment_dates()
    {
        var full=PayrollCores.Calendar(Halk,2026,10,null,null,60,0);Assert.Equal(new CalendarResult(31,8,23),full);
        Assert.Equal(new CalendarResult(16,10,6),PayrollCores.Calendar(Halk,2026,10,new(2026,10,5),new(2026,10,20),75,0));
        Assert.Throws<ArgumentException>(()=>PayrollCores.Calendar(Halk,2026,10,new(2026,10,30),null,60,0));
    }
    [Fact] public void Xml_uses_separate_workplace_profile_and_correct_month_days_and_occupation()
    {
        var s=Setup();s.Calculation.Month=10;s.Calculation.Rows.Single().DayHours="60";s.Calculation.CreatePayment();
        var profile=new SgkXmlStore(root);var w=new SgkWorkplace("12345678901234567890197000","TEST USTA KURUM","TEST ADRES","13","00000","A","07");
        profile.Save(Halk,new(w,[]));Assert.Equal(w,profile.Read(Halk).Workplace);Assert.Equal("",profile.Read(ModuleId.TemelEgitim).Workplace.RegistrationNumber);
        var editor=new SgkXmlEditor(s.Outputs.Records.Single(),profile.Read(Halk)){Confirmed=true};var xml=editor.Build();SgkXmlBuilder.Validate(xml);
        var document=System.Xml.Linq.XDocument.Parse(System.Text.Encoding.UTF8.GetString(xml));
        var person=document.Descendants("SIGORTALI").Single();Assert.Equal("8",person.Attribute("GUN")!.Value);Assert.Equal("23",person.Attribute("EKSIKGUNSAYISI")!.Value);
        Assert.Contains("2360",person.ToString());Assert.Equal("97",document.Root!.Element("ISYERI")!.Attribute("KONTROLNO")!.Value);
    }
    [Fact] public void Monetary_results_survive_sgk_capacity_error_but_xml_is_blocked()
    {
        var s=Setup();s.Calculation.Month=10;var row=s.Calculation.Rows.Single();row.DayHours="300";
        Assert.NotNull(row.Result);Assert.NotEmpty(row.SgkError);s.Calculation.CreatePayment();
        var editor=new SgkXmlEditor(s.Outputs.Records.Single(),new(new(),[])){Confirmed=true};Assert.Throws<ArgumentException>(()=>editor.Build());
    }
    [Fact] public void Payroll_report_uses_usta_ogretici_title_and_role()
    {
        var s=Setup();s.Calculation.Rows.Single().DayHours="60";s.Calculation.CreatePayment();
        var reports=PaymentReports.Build(s.Outputs.Records.Single().Document.Data,new("TEST KURUM","TEST BANKA","TEST GÖREVLİ","TEST YETKİLİ"));
        Assert.EndsWith("(USTA ÖĞRETİCİ)",reports[0].Title);Assert.Equal("Usta Öğretici",reports[0].Tables[0].Rows.Single()[2]);
    }
    [Fact] public void Core_has_no_dependency_on_other_payroll_cores_and_validates_its_own_parameters()
    {
        Assert.DoesNotContain(typeof(HalkEgitimPayrollModule).Assembly.GetReferencedAssemblies(),a=>a.Name is "Hakedis.TemelEgitim.Core" or "Hakedis.DinOgretimi.Core");
        var p=new HalkEgitimParameters(1.575512m,140,2,.14m,.2175m,.15m,.00759m,28075.5m,33030);
        var core=new HalkEgitimPayrollModule();Assert.Throws<ArgumentOutOfRangeException>(()=>core.Calculate(new(-1,0,0),p));
        Assert.Throws<ArgumentOutOfRangeException>(()=>core.Calculate(new(1,0,0),p with {EmployerRate=2}));
        var r=core.Calculate(new(75,0,1.005m),p with {Coefficient=2});Assert.Equal(280,r.DayRate);Assert.Equal(1.005m,r.Garnishment);
    }
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
}
