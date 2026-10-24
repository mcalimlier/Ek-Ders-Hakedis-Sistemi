using Hakedis.Contracts;
using Hakedis.Storage;
using Hakedis.App;

namespace Hakedis.Tests;

public sealed class UiSettingsTests : IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"EdhsSettingsTests",Guid.NewGuid().ToString("N"));
    [Fact] public void Settings_are_persisted_independently_of_payroll_and_restore_on_reopen()
    {
        var vm=new UiSettingsViewModel(root);Assert.Equal(1366,vm.SelectedSize.Width);
        var applied=0;vm.Applied+=()=>applied++;
        vm.SelectedSize=vm.Sizes.Single(s=>s.Width==1920);vm.Save();Assert.Equal(1,applied);
        var next=new UiSettingsViewModel(root);Assert.Equal(1920,next.SelectedSize.Width);Assert.Equal(1080,next.SelectedSize.Height);
        Assert.False(Directory.Exists(Path.Combine(root,"AylikKayitlar")));
        Assert.False(File.Exists(Path.Combine(root,"ogretmenler.json")));
    }
    [Fact] public void Invalid_dimensions_are_rejected_without_overwriting_the_valid_preference()
    {
        var store=new UiPreferencesStore(root);store.Save(new(1280,720));var saved=File.ReadAllBytes(store.PathName);
        Assert.Throws<ArgumentException>(()=>store.Save(new(12,10)));Assert.Equal(saved,File.ReadAllBytes(store.PathName));
        Assert.Equal(new UiPreferences(1280,720),store.Read());
    }
    [Theory] [InlineData(1920,1080,1280,680,1280,680)] [InlineData(1280,720,1920,1040,1280,720)]
    [InlineData(1366,768,1093,580,1093,580)]
    public void Window_fits_available_logical_work_area_with_dpi_and_taskbar(int w,int h,int aw,int ah,int ew,int eh)
    {var fit=WindowSizing.Fit(new(w,h),aw,ah);Assert.Equal(ew,fit.Width);Assert.Equal(eh,fit.Height);}
    [Fact] public void Settings_panel_is_reachable_and_imam_hatip_is_not_an_extra_selectable_module()
    {
        var shell=new ShellViewModel(Path.Combine(root,"ogretmenler.json"));shell.Navigate("Settings");
        Assert.True(shell.SettingsVisible);Assert.Equal("Ayarlar",shell.Title);
        Assert.DoesNotContain(shell.Workspace.Modules,m=>m.Id==ModuleId.ImamHatipLisesi);
        Assert.Contains(shell.Workspace.Modules,m=>m.Id==ModuleId.DinOgretimi && m.IsAvailable);
    }
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
}
