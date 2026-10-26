using Hakedis.Contracts;
using Hakedis.Storage;

namespace Hakedis.Tests;

public sealed class AlphanumericBankAccountTests : IDisposable
{
    // Deliberately synthetic: nonexistent bank code and zero check digits, format tests only.
    private const string Account = "TR00" + "00000" + "0" + "000000000Y000000";
    private readonly string folder = Path.Combine(Path.GetTempPath(), "HakedisTests", Guid.NewGuid().ToString("N"));
    private TeacherDirectory Store => new(Path.Combine(folder, "teachers.json"));
    private static TeacherRecord Teacher(string account) => new(Guid.NewGuid(), "Örnek Öğretmen", "", account, "Örnek Okul", "2341.04", null, null);

    [Fact]
    public void Bank_supplied_alphanumeric_account_survives_save_and_reopening()
    {
        var record = Teacher(Account);
        Store.Save(record);
        Assert.Equal(record.Iban, Assert.Single(Store.Read()).Iban);
    }

    [Fact]
    public void Account_case_and_display_spaces_are_normalized_without_replacing_letters()
    {
        Store.Save(Teacher("tr00 0000 0000 0000 000y 0000 00"));
        Assert.Equal(Account, Assert.Single(Store.Read()).Iban);
    }

    [Theory]
    [InlineData("TR00000000000000000Z000000")]
    [InlineData("TR330006100519786457841326")]
    [InlineData("")]
    public void Other_structurally_valid_accounts_are_accepted(string account)
    {
        Store.Save(Teacher(account));
        Assert.Equal(account, Assert.Single(Store.Read()).Iban);
    }

    public static IEnumerable<object[]> InvalidAccounts()
    {
        yield return [Account[..^1]];
        yield return [Account+"0"];
        yield return ["DE"+Account[2..]];
        yield return [Account[..2]+"A"+Account[3..]];
        yield return [Account[..6]+"A"+Account[7..]];
        yield return [Account[..9]+"1"+Account[10..]];
        yield return [Account.Replace('Y','?')];
        yield return [Account.Replace('Y','Ş')];
        yield return [Account[..^1]+"\n"];
    }
    [Theory]
    [MemberData(nameof(InvalidAccounts))]
    public void Invalid_structure_is_rejected_without_creating_records(string account)
    {
        Assert.Throws<ArgumentException>(() => Store.Save(Teacher(account)));
        Assert.Empty(Store.Read());
    }

    [Theory]
    [InlineData(Account, "BİÇİM UYGUN")]
    [InlineData("TR0000A000000000000Y000000", "HATALI")]
    [InlineData("", "-")]
    public void Payroll_account_status_reports_structure_without_claiming_checksum_verification(string account, string expected)
    {
        var shell = new Hakedis.App.ShellViewModel(Path.Combine(folder, "teachers.json"));
        shell.Registration.Editor.FullName = "Örnek Öğretmen";
        shell.SaveNewTeacher();
        var row = Assert.Single(shell.Calculation.Rows);
        row.Iban = account;
        Assert.Equal(expected, row.IbanCheck);
    }

    public void Dispose() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
}
