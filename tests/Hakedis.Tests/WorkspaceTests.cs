using Hakedis.App;
using Hakedis.Contracts;

namespace Hakedis.Tests;

public class WorkspaceTests
{
    private static WorkspaceViewModel Example() => new() { Person = "Örnek Öğretmen", DayHours = "117" };
    [Fact]
    public void Selecting_a_future_module_clears_results_and_blocks_calculation()
    {
        var model = Example(); model.Calculate(); Assert.NotEqual("—", model.NetText);
        model.SelectedModule = model.Modules.Single(m => m.Id == ModuleId.Ortaogretim);
        Assert.False(model.IsAvailable); Assert.Equal("—", model.NetText); Assert.Empty(model.Details);
        Assert.Throws<NotSupportedException>(model.Calculate);
    }
    [Fact]
    public void Changing_inputs_clears_previous_results()
    {
        var model = Example(); model.Calculate(); model.DayHours = "118";
        Assert.Equal("—", model.NetText); Assert.Empty(model.Details);
    }
    [Theory]
    [InlineData("1.575512")] [InlineData("1.575,512")] [InlineData("NaN")]
    public void Ambiguous_or_invalid_number_is_rejected(string coefficient)
    {
        var model = Example(); model.Coefficient = coefficient;
        Assert.Throws<ArgumentException>(model.Calculate);
    }
    [Fact]
    public void Saved_draft_reopens_in_another_workspace_with_identical_result()
    {
        var folder = Path.Combine(Path.GetTempPath(), "HakedisTests", Guid.NewGuid().ToString("N"));
        try
        {
            var model = Example(); model.Calculate(); var file = Path.Combine(folder, "draft.json"); model.Save(file);
            var reopened = new WorkspaceViewModel(); reopened.Open(file);
            Assert.Equal(model.NetText, reopened.NetText); Assert.Equal(model.GrossText, reopened.GrossText);
            Assert.Equal(model.Person, reopened.Person); Assert.Equal(model.Details.ToArray(), reopened.Details.ToArray());
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
}
