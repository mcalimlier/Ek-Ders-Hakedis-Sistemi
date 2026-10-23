using System.Text.Json;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.Tests;

public sealed class ExcelRecalculationTests
{
    [Fact]
    public void Fifty_scenarios_recalculated_by_microsoft_excel_match_all_fourteen_calculated_fields()
    {
        using var json=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","excel-recalculated.json")));
        var cases=json.RootElement.GetProperty("Cases");Assert.Equal(50,cases.GetArrayLength());
        var module=new TeacherPayrollModule();
        foreach(var sample in cases.EnumerateArray())
        {
            var input=new TeacherInput(sample.GetProperty("DayHours").GetDecimal(),sample.GetProperty("DykHours").GetDecimal(),sample.GetProperty("Garnishment").GetDecimal());
            var result=module.Calculate(input,TeacherParameters.SourceSeptember2026);
            foreach(var field in sample.EnumerateObject().Where(f=>f.Name is not ("DayHours" or "DykHours" or "Garnishment")))
            {
                var actual=Convert.ToDecimal(typeof(TeacherResult).GetProperty(field.Name)!.GetValue(result));
                var expected=field.Value.ValueKind==JsonValueKind.String ? decimal.Parse(field.Value.GetString()!,System.Globalization.CultureInfo.InvariantCulture) : field.Value.GetDecimal();
                Assert.True(Math.Abs(actual-expected)<0.00000001m,$"{field.Name}; hours={input.DayHours}/{input.DykHours}; icra={input.Garnishment}; Excel={expected}; C#={actual}");
            }
        }
    }
}
