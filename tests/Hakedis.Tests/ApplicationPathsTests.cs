using System.IO.Compression;
using Hakedis.Storage;

namespace Hakedis.Tests;

public sealed class ApplicationPathsTests : IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"HakedisTests",Guid.NewGuid().ToString("N"));
    [Fact]
    public void Launching_a_different_exe_copy_keeps_the_same_data_and_output_paths()
    {
        var paths=new ApplicationPaths(Path.Combine(root,"Desktop"));
        var first=Path.Combine(root,"exe-one");var second=Path.Combine(root,"exe-two");
        paths.Initialize(first);File.WriteAllText(paths.TeacherFilePath,"existing-data");
        paths.Initialize(second);
        Assert.Equal("existing-data",File.ReadAllText(paths.TeacherFilePath));
        Assert.Equal(Path.Combine(paths.RootDirectory,"Veriler","ogretmenler.json"),paths.TeacherFilePath);
        Assert.StartsWith(paths.RootDirectory,paths.OutputDirectory);
    }
    [Fact]
    public void Legacy_import_preserves_conflicts_and_does_not_copy_lock_files()
    {
        var paths=new ApplicationPaths(Path.Combine(root,"Desktop"));paths.Initialize();
        File.WriteAllText(paths.TeacherFilePath,"current");
        var legacy=Path.Combine(root,"old-exe");var data=Path.Combine(legacy,"Veriler");Directory.CreateDirectory(data);
        File.WriteAllText(Path.Combine(data,"ogretmenler.json"),"older");
        File.WriteAllText(Path.Combine(data,"parameters.json"),"parameters");
        File.WriteAllText(Path.Combine(data,"parameters.json.lock"),"lock");
        var issues=paths.Initialize(legacy);
        Assert.Single(issues);Assert.Equal("current",File.ReadAllText(paths.TeacherFilePath));
        Assert.False(File.Exists(Path.Combine(paths.DataDirectory,"parameters.json")));
        Assert.False(File.Exists(Path.Combine(paths.DataDirectory,"parameters.json.lock")));
        Assert.Contains(Directory.GetFiles(paths.BackupDirectory,"ogretmenler.json",SearchOption.AllDirectories),p=>File.ReadAllText(p)=="older");
    }
    [Fact]
    public void First_legacy_import_copies_records_once_and_repeated_launches_do_not_add_backups()
    {
        var paths=new ApplicationPaths(Path.Combine(root,"Desktop"));
        var legacy=Path.Combine(root,"old-exe");var data=Path.Combine(legacy,"Veriler");Directory.CreateDirectory(data);
        File.WriteAllText(Path.Combine(data,"ogretmenler.json"),"old-catalog");
        File.WriteAllText(Path.Combine(data,"ogretmenler.json.lock"),"");
        Assert.Empty(paths.Initialize(legacy));Assert.Equal("old-catalog",File.ReadAllText(paths.TeacherFilePath));
        var count=Directory.GetFiles(paths.BackupDirectory,"*",SearchOption.AllDirectories).Length;
        Assert.Empty(paths.Initialize(legacy));Assert.Equal(count,Directory.GetFiles(paths.BackupDirectory,"*",SearchOption.AllDirectories).Length);
    }
    [Fact]
    public void Legacy_copy_cannot_resurrect_an_archive_missing_from_an_existing_workspace()
    {
        var paths=new ApplicationPaths(Path.Combine(root,"Desktop"));paths.Initialize();File.WriteAllText(paths.TeacherFilePath,"catalog");
        var legacy=Path.Combine(root,"old-exe");var data=Path.Combine(legacy,"Veriler");Directory.CreateDirectory(data);
        File.WriteAllText(Path.Combine(data,"ogretmenler.json"),"catalog");File.WriteAllText(Path.Combine(data,"removed.json"),"old");
        Assert.Single(paths.Initialize(legacy));Assert.False(File.Exists(Path.Combine(paths.DataDirectory,"removed.json")));
    }
    [Fact]
    public void Pending_legacy_transaction_is_backed_up_without_copying_its_absolute_paths()
    {
        var paths=new ApplicationPaths(Path.Combine(root,"Desktop"));
        var legacy=Path.Combine(root,"old-exe");var data=Path.Combine(legacy,"Veriler");Directory.CreateDirectory(data);
        File.WriteAllText(Path.Combine(data,"ogretmenler.json"),"catalog");File.WriteAllText(Path.Combine(data,"hibrit-islem.json"),"journal");
        Assert.Single(paths.Initialize(legacy));Assert.False(File.Exists(paths.TeacherFilePath));
        Assert.Single(Directory.GetFiles(paths.BackupDirectory,"hibrit-islem.json",SearchOption.AllDirectories));
    }
    [Fact]
    public void Backup_contains_original_data_with_relative_paths_and_does_not_change_source()
    {
        var paths=new ApplicationPaths(Path.Combine(root,"Desktop"));paths.Initialize();
        File.WriteAllText(paths.TeacherFilePath,"unchanged");
        File.WriteAllText(paths.TeacherFilePath+".lock","");
        var zipPath=paths.BackupData();Assert.NotNull(zipPath);
        using var zip=ZipFile.OpenRead(zipPath!);
        var entry=Assert.Single(zip.Entries);Assert.Equal("Veriler/ogretmenler.json",entry.FullName);
        using var reader=new StreamReader(entry.Open());Assert.Equal("unchanged",reader.ReadToEnd());
        Assert.Equal("unchanged",File.ReadAllText(paths.TeacherFilePath));
        Assert.Null(new ApplicationPaths(Path.Combine(root,"EmptyDesktop")).BackupData());
        Assert.Equal(zipPath,paths.BackupData());
    }
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
}
