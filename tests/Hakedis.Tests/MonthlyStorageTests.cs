using Hakedis.Contracts;
using Hakedis.Storage;

namespace Hakedis.Tests;

public sealed class MonthlyStorageTests : IDisposable
{
    private readonly string folder=Path.Combine(Path.GetTempPath(),"HakedisTests",Guid.NewGuid().ToString("N"));
    [Fact]
    public void Updating_one_person_keeps_newer_other_person_records_and_backup()
    {
        var store=new MonthlyPayrollStore(folder);var first=new MonthlyInput(Guid.NewGuid(),117,0,0);var second=new MonthlyInput(Guid.NewGuid(),45,0,0);
        store.Update(ModuleId.TemelEgitim,2026,9,d=>d with{Entries=[first]});
        store.Update(ModuleId.TemelEgitim,2026,9,d=>d with{Entries=d.Entries.Append(second).ToArray()});
        store.Update(ModuleId.TemelEgitim,2026,9,d=>d with{Entries=d.Entries.Select(e=>e.TeacherId==first.TeacherId?e with{DayHours=108}:e).ToArray()});
        var read=store.Read(ModuleId.TemelEgitim,2026,9);Assert.Equal(2,read.Entries.Length);Assert.Contains(second,read.Entries);
        Assert.True(File.Exists(store.FilePath(ModuleId.TemelEgitim,2026,9)+".bak"));
        Assert.Empty(store.Read(ModuleId.Ortaogretim,2026,9).Entries);Assert.Empty(store.Read(ModuleId.TemelEgitim,2027,11).Entries);
    }
    [Fact]
    public void Wrong_period_and_duplicate_ids_in_file_are_rejected_without_overwrite()
    {
        var store=new MonthlyPayrollStore(folder);var e=new MonthlyInput(Guid.NewGuid(),117,0,0);
        Assert.Throws<InvalidDataException>(()=>store.Save(ModuleId.TemelEgitim,new(2026,9,PeriodValues.Source,[e,e])));
        store.Save(ModuleId.TemelEgitim,new(2026,9,PeriodValues.Source,[e]));
        var path=store.FilePath(ModuleId.TemelEgitim,2026,9);var original=File.ReadAllText(path);
        File.WriteAllText(path,original.Replace("\"Year\": 2026","\"Year\": 2027"));
        Assert.Throws<InvalidDataException>(()=>store.Update(ModuleId.TemelEgitim,2026,9,d=>d));
    }
    public void Dispose(){if(Directory.Exists(folder))Directory.Delete(folder,true);}
}
