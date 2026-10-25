using Hakedis.App;
using Hakedis.Contracts;
using Hakedis.Storage;
using Hakedis.TemelEgitim.Core;
using System.Text.Json;
using Hakedis.EmekliHalkEgitim.Core;

namespace Hakedis.Tests;
public sealed class RetiredHalkTests : IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"EdhsRetired",Guid.NewGuid().ToString("N"));
    private string PathName=>Path.Combine(root,"ogretmenler.json");
    private static ModuleId Retired=>ModuleId.HalkEgitimEmekliUstaOgretici;
    private ShellViewModel Setup()
    {
        new TeacherDirectory(PathName,Retired).Save(new(Guid.NewGuid(),"Emekli Deneme","10000000146","TR330006100519786457841326","Test Merkez","2360.43",new(2026,8,1),null,Retired));
        var s=new ShellViewModel(PathName);s.Calculation.SelectedModule=s.Calculation.Modules.Single(m=>m.Id==Retired);return s;
    }
    [Fact] public void Module_is_available_and_uses_independent_rules()
    {
        Assert.True(PayrollCores.IsAvailable(Retired));Assert.StartsWith("emekli-halk-egitim.",PayrollCores.RuleVersion(Retired));
        Assert.StartsWith("emekli-halk-egitim.",PayrollCores.CalendarRuleVersion(Retired));
    }
    [Fact] public void Independent_core_matches_fifty_excel_scenarios()
    {
        using var json=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","retired-excel-recalculated.json")));
        Assert.Equal("2026-09",json.RootElement.GetProperty("Period").GetString());
        var cases=json.RootElement.GetProperty("Cases");Assert.Equal(50,cases.GetArrayLength());
        var p=TeacherParameters.SourceSeptember2026 with {EmployeeRate=.075m,EmployerRate=.2475m};
        foreach(var c in cases.EnumerateArray())
        {
            var result=PayrollCores.Calculate(Retired,new(c.GetProperty("DayHours").GetDecimal(),c.GetProperty("DykHours").GetDecimal(),c.GetProperty("Garnishment").GetDecimal()),p);
            foreach(var f in c.EnumerateObject().Where(f=>f.Name is not ("DayHours" or "DykHours" or "Garnishment")))
                Assert.True(Math.Abs(Convert.ToDecimal(typeof(TeacherResult).GetProperty(f.Name)!.GetValue(result))-f.Value.GetDecimal())<.00000001m,f.Name);
        }
    }
    [Fact] public void Workspaces_use_retired_rates_and_normal_constants_changes_do_not_leak()
    {
        var s=Setup();var w=s.Calculation;Assert.Equal(.075m,w.Constants.Read().EmployeeRate);Assert.Equal(.2475m,w.Constants.Read().EmployerRate);
        w.Rows.Single().DayHours="12";Assert.Equal(2448.35m,w.Rows.Single().Result!.Net);w.CreatePayment();
        var paid=s.Outputs.Records.Single();var bytes=File.ReadAllBytes(paid.Path);
        s.CommonConstants.ToggleSave();s.CommonConstants.Editor.Coefficient="2";s.CommonConstants.ToggleSave();
        Assert.Equal(1.575512m,w.Constants.Read().Coefficient);Assert.Equal(bytes,File.ReadAllBytes(paid.Path));
        w.SelectedModule=w.Modules.Single(m=>m.Id==ModuleId.HalkEgitimUstaOgretici);
        Assert.Equal(.14m,w.Constants.Read().EmployeeRate);Assert.Equal(2,w.Constants.Read().Coefficient);
        w.SelectedModule=w.Modules.Single(m=>m.Id==Retired);Assert.Equal(.075m,w.Constants.Read().EmployeeRate);
    }
    [Fact] public void Xml_profile_keeps_common_workplace_but_retired_document_type_is_02()
    {
        var s=Setup();s.Calculation.Rows.Single().DayHours="12";s.Calculation.CreatePayment();
        var store=new SgkXmlStore(root);var normal=new SgkWorkplace("12345678901234567890197000","TEST KURUM","TEST ADRES","13","00000","A","07");
        store.Save(ModuleId.HalkEgitimUstaOgretici,new(normal,[]));var settings=store.Read(Retired);
        Assert.Equal("02",settings.Workplace.DocumentType);Assert.Equal(normal.RegistrationNumber,settings.Workplace.RegistrationNumber);
        var editor=new SgkXmlEditor(s.Outputs.Records.Single(),settings){Confirmed=true};var xml=editor.Build();SgkXmlBuilder.Validate(xml);
        Assert.Contains("BELGETURU=\"02\"",System.Text.Encoding.UTF8.GetString(xml));
        store.Save(Retired,editor.Settings());Assert.Equal("13",store.Read(ModuleId.HalkEgitimUstaOgretici).Workplace.DocumentType);
        Assert.Equal("",store.Read(ModuleId.TemelEgitim).Workplace.DocumentType);
    }
    [Fact] public void Reports_use_retired_role_and_retired_rates_in_headers()
    {
        var s=Setup();s.Calculation.Rows.Single().DayHours="12";s.Calculation.CreatePayment();
        var report=PaymentReports.Build(s.Outputs.Records.Single().Document.Data,new())[0];
        Assert.Contains("EMEKLİ USTA ÖĞRETİCİ",report.Title);Assert.Equal("Emekli Usta Öğretici",report.Tables[0].Rows.Single()[2]);
        var headers=string.Join(" ",report.Tables.SelectMany(t=>t.Headers));
        Assert.Contains("24,75",headers);Assert.Contains("7,5",headers);Assert.DoesNotContain("21,75",headers);
    }
    [Fact] public void Calendar_counts_inclusive_dates_and_does_not_change_money()
    {
        Assert.Equal(new CalendarResult(16,2,14),PayrollCores.Calendar(Retired,2026,9,new(2026,9,5),new(2026,9,20),12,0));
        var s=Setup();var row=s.Calculation.Rows.Single();row.DayHours="300";Assert.NotNull(row.Result);Assert.NotEmpty(row.SgkError);
    }
    [Fact] public void Retired_constants_card_saves_independently_and_old_archive_keeps_its_rates()
    {
        var s=Setup();var w=s.Calculation;w.Rows.Single().DayHours="12";w.CreatePayment();var paid=s.Outputs.Records.Single();var bytes=File.ReadAllBytes(paid.Path);
        Assert.True(s.RetiredConstants.IsReadOnly);s.RetiredConstants.ToggleSave();s.RetiredConstants.Editor.EmployeeRate="0,08";
        Assert.Equal(.08m,w.Constants.Read().EmployeeRate);Assert.Equal(.14m,s.Monthly.Constants.Read().EmployeeRate);
        s.RetiredConstants.ToggleSave();Assert.True(s.RetiredConstants.IsReadOnly);
        Assert.Equal(.08m,new CommonConstantsStore(root,Retired).Read().EmployeeRate);Assert.Equal(.14m,new CommonConstantsStore(root).Read().EmployeeRate);
        Assert.Equal(bytes,File.ReadAllBytes(paid.Path));s.ClearCurrentWork();s.Outputs.Selected=paid;s.BeginArchiveEdit();
        Assert.Equal(.075m,w.Constants.Read().EmployeeRate);Assert.NotSame(s.RetiredConstants.Editor,w.Constants);
        s.CancelArchiveEdit();Assert.Equal(.08m,w.Constants.Read().EmployeeRate);
    }
    [Fact] public void Core_is_independent_and_validates_its_parameters()
    {
        Assert.DoesNotContain(typeof(EmekliHalkEgitimPayrollModule).Assembly.GetReferencedAssemblies(),a=>a.Name is "Hakedis.TemelEgitim.Core" or "Hakedis.DinOgretimi.Core" or "Hakedis.HalkEgitim.Core");
        var p=new EmekliHalkEgitimParameters(1.575512m,140,2,.075m,.2475m,.15m,.00759m,28075.5m,33030);
        var core=new EmekliHalkEgitimPayrollModule();Assert.Throws<ArgumentOutOfRangeException>(()=>core.Calculate(new(-1,0,0),p));
        Assert.Throws<ArgumentOutOfRangeException>(()=>core.Calculate(new(1,0,0),p with {EmployeeRate=2}));
    }
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
}
