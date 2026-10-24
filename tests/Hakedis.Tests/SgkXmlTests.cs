using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Hakedis.Contracts;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.Tests;

public sealed class SgkXmlTests
{
    private static readonly Guid Id=Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static PaymentSnapshot Snapshot()
    {
        var t=new TeacherRecord(Id,"ALİ VELİ ÖZTÜRK","10000000146","","Okul","2341.04",new(2026,10,5),new(2026,10,25));
        var input=new MonthlyInput(Id,75,0,0);var p=PeriodValues.Source;
        var result=new TeacherPayrollModule().Calculate(new(75,0,0),new(p.Coefficient,p.Indicator,p.DykMultiplier,p.EmployeeRate,p.EmployerRate,p.IncomeRate,p.StampRate,p.IncomeExemption,p.StampExemption));
        return new(2026,10,p,[new(t,input,result)],"",TeacherCalendar.RuleVersion);
    }
    private static SgkWorkplace Workplace=>new("12345678901234567890120000","TEST & KURUM","ÖRNEK ADRES","13","00000");
    private static SgkPerson Person=>new(Id,"ALİ VELİ","ÖZTÜRK","13","22");
    [Theory] [InlineData("A")] [InlineData("E")] [InlineData("İ")]
    public void User_confirmed_nature_code_is_written_exactly_and_validates_against_original_schema(string code)
    {
        var bytes=SgkXmlBuilder.Build(Snapshot(),Workplace with{DocumentNature=code},[Person]);
        var document=XDocument.Parse(Encoding.UTF8.GetString(bytes));
        Assert.Equal(code,document.Root!.Element("BORDRO")!.Attribute("BELGEMAHIYET")!.Value);
        SgkXmlBuilder.Validate(bytes);
    }
    [Theory] [InlineData("T")] [InlineData("I")]
    public void Unsupported_T_and_dotless_I_are_not_accepted_as_cancellation(string code)
    {
        Assert.Throws<ArgumentException>(()=>SgkXmlBuilder.Build(Snapshot(),Workplace with{DocumentNature=code},[Person]));
    }
    [Theory] [InlineData("T")] [InlineData("I")]
    public void Old_unsupported_saved_nature_requires_an_explicit_new_selection(string code)
    {
        var record=new Hakedis.Storage.ArchivedPayment("unused","version",new(1,ModuleId.TemelEgitim,"rule",DateTimeOffset.UtcNow,Snapshot()));
        var editor=new Hakedis.App.SgkXmlEditor(record,new(Workplace with{DocumentNature=code},[])){Confirmed=true};
        Assert.Equal("",editor.DocumentNature);
        Assert.Throws<ArgumentException>(editor.Build);
    }
    [Fact] public void Embedded_schema_is_the_exact_user_supplied_file()
    {
        using var schema=SgkXmlBuilder.OpenSchema();
        Assert.Equal("F107BB3319014E281014E38884FDEAA63E7DBC756AB50F075B8FA98130B4240B",Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(schema)));
    }
    [Fact] public void Shared_missing_day_reason_preserves_the_two_digit_code()
    {
        var bytes=SgkXmlBuilder.Build(Snapshot(),Workplace with {DefaultMissingReason="07"},[Person with {MissingReason=""}]);
        Assert.Equal("07",XDocument.Parse(Encoding.UTF8.GetString(bytes)).Descendants("SIGORTALI").Single().Attribute("EKSIKGUNNEDENI")!.Value);
    }
    [Fact] public void Xml_uses_exact_schema_structure_frozen_gross_dates_and_calendar_days()
    {
        var data=Snapshot();var xml=SgkXmlBuilder.Build(data,Workplace,[Person]);
        Assert.False(xml.Take(3).SequenceEqual(new byte[]{239,187,191}));
        var text=Encoding.UTF8.GetString(xml);Assert.Contains("encoding=\"utf-8\"",text,StringComparison.OrdinalIgnoreCase);
        var root=XDocument.Parse(text).Root!;Assert.Equal("AYLIKBILDIRGELER",root.Name.LocalName);Assert.Equal(XNamespace.None,root.Name.Namespace);
        Assert.Equal(new[]{"ISYERI","BORDRO","BILDIRGELER"},root.Elements().Select(e=>e.Name.LocalName));
        Assert.Equal("123456789012345678901",root.Element("ISYERI")!.Attribute("ISYERISICIL")!.Value);
        Assert.Equal("TEST & KURUM",root.Element("ISYERI")!.Attribute("ISYERIUNVAN")!.Value);
        var row=root.Descendants("SIGORTALI").Single();
        Assert.Equal(data.Rows[0].Result.Gross.ToString("0.00",CultureInfo.InvariantCulture),row.Attribute("PEK")!.Value);
        Assert.Equal("10",row.Attribute("GUN")!.Value);Assert.Equal("11",row.Attribute("EKSIKGUNSAYISI")!.Value);
        Assert.Equal("0510",row.Attribute("GIRISGUN")!.Value);Assert.Equal("2510",row.Attribute("CIKISGUN")!.Value);
        Assert.Null(row.Attribute("SIGORTALISICIL"));Assert.Null(row.Attribute("RAPCALISTI"));
        Assert.Equal("2341.04",row.Attribute("MESLEKKOD")!.Value);
    }
    [Fact] public void Missing_required_settings_reason_or_identity_never_produces_xml()
    {
        Assert.Throws<ArgumentException>(()=>SgkXmlBuilder.Build(Snapshot(),Workplace with {Address=""},[Person]));
        Assert.Throws<ArgumentException>(()=>SgkXmlBuilder.Build(Snapshot(),Workplace,[Person with {MissingReason=""}]));
        Assert.Throws<ArgumentException>(()=>SgkXmlBuilder.Build(Snapshot(),Workplace,[Person with {ExitReason=""}]));
        Assert.Throws<ArgumentException>(()=>SgkXmlBuilder.Build(Snapshot(),Workplace,[Person with {GivenName=new string('A',19)}]));
        Assert.Throws<ArgumentException>(()=>SgkXmlBuilder.Build(Snapshot(),Workplace,[]));
    }
    [Fact] public void Older_employment_dates_are_omitted_and_culture_does_not_change_decimal_separator()
    {
        var s=Snapshot();s=s with {Rows=[s.Rows[0] with {Teacher=s.Rows[0].Teacher with {StartDate=new(2025,10,5),EndDate=null}}]};
        var old=CultureInfo.CurrentCulture;
        try{CultureInfo.CurrentCulture=new("tr-TR");var row=XDocument.Parse(Encoding.UTF8.GetString(SgkXmlBuilder.Build(s,Workplace,[Person with {ExitReason=""}]))).Descendants("SIGORTALI").Single();
            Assert.Null(row.Attribute("GIRISGUN"));Assert.Null(row.Attribute("CIKISGUN"));Assert.DoesNotContain(",",row.Attribute("PEK")!.Value);}
        finally{CultureInfo.CurrentCulture=old;}
    }
    [Fact] public void Schema_rejects_extra_attributes_and_missing_required_attributes()
    {
        var xml=XDocument.Parse(Encoding.UTF8.GetString(SgkXmlBuilder.Build(Snapshot(),Workplace,[Person])));
        xml.Root!.SetAttributeValue("FAZLAALAN","yasak");
        Assert.Throws<ArgumentException>(()=>SgkXmlBuilder.Validate(Encoding.UTF8.GetBytes(xml.ToString())));
        xml.Root.Attribute("FAZLAALAN")!.Remove();xml.Root.Element("ISYERI")!.Attribute("ISYERIUNVAN")!.Remove();
        Assert.Throws<ArgumentException>(()=>SgkXmlBuilder.Validate(Encoding.UTF8.GetBytes(xml.ToString())));
    }
    [Fact] public void Xml_editor_requires_explicit_review_and_preserves_name_boundaries()
    {
        var record=new Hakedis.Storage.ArchivedPayment("unused","version",new(1,ModuleId.TemelEgitim,"rule",DateTimeOffset.UtcNow,Snapshot()));
        var editor=new Hakedis.App.SgkXmlEditor(record,new(Workplace,[]));
        Assert.Equal("ALİ VELİ",editor.People.Single().GivenName);Assert.Equal("ÖZTÜRK",editor.People.Single().FamilyName);
        Assert.Throws<ArgumentException>(editor.Build);editor.Confirmed=true;editor.People.Single().MissingReason="13";editor.People.Single().ExitReason="22";
        Assert.NotEmpty(editor.Build());
        editor.People.Single().GivenName="ALİ";editor.People.Single().FamilyName="VELİ ÖZTÜRK";
        Assert.Equal("VELİ ÖZTÜRK",editor.Settings().Names.Single().FamilyName);
    }
    [Fact] public void Teacher_modules_share_workplace_but_master_instructor_profile_is_separate()
    {
        var root=Path.Combine(Path.GetTempPath(),"HakedisTests",Guid.NewGuid().ToString("N"));
        try
        {
            var store=new Hakedis.Storage.SgkXmlStore(root);store.Save(ModuleId.TemelEgitim,new(Workplace,[]));
            Assert.Equal(Workplace,store.Read(ModuleId.Ortaogretim).Workplace);
            Assert.Equal("",store.Read(ModuleId.HalkEgitimUstaOgretici).Workplace.RegistrationNumber);
            Assert.Throws<ArgumentException>(()=>Hakedis.Storage.SgkXmlStore.Export(root,ModuleId.TemelEgitim,Snapshot(),Workplace with {Address=""},[Person]));
            Assert.Empty(Directory.GetFiles(root,"*.xml",SearchOption.AllDirectories));
        }
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
    [Fact] public void Changed_teacher_name_does_not_restore_an_old_xml_name()
    {
        var record=new Hakedis.Storage.ArchivedPayment("unused","version",new(1,ModuleId.TemelEgitim,"rule",DateTimeOffset.UtcNow,Snapshot()));
        var editor=new Hakedis.App.SgkXmlEditor(record,new(Workplace,[new(Id,"ESKİ","SOYAD")]));
        Assert.Equal("ALİ VELİ",editor.People.Single().GivenName);Assert.Equal("ÖZTÜRK",editor.People.Single().FamilyName);
    }
}
