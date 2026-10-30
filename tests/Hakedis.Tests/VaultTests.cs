using Hakedis.Storage;
using System.Security.Cryptography;
using System.Text;

namespace Hakedis.Tests;

public sealed class VaultTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "HakedisVaultTests", Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(root, "first.edhskasa");
    private const string Password = "Yalnız-bana-özel-uzun-parola-2026";

    [Theory]
    [InlineData("Abcde12!?")]
    [InlineData("Abcdefgh12")]
    [InlineData("Abcdefgh!?")]
    [InlineData("12345678!?")]
    [InlineData("Abcdef12  ")]
    [InlineData("Abcdefghijklmnop12")]
    [InlineData("Abcdefghijklmnop!?")]
    [InlineData("1234567890123456!?")]
    public void New_password_requires_ten_characters_and_letter_digit_symbol(string candidate)
    {
        var store = new VaultStore(FilePath);
        Assert.Throws<ArgumentException>(() => store.Create(candidate, out _));
        Assert.False(store.Exists);
        using var session = store.Create(Password, out _);
        var before = File.ReadAllBytes(FilePath);
        Assert.Throws<ArgumentException>(() => session.ChangePassword(candidate));
        Assert.Equal(before, File.ReadAllBytes(FilePath));
    }

    [Theory]
    [InlineData("Abcdef12!?")]
    [InlineData("Şifre123!?")]
    public void Exactly_ten_mixed_characters_work_for_creation_and_password_change(string candidate)
    {
        var store = new VaultStore(FilePath);
        using (var created = store.Create(candidate, out _)) { }
        using var opened = store.Unlock(candidate);
        opened.ChangePassword("Yeni1234!?");
        using var changed = store.Unlock("Yeni1234!?");
        Assert.Equal(2, changed.Read().Groups.Length);
    }

    [Fact]
    public void Empty_vault_is_explicit_and_all_fields_are_encrypted_and_portable()
    {
        var store = new VaultStore(FilePath);
        Assert.False(store.Exists);
        using var session = store.Create(Password, out var recovery);
        Assert.Equal(64, recovery.Length);
        Assert.Equal(2, session.Read().Groups.Length);
        var data = session.Read();
        data.Groups[0] = new("KULLANICI-ÖZEL", "SİSTEM-SIR", "İŞYERİ-SIR", "KOD-SIR", "SİCİL-SIR");
        data.Groups[1] = new("USTA-ÖZEL", "USTA-SİSTEM", "USTA-İŞYERİ", "USTA-KOD", "USTA-SİCİL");
        session.Save(data);
        var bytes = File.ReadAllBytes(FilePath);
        foreach (var value in new[] { Password, recovery, "KULLANICI-ÖZEL", "SİSTEM-SIR", "KOD-SIR", "SİCİL-SIR", "USTA-KOD" })
            Assert.DoesNotContain(value, Encoding.UTF8.GetString(bytes));
        var copy = new VaultStore(Path.Combine(root, "other-pc", "copy.edhskasa"));
        copy.Import(FilePath, Password);
        using var opened = copy.Unlock(Password);
        Assert.Equal(data.Groups, opened.Read().Groups);
        using var recovered = copy.Recover(recovery);
        Assert.Equal(data.Groups, recovered.Read().Groups);
    }

    [Fact]
    public void Wrong_credentials_tampering_and_invalid_import_do_not_replace_existing_data()
    {
        var store = new VaultStore(FilePath);
        using var session = store.Create(Password, out _);
        var original = File.ReadAllBytes(FilePath);
        Assert.ThrowsAny<CryptographicException>(() => store.Unlock("incorrect"));
        Assert.ThrowsAny<CryptographicException>(() => store.Recover(new string('A', 64)));
        var bad = Path.Combine(root, "bad.edhskasa");
        File.WriteAllText(bad, "{}");
        Assert.ThrowsAny<Exception>(() => store.Import(bad, Password));
        Assert.Equal(original, File.ReadAllBytes(FilePath));
        var tampered = original.ToArray(); tampered[^3] ^= 1;
        File.WriteAllBytes(bad, tampered);
        Assert.ThrowsAny<Exception>(() => new VaultStore(bad).Unlock(Password));
        Assert.Throws<IOException>(() => store.Create(Password, out _));
    }

    [Fact]
    public void Password_change_recovery_lock_and_concurrent_writes_are_enforced()
    {
        var store = new VaultStore(FilePath);
        using var first = store.Create(Password, out var recovery);
        using var second = store.Unlock(Password);
        var data = first.Read(); data.Groups[0] = new("saved", "", "", "", ""); first.Save(data);
        Assert.Throws<IOException>(() => second.Save(second.Read()));
        first.ChangePassword("Yeni-uzun-ana-parolam-2026");
        Assert.ThrowsAny<CryptographicException>(() => store.Unlock(Password));
        using var changed = store.Unlock("Yeni-uzun-ana-parolam-2026");
        Assert.Equal("saved", changed.Read().Groups[0].UserCode);
        using var recovered = store.Recover(recovery);
        recovered.ChangePassword("Kurtarmadan-sonraki-parola-2026");
        using var restored = store.Unlock("Kurtarmadan-sonraki-parola-2026");
        first.Dispose();
        Assert.Throws<ObjectDisposedException>(() => first.Read());
        Assert.Throws<ObjectDisposedException>(() => first.Save(data));
    }

    [Fact]
    public void Weak_passwords_oversized_files_and_wrong_group_counts_are_rejected()
    {
        var store = new VaultStore(FilePath);
        Assert.Throws<ArgumentException>(() => store.Create("123", out _));
        using var session = store.Create(Password, out _);
        Assert.Throws<ArgumentException>(() => session.Save(new VaultData([])));
        var bad = Path.Combine(root, "huge.edhskasa");
        File.WriteAllBytes(bad, new byte[1_048_577]);
        Assert.Throws<InvalidDataException>(() => new VaultStore(bad).Unlock(Password));
    }

    [Fact]
    public void Maximum_unicode_fields_roundtrip_and_every_save_uses_a_new_nonce()
    {
        var store = new VaultStore(FilePath);
        using var session = store.Create(Password, out _);
        var value = new string('漢', 4096);
        var group = new VaultWorkplace(value, value, value, value, value);
        var data = new VaultData([group, group]);
        session.Save(data);
        var first = File.ReadAllBytes(FilePath);
        session.Save(data);
        Assert.NotEqual(first, File.ReadAllBytes(FilePath));
        using var reopened = store.Unlock(Password);
        Assert.Equal(data.Groups, reopened.Read().Groups);
    }

    [Fact]
    public void Authenticated_payload_tampering_is_rejected_and_exports_remain_encrypted()
    {
        var store = new VaultStore(FilePath);
        using var session = store.Create(Password, out _);
        var target = Path.Combine(root, "export.edhskasa"); session.Export(target);
        Assert.Equal(File.ReadAllBytes(FilePath), File.ReadAllBytes(target));
        var json = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(target))!;
        var cipher = Convert.FromBase64String(json["Payload"]!["Ciphertext"]!.GetValue<string>());
        cipher[0] ^= 1; json["Payload"]!["Ciphertext"] = Convert.ToBase64String(cipher);
        File.WriteAllText(target, json.ToJsonString());
        Assert.ThrowsAny<CryptographicException>(() => new VaultStore(target).Unlock(Password));
        var original = File.ReadAllBytes(FilePath);
        Assert.ThrowsAny<CryptographicException>(() => store.Import(target, Password, true));
        Assert.Equal(original, File.ReadAllBytes(FilePath));
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
