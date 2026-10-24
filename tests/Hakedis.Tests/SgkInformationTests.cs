using System.Text.Json;
using Hakedis.App;
using Hakedis.Contracts;

namespace Hakedis.Tests;

public sealed class SgkInformationTests : IDisposable
{
    private readonly string folder=Path.Combine(Path.GetTempPath(),"HakedisTests",Guid.NewGuid().ToString("N"));
    private ShellViewModel Create(int year,int month,DateTime? start=null,DateTime? end=null)
    {
        var shell=new ShellViewModel(Path.Combine(folder,"ogretmenler.json"));
        shell.Registration.Editor.FullName="Örnek Öğretmen";shell.Registration.Editor.Iban="TR330006100519786457841326";
        shell.Registration.Editor.StartDate=start;shell.Registration.Editor.EndDate=end;shell.SaveNewTeacher();
        shell.Monthly.Year=year;shell.Monthly.Month=month;shell.Monthly.SelectedTeacher=shell.Monthly.Teachers.Single();
        return shell;
    }
    [Theory]
    [InlineData(2026,10,"15")]
    [InlineData(2026,9,"14")]
    [InlineData(2026,2,"12")]
    [InlineData(2028,2,"13")]
    public void Full_month_uses_actual_calendar_length(int year,int month,string missing)
    {
        var shell=Create(year,month);shell.Monthly.DayHours="117";
        Assert.Equal(16,shell.Monthly.Rows[0].Result!.PremiumDays);
        Assert.Equal(missing,shell.Monthly.Rows[0].MissingDays);
    }
    [Fact]
    public void October_fifth_entry_uses_twenty_seven_available_days_and_keeps_money_unchanged()
    {
        var shell=Create(2026,10,new(2026,10,5));shell.Monthly.DayHours="117";var before=shell.Monthly.Rows[0].Result;
        Assert.Equal("11",shell.Monthly.Rows[0].MissingDays);
        Assert.Equal(before,shell.Monthly.Rows[0].Result);
        shell.Monthly.Save(false);
        shell.Calculation.Month=10;Assert.Equal("11",shell.Calculation.Rows[0].MissingDays);
        var reopened=new ShellViewModel(Path.Combine(folder,"ogretmenler.json"));reopened.Calculation.Month=10;
        Assert.Equal("11",reopened.Calculation.Rows[0].MissingDays);
    }
    [Fact]
    public void Registered_entry_and_exit_dates_are_inclusive()
    {
        var shell=Create(2026,10,new(2026,10,5),new(2026,10,20));
        shell.Monthly.DayHours="75";Assert.Equal("6",shell.Monthly.Rows[0].MissingDays);
    }
    [Fact]
    public void Old_entry_has_no_effect_on_full_month_and_dyk_hours_count_toward_work_days()
    {
        var shell=Create(2026,10,new(2024,9,5));shell.Monthly.DayHours="7,5";shell.Monthly.DykHours="0,01";
        Assert.Equal(2,shell.Monthly.Rows[0].Result!.PremiumDays);Assert.Equal("29",shell.Monthly.Rows[0].MissingDays);
    }
    [Theory]
    [InlineData(31,null,"7,5",1,0)]
    [InlineData(null,1,"7,5",1,0)]
    [InlineData(null,20,"75",20,10)]
    [InlineData(null,null,"0",31,31)]
    public void Entry_on_last_day_exit_on_first_day_and_zero_hours(int? start,int? end,string hours,int maximum,int missing)
    {
        var shell=Create(2026,10,start.HasValue?new DateTime(2026,10,start.Value):null,end.HasValue?new DateTime(2026,10,end.Value):null);
        shell.Monthly.DayHours=hours;shell.Monthly.Save(false);
        Assert.Equal(maximum,shell.Monthly.Rows[0].SgkInformation!.CalendarDays);
        Assert.Equal(missing,shell.Monthly.Rows[0].SgkInformation!.MissingDays);
    }
    [Fact]
    public void Impossible_missing_day_count_does_not_cancel_financial_payroll_or_create_negative_information()
    {
        var shell=Create(2026,10);shell.Monthly.DayHours="300";
        Assert.NotNull(shell.Monthly.Rows[0].Result);Assert.Equal("—",shell.Monthly.Rows[0].MissingDays);
        shell.Monthly.Save(false);shell.Calculation.Month=10;shell.Calculation.CreatePayment();
        Assert.NotEqual("—",shell.Calculation.NetText);
    }
    [Fact]
    public void Snapshot_saves_informational_day_values_without_changing_payroll_amounts()
    {
        var shell=Create(2026,10,new(2026,10,5));shell.Monthly.DayHours="117";shell.Monthly.Save(false);
        shell.Calculation.Month=10;shell.Calculation.CreatePayment();
        var file=Directory.EnumerateFiles(folder,"hakediş-*.json",SearchOption.AllDirectories).Single();
        using var json=JsonDocument.Parse(File.ReadAllText(file));
        var info=json.RootElement.GetProperty("Data").GetProperty("Rows")[0].GetProperty("SgkInformation");
        Assert.Equal(27,info.GetProperty("CalendarDays").GetInt32());Assert.Equal(16,info.GetProperty("PremiumDays").GetInt32());Assert.Equal(11,info.GetProperty("MissingDays").GetInt32());
    }
    public void Dispose(){if(Directory.Exists(folder))Directory.Delete(folder,true);}
}
