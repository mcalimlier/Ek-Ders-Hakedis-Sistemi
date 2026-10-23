namespace Hakedis.Contracts;

public sealed record MonthlyInput(Guid TeacherId, decimal DayHours, decimal DykHours, decimal Garnishment,
    DateTime? MonthStart = null, DateTime? MonthEnd = null);
public sealed record PeriodValues(decimal Coefficient, decimal Indicator, decimal IncomeExemption, decimal StampExemption,
    decimal EmployeeRate = .14m, decimal EmployerRate = .2175m, decimal IncomeRate = .15m,
    decimal StampRate = .00759m, decimal DykMultiplier = 2m)
{
    public static PeriodValues Source => new(1.575512m,140m,28075.5m,33030m);
}
public sealed record MonthlyDocument(int Year, int Month, PeriodValues Constants, MonthlyInput[] Entries);
