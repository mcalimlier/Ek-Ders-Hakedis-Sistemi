using Hakedis.App;
using Hakedis.Contracts;
using Hakedis.Storage;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.Tests;

public sealed class HybridPayrollTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "HakedisTests", Guid.NewGuid().ToString("N"));
    private string PathName => Path.Combine(folder, "ogretmenler.json");
    [Theory]
    [InlineData(117, 17, 16, 1)]
    [InlineData(0, 17, 0, 17)]
    [InlineData(7.51, 17, 2, 15)]
    public void Missing_days_uses_inclusive_dates_and_rounded_up_premium(decimal hours, int capacity, int premium, int missing)
    {
        var r = TeacherCalendar.Calculate(new(2026,9,14), new(2026,9,30), null, null, hours, 0);
        Assert.Equal(capacity, r.CalendarDays); Assert.Equal(premium, r.PremiumDays); Assert.Equal(missing, r.MissingDays);
    }
    [Fact]
    public void Calendar_clips_employment_and_rejects_over_capacity_instead_of_negative_days()
    {
        var r = TeacherCalendar.Calculate(new(2026,9,14), new(2026,9,30), new DateTime(2026,9,20), new DateTime(2026,9,25), 30, 0);
        Assert.Equal(6, r.CalendarDays); Assert.Equal(2, r.MissingDays);
        Assert.Throws<ArgumentException>(() => TeacherCalendar.Calculate(new(2026,9,14),new(2026,9,30),null,null,150,0));
        Assert.Throws<ArgumentException>(() => TeacherCalendar.Calculate(new(2026,9,30),new(2026,9,14),null,null,0,0));
    }
    [Fact]
    public void Garnishment_retains_excel_raw_value_until_total_deduction_is_rounded()
    {
        var r = new TeacherPayrollModule().Calculate(new(117,0,1.005m), TeacherParameters.SourceSeptember2026);
        Assert.Equal(1.005m, r.Garnishment);
    }
    [Fact]
    public void Pdf_catalog_is_complete_and_turkish_word_search_finds_teacher_codes()
    {
        Assert.Equal(7190, OccupationCatalog.All.Count);
        Assert.Equal("0110.00", OccupationCatalog.All[0].Code);
        Assert.Contains(OccupationCatalog.Search("sınıf öğretmen"), x => x.Name.Contains("Sınıf", StringComparison.OrdinalIgnoreCase));
        Assert.Single(OccupationCatalog.Search("9901.06"));
        Assert.DoesNotContain(OccupationCatalog.All, x => x.Name.Contains('\ufffd') || x.Name.Contains(" / 113"));
    }
    [Fact]
    public void Independent_module_choices_do_not_move_registration_or_saved_monthly_records()
    {
        var shell = new ShellViewModel(PathName);
        shell.Registration.Editor.FullName = "Temel öğretmeni"; shell.SaveNewTeacher();
        var entry = shell.Monthly; entry.SelectedTeacher = entry.Teachers.Single(); entry.DayHours = "117"; entry.Save(false);
        shell.RegistrationModule = shell.Workspace.Modules.Single(m => m.Id == ModuleId.Ortaogretim);
        shell.Registration.Editor.FullName = "Orta öğretmeni"; shell.SaveNewTeacher();
        Assert.Equal(ModuleId.TemelEgitim, entry.SelectedModule.Id);
        Assert.Equal("Temel öğretmeni", Assert.Single(entry.Teachers).FullName);
        var reopened = new ShellViewModel(PathName);
        reopened.Monthly.SelectedTeacher = reopened.Monthly.Teachers.Single();
        Assert.Equal("117", reopened.Monthly.DayHours);
        reopened.Calculation.SelectedModule = reopened.Workspace.Modules.Single(m => m.Id == ModuleId.Ortaogretim);
        Assert.Equal("Orta öğretmeni", Assert.Single(reopened.Calculation.Rows).Teacher.FullName);
        Assert.Null(reopened.Calculation.Rows[0].Result);
    }
    [Fact]
    public void Hybrid_changes_are_live_but_only_valid_explicit_save_creates_payment_document()
    {
        var shell = new ShellViewModel(PathName); shell.Registration.Editor.FullName = "Öğretmen";
        shell.Registration.Editor.Iban = "TR330006100519786457841326"; shell.SaveNewTeacher();
        var row = Assert.Single(shell.Calculation.Rows); row.DayHours = "117";
        Assert.Equal(25806.89m, row.Result!.Gross); // Sabit kaynak katsayısı × 140 × 117.
        var before = shell.Calculation.NetTotal; row.DayHours = "108"; Assert.NotEqual(before, shell.Calculation.NetTotal);
        row.DayHours = "abc"; Assert.Null(row.Result); Assert.Equal("—", shell.Calculation.NetText);
        Assert.Throws<ArgumentException>(shell.Calculation.CreatePayment);
        row.DayHours = "117"; shell.Calculation.CreatePayment();
        Assert.True(Directory.EnumerateFiles(folder, "hakediş-*.json", SearchOption.AllDirectories).Any());
        Assert.Equal("117", Assert.Single(new ShellViewModel(PathName).Calculation.Rows).DayHours);
    }
    [Fact]
    public void Monthly_entry_is_live_in_matching_period_and_november_2027_keeps_separate_data()
    {
        var shell=new ShellViewModel(PathName);shell.Registration.Editor.FullName="Öğretmen";shell.SaveNewTeacher();
        shell.Monthly.DayHours="117";Assert.Equal(25806.89m,shell.Calculation.Rows[0].Result!.Gross);
        shell.Monthly.Save(false);
        shell.Calculation.Year=2027;shell.Calculation.Month=11;
        Assert.Equal("Kasım-2027",shell.Calculation.PeriodText);Assert.False(shell.Calculation.Rows[0].HasInput);
        shell.Calculation.Rows[0].DayHours="45";Assert.Equal("117",shell.Monthly.DayHours);
        shell.Calculation.Year=2026;shell.Calculation.Month=9;
        Assert.Equal("117",shell.Calculation.Rows[0].DayHours);
    }
    [Fact]
    public void Invalid_constant_and_invalid_date_clear_live_totals_and_cannot_be_saved()
    {
        var shell=new ShellViewModel(PathName);shell.Registration.Editor.FullName="Öğretmen";shell.SaveNewTeacher();
        shell.Monthly.DayHours="117";shell.Calculation.Constants.Coefficient="x";
        Assert.Equal("—",shell.Calculation.NetText);Assert.Null(shell.Monthly.Rows[0].Result);
        Assert.Throws<ArgumentException>(shell.Monthly.SaveConstants);
        shell.Monthly.Constants.Coefficient="1,575512";
        shell.Monthly.MonthStart=new DateTime(2027,1,1);
        Assert.Equal("—",shell.Calculation.NetText);Assert.Throws<ArgumentException>(()=>shell.Monthly.Save(false));
    }
    [Fact]
    public void Blank_zero_entry_is_saved_and_preview_syncs_and_corrupt_file_cannot_be_replaced()
    {
        var shell=new ShellViewModel(PathName);shell.Registration.Editor.FullName="Öğretmen";shell.SaveNewTeacher();
        shell.Monthly.Save(false);Assert.True(shell.Calculation.Rows[0].HasInput);Assert.NotNull(shell.Calculation.Rows[0].Result);
        var path=new MonthlyPayrollStore(folder).FilePath(ModuleId.TemelEgitim,2026,9);
        File.WriteAllText(path,"broken");Assert.Throws<InvalidDataException>(()=>shell.Monthly.Save(true));Assert.Equal("broken",File.ReadAllText(path));
    }
    public void Dispose() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
}
