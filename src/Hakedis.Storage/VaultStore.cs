using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Konscious.Security.Cryptography;

namespace Hakedis.Storage;

public sealed record VaultWorkplace(string UserCode, string SystemPassword, string WorkplacePassword, string WorkplaceCode, string RegistrationNumber);
public sealed record VaultData(VaultWorkplace[] Groups)
{
    public static VaultData Empty() => new([new("", "", "", "", ""), new("", "", "", "", "")]);
}

/// <summary>Version 1 fixes Argon2id parameters; untrusted files cannot choose memory/CPU costs.</summary>
public sealed class VaultStore(string path)
{
    public const string PasswordRequirements = "En az 10 karakter; en az bir harf, bir rakam ve bir sembol gereklidir. Boşluk sembol sayılmaz.";
    public string FilePath { get; } = Path.GetFullPath(path);
    public bool Exists => File.Exists(FilePath);
    public static string DefaultPath => Path.Combine(ApplicationPaths.Default.RootDirectory, "Kasa", "isyeri.edhskasa");
    private const int MaximumBytes = 1_048_576;
    internal sealed record Box(byte[] Nonce, byte[] Ciphertext, byte[] Tag);
    internal sealed record Envelope(int Version, byte[] Salt, Box PasswordKey, Box RecoveryKey, Box Payload);

    public VaultSession Create(string password, out string recoveryCode)
    {
        ValidatePassword(password);
        using var guard = Guard();
        if (Exists) throw new IOException("Bu konumda zaten bir kasa var. Mevcut kasa değiştirilmedi.");
        var key = RandomNumberGenerator.GetBytes(32);
        var recovery = RandomNumberGenerator.GetBytes(32);
        var salt = RandomNumberGenerator.GetBytes(16);
        var derived = Derive(password, salt);
        try
        {
            var envelope = new Envelope(1, salt, Encrypt(key, derived, "password"), Encrypt(key, recovery, "recovery"), EncryptData(VaultData.Empty(), key));
            var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope);
            AtomicWrite(FilePath, bytes, false);
            recoveryCode = Convert.ToHexString(recovery);
            return new VaultSession(this, envelope, key.ToArray(), SHA256.HashData(bytes));
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(recovery); CryptographicOperations.ZeroMemory(derived); }
    }

    public VaultSession Unlock(string password) => Open(password, false);
    public VaultSession Recover(string recoveryCode) => Open(recoveryCode, true);
    private VaultSession Open(string credential, bool recovery)
    {
        var bytes = ReadFile(FilePath);
        var envelope = Parse(bytes);
        byte[] wrappingKey;
        if (recovery)
        {
            if (credential.Length != 64 || !credential.All(Uri.IsHexDigit)) throw new CryptographicException("Kurtarma anahtarı geçersiz.");
            wrappingKey = Convert.FromHexString(credential);
        }
        else wrappingKey = Derive(credential, envelope.Salt);
        byte[]? key = null;
        try
        {
            key = Decrypt(recovery ? envelope.RecoveryKey : envelope.PasswordKey, wrappingKey, recovery ? "recovery" : "password");
            _ = DecryptData(envelope.Payload, key);
            return new VaultSession(this, envelope, key.ToArray(), SHA256.HashData(bytes));
        }
        finally { CryptographicOperations.ZeroMemory(wrappingKey); if (key is not null) CryptographicOperations.ZeroMemory(key); }
    }

    public void Import(string sourcePath, string password, bool replace = false)
    {
        var source = new VaultStore(sourcePath);
        // Verify exactly the bytes that will be installed, without rereading a mutable source.
        var bytes = ReadFile(source.FilePath);
        var envelope = Parse(bytes);
        var wrapping = Derive(password, envelope.Salt);
        byte[]? key = null;
        try { key = Decrypt(envelope.PasswordKey, wrapping, "password"); _ = DecryptData(envelope.Payload, key); }
        finally { CryptographicOperations.ZeroMemory(wrapping); if (key is not null) CryptographicOperations.ZeroMemory(key); }
        using var guard = Guard();
        if (Exists && !replace) throw new IOException("Mevcut kasanın değiştirilmesi için açık onay gerekir.");
        AtomicWrite(FilePath, bytes, replace);
    }

    internal byte[] Commit(Envelope envelope, byte[] expectedRevision)
    {
        using var guard = Guard();
        if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(ReadFile(FilePath)), expectedRevision))
            throw new IOException("Kasa başka bir oturumda değiştirildi. Kilitleyip yeniden açın; bu değişiklik kaydedilmedi.");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope);
        AtomicWrite(FilePath, bytes, true);
        return SHA256.HashData(bytes);
    }

    internal void Export(string target, byte[] revision)
    {
        var bytes = ReadFile(FilePath);
        if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(bytes), revision)) throw new IOException("Kasa değiştirildi. Yeniden açın.");
        if (string.Equals(Path.GetFullPath(target), FilePath, StringComparison.OrdinalIgnoreCase)) throw new IOException("Yedek için farklı bir konum seçin.");
        AtomicWrite(Path.GetFullPath(target), bytes, true);
    }

    private FileStream Guard()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        return new FileStream(FilePath + ".guard", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }

    private static void AtomicWrite(string target, byte[] bytes, bool replace)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { stream.Write(bytes); stream.Flush(true); }
            File.Move(temporary, target, replace);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static byte[] ReadFile(string file)
    {
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length is <= 0 or > MaximumBytes) throw new InvalidDataException("Kasa dosyası boyutu geçersiz.");
        var bytes = new byte[(int)stream.Length]; stream.ReadExactly(bytes); return bytes;
    }

    private static Envelope Parse(byte[] bytes)
    {
        try
        {
            var e = JsonSerializer.Deserialize<Envelope>(bytes, new JsonSerializerOptions { MaxDepth = 8 });
            if (e is null || e.Version != 1 || e.Salt is not { Length: 16 }) throw new InvalidDataException("Kasa biçimi veya sürümü desteklenmiyor.");
            ValidateBox(e.PasswordKey, 32); ValidateBox(e.RecoveryKey, 32); ValidateBox(e.Payload, null);
            return e;
        }
        catch (JsonException) { throw new InvalidDataException("Kasa dosyası okunamıyor veya bozulmuş."); }
    }

    private static void ValidateBox(Box? box, int? size)
    {
        if (box is null || box.Nonce is not { Length: 12 } || box.Tag is not { Length: 16 } || box.Ciphertext is null ||
            box.Ciphertext.Length is <= 0 or > 524_288 || (size.HasValue && box.Ciphertext.Length != size))
            throw new InvalidDataException("Kasa dosyası geçersiz.");
    }

    internal static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length > 1024)
            throw new ArgumentException(PasswordRequirements + " En fazla 1024 karakter kullanılabilir.");
        var characters = password.EnumerateRunes().ToArray();
        if (characters.Length < 10 || !characters.Any(Rune.IsLetter) || !characters.Any(Rune.IsDigit) ||
            !characters.Any(c => Rune.IsPunctuation(c) || Rune.IsSymbol(c)))
            throw new ArgumentException(PasswordRequirements);
    }

    internal static byte[] Derive(string password, byte[] salt)
    {
        if (password.Length > 1024) throw new ArgumentException("Parola çok uzun.");
        var input = Encoding.UTF8.GetBytes(password);
        try
        {
            using var argon = new Argon2id(input) { Salt = salt, MemorySize = 65536, Iterations = 3, DegreeOfParallelism = 1 };
            return argon.GetBytes(32);
        }
        finally { CryptographicOperations.ZeroMemory(input); }
    }

    internal static Box Encrypt(byte[] plaintext, byte[] key, string purpose)
    {
        var nonce = RandomNumberGenerator.GetBytes(12); var cipher = new byte[plaintext.Length]; var tag = new byte[16];
        using var aes = new AesGcm(key, 16); aes.Encrypt(nonce, plaintext, cipher, tag, Encoding.UTF8.GetBytes("EDHS-vault-v1/" + purpose));
        return new Box(nonce, cipher, tag);
    }

    private static byte[] Decrypt(Box box, byte[] key, string purpose)
    {
        var plain = new byte[box.Ciphertext.Length];
        try { using var aes = new AesGcm(key, 16); aes.Decrypt(box.Nonce, box.Ciphertext, box.Tag, plain, Encoding.UTF8.GetBytes("EDHS-vault-v1/" + purpose)); return plain; }
        catch { CryptographicOperations.ZeroMemory(plain); throw; }
    }

    private static void ValidateData(VaultData? data)
    {
        if (data?.Groups is not { Length: 2 }) throw new ArgumentException("Kasada iki sabit işyeri bulunmalıdır.");
        foreach (var group in data.Groups)
        {
            if (group is null) throw new ArgumentException("İşyeri bilgileri eksik.");
            foreach (var value in new[] { group.UserCode, group.SystemPassword, group.WorkplacePassword, group.WorkplaceCode, group.RegistrationNumber })
                if (value is null || value.Length > 4096) throw new ArgumentException("Her alan en fazla 4096 karakter içerebilir.");
        }
    }

    internal static Box EncryptData(VaultData data, byte[] key)
    {
        ValidateData(data); var bytes = JsonSerializer.SerializeToUtf8Bytes(data);
        try { return Encrypt(bytes, key, "payload"); } finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    internal static VaultData DecryptData(Box payload, byte[] key)
    {
        var bytes = Decrypt(payload, key, "payload");
        try { var data = JsonSerializer.Deserialize<VaultData>(bytes); ValidateData(data); return data!; }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
}

public sealed class VaultSession : IDisposable
{
    private readonly VaultStore store;
    private VaultStore.Envelope envelope;
    private byte[]? key;
    private byte[] revision;
    internal VaultSession(VaultStore store, VaultStore.Envelope envelope, byte[] key, byte[] revision)
    { this.store = store; this.envelope = envelope; this.key = key; this.revision = revision; }
    private byte[] Key => key ?? throw new ObjectDisposedException(nameof(VaultSession), "Kasa kilitli.");
    public VaultData Read() => VaultStore.DecryptData(envelope.Payload, Key);
    public void Save(VaultData data)
    {
        var updated = envelope with { Payload = VaultStore.EncryptData(data, Key) };
        revision = store.Commit(updated, revision); envelope = updated;
    }
    public void ChangePassword(string password)
    {
        VaultStore.ValidatePassword(password);
        var salt = RandomNumberGenerator.GetBytes(16); var wrapping = VaultStore.Derive(password, salt);
        try
        {
            var updated = envelope with { Salt = salt, PasswordKey = VaultStore.Encrypt(Key, wrapping, "password") };
            revision = store.Commit(updated, revision); envelope = updated;
        }
        finally { CryptographicOperations.ZeroMemory(wrapping); }
    }
    public void Export(string target) { _ = Key; store.Export(target, revision); }
    public void Dispose() { if (key is not null) { CryptographicOperations.ZeroMemory(key); key = null; } }
}
