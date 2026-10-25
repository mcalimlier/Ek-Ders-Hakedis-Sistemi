using System.Text.Json;
using Hakedis.App;
using Hakedis.Contracts;
using Hakedis.Storage;
using Hakedis.DinOgretimi.Core;

namespace Hakedis.Tests;

public sealed class DinOgretimiTests : IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"HakedisDinTests",Guid.NewGuid().ToString("N"));
    private string Catalog=>Path.Combine(root,"ogretmenler.json");
    [Fact] public void Independent_core_matches_fifty_scenarios_recalculated_in_the_supplied_excel()
    {
        using var json=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","din-excel-recalculated.json")));
        var cases=json.RootElement.GetProperty("Cases");Assert.Equal(50,cases.GetArrayLength());
        var core=new DinOgretimiPayrollModule();Assert.Equal(ModuleId.DinOgretimi,core.Id);
        foreach(var sample in cases.EnumerateArray())
        {
            var result=core.Calculate(new(sample.GetProperty("DayHours").GetDecimal(),sample.GetProperty("DykHours").GetDecimal(),sample.GetProperty("Garnishment").GetDecimal()),DinOgretimiParameters.SourceSeptember2026);
            foreach(var field in sample.EnumerateObject().Where(f=>f.Name is not ("DayHours" or "DykHours" or "Garnishment")))
            {
                var actual=Convert.ToDecimal(typeof(DinOgretimiResult).GetProperty(field.Name)!.GetValue(result));
                var expected=field.Value.GetDecimal();
                Assert.True(Math.Abs(actual-expected)<.00000001m,$"Din Öğretimi {field.Name}: Excel={expected}; C#={actual}");
            }
        }
    }
    [Fact] public void Core_assembly_does_not_depend_on_the_temel_egitim_calculator()
        =>Assert.DoesNotContain(typeof(DinOgretimiPayrollModule).Assembly.GetReferencedAssemblies(),a=>a.Name=="Hakedis.TemelEgitim.Core");
    [Fact] public void Catalog_routes_to_independent_rule_versions_and_rejects_unavailable_modules()
    {
        Assert.True(PayrollCores.Catalog.Single(m=>m.Id==ModuleId.DinOgretimi).IsAvailable);
        Assert.Equal("Din Öğretimi Ücretli Öğretmen",PayrollCores.Catalog.Single(m=>m.Id==ModuleId.DinOgretimi).Name);
        Assert.NotEqual(PayrollCores.RuleVersion(ModuleId.TemelEgitim),PayrollCores.RuleVersion(ModuleId.DinOgretimi));
        Assert.Throws<NotSupportedException>(()=>PayrollCores.RuleVersion(ModuleId.Ortaogretim));
    }
    private ShellViewModel Setup()
    {
        var shell=new ShellViewModel(Catalog);
        Add(shell,ModuleId.TemelEgitim,"Temel Deneme","10000000146");
        Add(shell,ModuleId.DinOgretimi,"Din Deneme","10000000200");
        return shell;
    }
    private static void Add(ShellViewModel s,ModuleId module,string name,string tc)
    {
        s.RegistrationModule=s.Workspace.Modules.Single(m=>m.Id==module);
        s.Registration.Editor.FullName=name;s.Registration.Editor.IdentityNumber=tc;
        s.Registration.Editor.Iban="TR330006100519786457841326";s.Registration.Editor.OccupationCode="2341.04";
        s.Registration.Editor.StartDate=new DateTime(2026,9,14);s.SaveNewTeacher();
    }
    [Fact] public void Monthly_hybrid_archive_personal_and_edit_flows_stay_in_the_selected_module()
    {
        var s=Setup();var w=s.Calculation;
        Assert.Equal("Temel Deneme",Assert.Single(w.Rows).FullName);
        w.Rows[0].DayHours="117";w.CreatePayment();
        var basic=new PaymentArchiveStore(root).ReadAll().Single();var bytes=File.ReadAllBytes(basic.Path);
        w.SelectedModule=w.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);
        Assert.Equal("Din Deneme",Assert.Single(w.Rows).FullName);Assert.Null(w.Rows[0].Result);
        w.SelectedTeacher=w.Teachers.Single();w.DayHours="117";w.DykHours="0";w.Save(false);
        Assert.Equal(117,new MonthlyPayrollStore(root).Read(ModuleId.DinOgretimi,2026,9).Entries.Single().DayHours);
        Assert.Equal(22193.93m,w.Rows[0].Result!.Net);Assert.Equal("1",w.Rows[0].MissingDays);
        w.CreatePayment();var all=new PaymentArchiveStore(root).ReadAll();Assert.Equal(2,all.Count);
        var din=all.Single(a=>a.Document.ModuleId==ModuleId.DinOgretimi);
        Assert.Equal("Din Öğretimi Ücretli Öğretmen",din.Module);Assert.Equal(PayrollCores.RuleVersion(ModuleId.DinOgretimi),din.Document.RuleVersion);
        Assert.Equal(PayrollCores.CalendarRuleVersion(ModuleId.DinOgretimi),din.Document.Data.SgkRuleVersion);
        Assert.Throws<InvalidOperationException>(()=>w.CreatePayment());
        var p=s.Personal;p.Year=2026;p.Month=9;p.SelectedModule=p.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);
        p.SelectedTeacher=Assert.Single(p.Teachers);Assert.Equal("Din Deneme",p.SelectedTeacher.FullName);Assert.Equal(din.Path,p.ReadSelection().Path);
        s.ClearCurrentWork();s.Outputs.Selected=din;s.BeginArchiveEdit();Assert.True(w.IsArchiveEditing);
        w.Rows[0].DayHours="90";Assert.False(s.SaveArchiveEdit(false));Assert.Equal(117,new PaymentArchiveStore(root).Read(din.Path).Document.Data.Rows[0].Input.DayHours);
        Assert.True(s.SaveArchiveEdit(true));Assert.Equal(2,new PaymentArchiveStore(root).ReadAll().Count);
        Assert.Equal(90,new PaymentArchiveStore(root).Read(din.Path).Document.Data.Rows[0].Input.DayHours);
        Assert.Equal(bytes,File.ReadAllBytes(basic.Path));
    }
    [Fact] public void Corrupted_din_results_and_cross_module_rows_are_rejected()
    {
        var s=Setup();var w=s.Calculation;w.SelectedModule=w.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);
        w.Rows[0].DayHours="117";w.CreatePayment();var data=s.Outputs.Records.Single().Document.Data;
        var row=data.Rows[0];var bad=row.Result with {Gross=row.Result.Gross+1};
        Assert.Throws<InvalidDataException>(()=>PaymentArchiveStore.Validate(data with {Rows=[row with {Result=bad}]},ModuleId.DinOgretimi));
        Assert.Throws<InvalidDataException>(()=>PaymentArchiveStore.Validate(data,ModuleId.TemelEgitim));
    }
    [Fact] public void Din_calendar_is_independent_and_clips_entry_exit_inclusively()
    {
        var r=DinOgretimiCalendar.CalculateMonth(2026,10,new(2026,10,5),new(2026,10,20),75,0);
        Assert.Equal(16,r.CalendarDays);Assert.Equal(10,r.PremiumDays);Assert.Equal(6,r.MissingDays);
        Assert.Throws<ArgumentException>(()=>DinOgretimiCalendar.CalculateMonth(2026,9,new(2026,9,14),null,200,0));
    }
    [Fact] public void Invalid_parameters_and_negative_inputs_do_not_produce_payments()
    {
        var core=new DinOgretimiPayrollModule();var p=DinOgretimiParameters.SourceSeptember2026;
        Assert.Throws<ArgumentOutOfRangeException>(()=>core.Calculate(new(-1,0,0),p));
        Assert.Throws<ArgumentOutOfRangeException>(()=>core.Calculate(new(0,1,0),p with {EmployerRate=2}));
        var r=core.Calculate(new(75,0,1.005m),p with {Coefficient=2});Assert.Equal(280m,r.DayRate);Assert.Equal(1.005m,r.Garnishment);
    }
    [Theory] [InlineData("A")] [InlineData("E")] [InlineData("İ")]
    public void Xml_editor_uses_din_calendar_and_the_original_sgk_schema(string nature)
    {
        var s=Setup();var w=s.Calculation;w.SelectedModule=w.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);
        w.Rows[0].DayHours="117";w.CreatePayment();var record=s.Outputs.Records.Single();
        var workplace=new SgkWorkplace("12345678901234567890120000","TEST KURUM","TEST ADRES","13","00000",nature,"07");
        var editor=new SgkXmlEditor(record,new(workplace,[])){Confirmed=true};
        Assert.Equal("16",editor.People[0].Days);Assert.Equal("1",editor.People[0].MissingDays);
        var bytes=editor.Build();Hakedis.TemelEgitim.Core.SgkXmlBuilder.Validate(bytes);
        var xml=System.Xml.Linq.XDocument.Parse(System.Text.Encoding.UTF8.GetString(bytes));
        Assert.Equal(nature,xml.Root!.Element("BORDRO")!.Attribute("BELGEMAHIYET")!.Value);
        var insurance=xml.Descendants("SIGORTALI").Single();
        Assert.Contains("10000000200",insurance.ToString());Assert.DoesNotContain("10000000146",insurance.ToString());
        var bad=record.Document.Data with {Rows=[record.Document.Data.Rows[0] with {SgkInformation=new(17,15,2)}]};
        Assert.Throws<ArgumentException>(()=>SgkXmlStore.Build(ModuleId.DinOgretimi,bad,workplace,editor.ReadPeople()));
    }
    [Fact] public void Previous_month_counts_are_module_specific_and_historical_constants_are_frozen()
    {
        var s=Setup();var w=s.Calculation;w.Rows[0].DayHours="75";w.CreatePayment();
        w.SelectedModule=w.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);w.Rows[0].DayHours="117";w.CreatePayment();
        var din=s.Outputs.Records.Single(x=>x.Document.ModuleId==ModuleId.DinOgretimi);var original=din.Document.Data;
        new CommonConstantsStore(root).Save(PeriodValues.Source with {Coefficient=2});
        Assert.Equal(1,new PaymentArchiveStore(root).Movement(ModuleId.DinOgretimi,2026,10,[]).Previous);
        Assert.Equal(0,new PaymentArchiveStore(root).Movement(ModuleId.Ortaogretim,2026,10,[]).Previous);
        Assert.Equal(original.Constants,new PaymentArchiveStore(root).Read(din.Path).Document.Data.Constants);
        var tampered=File.ReadAllText(din.Path).Replace(PayrollCores.RuleVersion(ModuleId.DinOgretimi),PayrollCores.RuleVersion(ModuleId.TemelEgitim));
        File.WriteAllText(din.Path,tampered);Assert.Throws<InvalidDataException>(()=>new PaymentArchiveStore(root).Read(din.Path));
    }
    [Fact] public void Teacher_identity_cannot_be_registered_in_two_cores()
    {
        var s=Setup();Assert.Throws<ArgumentException>(()=>Add(s,ModuleId.DinOgretimi,"Aynı Kişi","10000000146"));
        Assert.Single(new TeacherDirectory(Catalog,ModuleId.DinOgretimi).Read());
        Assert.Single(new TeacherDirectory(Catalog,ModuleId.TemelEgitim).Read());
    }
    [Fact] public void Calendar_errors_do_not_erase_monetary_results_but_prevent_xml()
    {
        var s=Setup();var w=s.Calculation;w.SelectedModule=w.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);
        var row=w.Rows.Single();row.DayHours="200";Assert.NotNull(row.Result);Assert.NotEmpty(row.SgkError);
        w.CreatePayment();var record=s.Outputs.Records.Single();Assert.True(record.Net>0);
        var workplace=new SgkWorkplace("12345678901234567890120000","TEST KURUM","TEST ADRES","13","00000","A","07");
        var editor=new SgkXmlEditor(record,new(workplace,[])){Confirmed=true};Assert.Throws<ArgumentException>(()=>editor.Build());
    }
    [Fact] public void Draft_roundtrip_uses_the_selected_core_and_cannot_load_another_modules_draft()
    {
        var vm=new WorkspaceViewModel();vm.SelectedModule=vm.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);
        vm.Person="Din test";vm.DayHours="117";vm.Calculate();var path=Path.Combine(root,"draft.json");vm.Save(path);
        var loaded=new WorkspaceViewModel();Assert.Throws<InvalidDataException>(()=>loaded.Open(path));
        loaded.SelectedModule=loaded.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);loaded.Open(path);
        Assert.Equal(vm.NetText,loaded.NetText);Assert.Contains("Din Öğretimi",loaded.Modules.Single(m=>m.Id==ModuleId.DinOgretimi).Name);
    }
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
}
