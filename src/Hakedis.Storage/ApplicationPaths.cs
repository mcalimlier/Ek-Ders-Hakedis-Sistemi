using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Hakedis.Contracts;

namespace Hakedis.Storage;

/// <summary>All portable copies use the same user-visible workspace, independent of the EXE location.</summary>
public sealed class ApplicationPaths
{
    public static ApplicationPaths Default {get;}=new(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
    public string RootDirectory {get;}
    public string DataDirectory=>Path.Combine(RootDirectory,"Veriler");
    public string TeacherFilePath=>Path.Combine(DataDirectory,"ogretmenler.json");
    public string OutputDirectory=>Path.Combine(RootDirectory,"Çıktılar");
    public string BackupDirectory=>Path.Combine(RootDirectory,"Yedekler");
    public string DraftDirectory=>OutputDirectory;

    public ApplicationPaths(string desktopDirectory)
    {
        if(string.IsNullOrWhiteSpace(desktopDirectory))throw new ArgumentException("Masaüstü klasörü bulunamadı.");
        RootDirectory=Path.Combine(Path.GetFullPath(desktopDirectory),AppIdentity.FolderName);
    }

    public IReadOnlyList<string> Initialize(string? legacyApplicationDirectory=null)
    {
        foreach(var path in new[]{RootDirectory,DataDirectory,OutputDirectory,BackupDirectory})Directory.CreateDirectory(path);
        var warnings=new List<string>();
        if(string.IsNullOrWhiteSpace(legacyApplicationDirectory))return warnings;
        var source=Path.GetFullPath(Path.Combine(legacyApplicationDirectory,"Veriler"));
        if(string.Equals(source,DataDirectory,StringComparison.OrdinalIgnoreCase) || !Directory.Exists(source))return warnings;
        // Never scan the backup tree or use an imported file to redirect the data root.
        if(source.StartsWith(RootDirectory+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))return warnings;
        var files=DataFiles(source).Select(f=>(Path:f,Relative:Path.GetRelativePath(source,f),Bytes:File.ReadAllBytes(f))).ToArray();
        if(files.Length==0)return warnings;
        using var migration=new FileStream(Path.Combine(DataDirectory,"aktarim.guard"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        foreach(var file in files)
        {
            var destination=Path.Combine(DataDirectory,file.Relative);
            if(File.Exists(destination) && !SHA256.HashData(file.Bytes).SequenceEqual(SHA256.HashData(File.ReadAllBytes(destination))))
                warnings.Add(file.Relative+": iki konumda farklı kayıt var. Otomatik birleştirme yapılmadı; mevcut kayıtlar korundu.");
        }
        if(files.Any(f=>f.Relative.Equals("hibrit-islem.json",StringComparison.OrdinalIgnoreCase) || f.Relative.Split(Path.DirectorySeparatorChar).Any(p=>p.StartsWith(".hibrit-",StringComparison.OrdinalIgnoreCase))))
            warnings.Add("Eski klasörde yarım kalmış toplu kayıt işlemi var. Eski dosyalar Yedekler/EskiKonum altında korundu; otomatik birleştirme yapılmadı.");
        if(warnings.Count==0 && DataFiles(DataDirectory).Any() && files.Any(f=>!File.Exists(Path.Combine(DataDirectory,f.Relative))))
            warnings.Add("Ana veri klasöründe kayıtlar var; eski konumdaki ek dosyalar otomatik birleştirilmedi. Silinmiş eski hakedişlerin geri gelmemesi için diğer kopya Yedekler/EskiKonum altında korundu.");
        if(warnings.Count==0 && files.All(f=>File.Exists(Path.Combine(DataDirectory,f.Relative))))return warnings;
        // A complete copy is retained before adding missing files; existing records are never overwritten.
        var recovery=Path.Combine(BackupDirectory,"EskiKonum",Fingerprint(files.Select(f=>(f.Relative,f.Bytes))));
        foreach(var file in files)
        {
            var relative=file.Relative;var bytes=file.Bytes;
            var backup=Path.Combine(recovery,relative);Directory.CreateDirectory(Path.GetDirectoryName(backup)!);File.WriteAllBytes(backup,bytes);
        }
        if(warnings.Count>0)return warnings;
        foreach(var file in files)
        {
            var relative=file.Relative;var bytes=file.Bytes;
            var destination=Path.Combine(DataDirectory,relative);
            if(File.Exists(destination))continue;
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            // Write and flush a sibling temporary file so an interruption cannot create a half JSON.
            var temporary=destination+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {stream.Write(bytes);stream.Flush(true);}
                File.Move(temporary,destination);
            }
            finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
        return warnings;
    }

    public string? BackupData()
    {
        var files=DataFiles(DataDirectory).Select(f=>(Relative:Path.GetRelativePath(DataDirectory,f),Bytes:File.ReadAllBytes(f))).ToArray();if(files.Length==0)return null;
        var folder=Path.Combine(BackupDirectory,"Otomatik");Directory.CreateDirectory(folder);
        var target=Path.Combine(folder,$"Veriler-{DateTime.Now:yyyyMMdd}-{Fingerprint(files)}.zip");
        if(File.Exists(target))return target;
        var temporary=target+".tmp";
        try
        {
            using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {
                using(var zip=new ZipArchive(stream,ZipArchiveMode.Create,true))
                    foreach(var file in files)
                    {
                        var entry=zip.CreateEntry(Path.Combine("Veriler",file.Relative).Replace('\\','/'),CompressionLevel.Optimal);
                        using var output=entry.Open();output.Write(file.Bytes);
                    }
                stream.Flush(true);
            }
            File.Move(temporary,target);return target;
        }
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }


    private static string Fingerprint(IEnumerable<(string Relative,byte[] Bytes)> files)
    {
        using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach(var file in files.OrderBy(f=>f.Relative,StringComparer.Ordinal))
        {hash.AppendData(Encoding.UTF8.GetBytes(file.Relative+"\0"));hash.AppendData(SHA256.HashData(file.Bytes));}
        return Convert.ToHexString(hash.GetHashAndReset())[..24];
    }
    private static IEnumerable<string> DataFiles(string root)=>!Directory.Exists(root)?[]:
        Directory.EnumerateFiles(root,"*",new EnumerationOptions{RecurseSubdirectories=true,IgnoreInaccessible=false,AttributesToSkip=FileAttributes.ReparsePoint})
            .Where(f=>f.EndsWith(".json",StringComparison.OrdinalIgnoreCase) || f.EndsWith(".json.bak",StringComparison.OrdinalIgnoreCase));
}
