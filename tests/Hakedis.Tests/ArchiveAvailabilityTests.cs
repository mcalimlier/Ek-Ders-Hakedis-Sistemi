using Hakedis.App;
using Hakedis.Storage;

namespace Hakedis.Tests;

public sealed class ArchiveAvailabilityTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "HakedisTests", Guid.NewGuid().ToString("N"));
    private ShellViewModel CreateArchive()
    {
        var shell = new ShellViewModel(Path.Combine(root, "ogretmenler.json"));
        shell.Registration.Editor.FullName = "Örnek Öğretmen";
        shell.Registration.Editor.Iban = "TR330006100519786457841326";
        shell.Registration.Editor.OccupationCode = "2341.04";
        shell.SaveNewTeacher();
        shell.Monthly.DayHours = "117";
        shell.Calculation.CreatePayment();
        return shell;
    }

    [Fact]
    public void Reload_keeps_readable_archives_and_reports_corrupt_file_without_modifying_it()
    {
        var shell = CreateArchive();
        var valid = Assert.Single(shell.Outputs.Records);
        var validBytes = File.ReadAllBytes(valid.Path);
        var corrupt = Path.Combine(Path.GetDirectoryName(valid.Path)!, "hakediş-corrupt.json");
        File.WriteAllText(corrupt, "broken archive");
        var reopened = new OutputsViewModel(root);
        Assert.Equal(valid.Path, Assert.Single(reopened.Records).Path);
        Assert.Equal(valid.Path, reopened.Selected?.Path);
        Assert.Contains("1 dosya okunamadı", reopened.Status);
        Assert.Contains("hakediş-corrupt.json", Assert.Single(reopened.LoadErrors));
        var available = new PaymentArchiveStore(root).ReadAvailable(out var errors);
        Assert.Equal(valid.Path, Assert.Single(available).Path);
        Assert.Contains("hakediş-corrupt.json", Assert.Single(errors));
        Assert.Equal("broken archive", File.ReadAllText(corrupt));
        Assert.Equal(validBytes, File.ReadAllBytes(valid.Path));
        Assert.Throws<InvalidDataException>(() => new PaymentArchiveStore(root).ReadAll());
    }

    [Fact]
    public void Reload_preserves_selection_and_selects_newest_when_selection_is_missing()
    {
        var shell = CreateArchive();
        var first = Assert.Single(shell.Outputs.Records);
        shell.Calculation.ClearWork();
        shell.Calculation.Month = 10;
        shell.Calculation.Rows[0].DayHours = "90";
        shell.Calculation.CreatePayment();
        shell.Outputs.Selected = shell.Outputs.Records.Single(r => r.Path == first.Path);
        shell.Outputs.Reload();
        Assert.Equal(first.Path, shell.Outputs.Selected?.Path);
        shell.Outputs.Selected = null;
        shell.Outputs.Reload();
        Assert.Equal(shell.Outputs.Records[0].Path, shell.Outputs.Selected?.Path);
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
