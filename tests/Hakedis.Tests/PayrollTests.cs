using Hakedis.Contracts;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.Tests;

public class PayrollTests
{
    private readonly TeacherPayrollModule module = new();
    private readonly TeacherParameters p = TeacherParameters.SourceSeptember2026;

    [Fact]
    public void Source_workbook_ten_people_reconcile_to_recorded_totals()
    {
        decimal[] hours = [117, 117, 108, 45, 117, 117, 117, 117, 63, 54];
        var results = hours.Select(h => module.Calculate(new(h, 0, 0), p)).ToArray();
        Assert.Equal(214395.70m, results.Sum(r => r.Gross));
        Assert.Equal(46631.07m, results.Sum(r => r.EmployerPremium));
        Assert.Equal(30015.36m, results.Sum(r => r.EmployeePremium));
        Assert.Equal(261026.77m, results.Sum(r => r.Accrual));
        Assert.Equal(184380.34m, results.Sum(r => r.Net));
        Assert.Equal(134, results.Sum(r => r.PremiumDays));
    }

    [Fact]
    public void Mixed_hours_preserve_source_intermediate_rounding()
    {
        var r = module.Calculate(new(1, 1, 10), p);
        Assert.Equal(220.57168m, r.DayRate);
        Assert.Equal(441.14336m, r.DykGross);
        Assert.Equal(661.71m, r.Gross);
        Assert.Equal(569.07m - 10, r.Net);
        Assert.Equal(r.Gross - r.EmployeePremium - r.IncomeTax - r.StampTax - 10, r.Net);
    }

    [Theory]
    [InlineData(0, 0)] [InlineData(7.5, 1)] [InlineData(7.51, 2)]
    public void Premium_days_round_up(decimal hours, int days)
        => Assert.Equal(days, module.Calculate(new(hours, 0, 0), p).PremiumDays);

    [Theory]
    [InlineData(99, 0)] [InlineData(100, 0)] [InlineData(101, .15)]
    public void Taxes_apply_only_to_amount_above_exemption(decimal gross, decimal tax)
    {
        var custom = new TeacherParameters(1, 1, 2, 0, 0, .15m, .01m, 100, 100);
        var r = module.Calculate(new(gross, 0, 0), custom);
        Assert.Equal(tax, r.IncomeTax);
        Assert.Equal(gross > 100 ? .01m : 0, r.StampTax);
    }

    [Fact]
    public void Indicator_is_authoritative_and_rounding_is_away_from_zero()
    {
        var r = module.Calculate(new(1, 0, 0), p with { Coefficient = 1.005m, Indicator = 1 });
        Assert.Equal(1.01m, r.DayGross);
    }

    [Theory]
    [InlineData(-1, 0, 0)] [InlineData(0, -1, 0)] [InlineData(0, 0, -1)]
    public void Invalid_hours_or_garnishment_are_rejected(decimal a, decimal b, decimal c)
        => Assert.Throws<ArgumentOutOfRangeException>(() => module.Calculate(new(a, b, c), p));

    [Fact]
    public void Invalid_parameters_are_rejected()
        => Assert.Throws<ArgumentOutOfRangeException>(() => module.Calculate(new(10, 0, 0), p with { EmployeeRate = 2 }));

    [Theory]
    [InlineData(ModuleId.ImamHatipLisesi)] [InlineData(ModuleId.Ortaogretim)]
    [InlineData(ModuleId.MeslekiEgitim)] [InlineData(ModuleId.HalkEgitimUstaOgretici)]
    public void Future_modules_never_fall_back_to_basic_education(ModuleId id)
    {
        var registry = new ModuleRegistry([module]);
        Assert.Throws<NotSupportedException>(() => registry.Resolve<TeacherPayrollModule>(id));
        Assert.False(registry.Catalog.Single(m => m.Id == id).IsAvailable);
    }

    [Fact]
    public void Duplicate_module_registration_is_rejected()
        => Assert.Throws<ArgumentException>(() => new ModuleRegistry([module, new TeacherPayrollModule()]));
}
