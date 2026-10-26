using System.Windows;
using Hakedis.Contracts;
using Hakedis.Storage;

namespace Hakedis.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private Mutex? instance;
    private bool ownsInstance;
    private ApplicationPaths? paths;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            instance=new Mutex(true,"Local\\EkDersHakedisSistemi",out ownsInstance);
            if(!ownsInstance)
            {
                MessageBox.Show("Ek Ders Hakediş Sistemi zaten açık. Görev çubuğundaki simgeden açık pencereye dönebilirsiniz.",AppIdentity.Name,MessageBoxButton.OK,MessageBoxImage.Information);
                Shutdown();return;
            }
            paths=ApplicationPaths.Default;
            var warnings=paths.Initialize(AppContext.BaseDirectory).ToList();
            if(e.Args.Contains("--prepare-workspace",StringComparer.Ordinal))
            {
                Shutdown(warnings.Count==0?0:2);return;
            }
            var shell=new ShellViewModel(paths.TeacherFilePath);
            try{paths.BackupData();}
            catch(Exception ex) when(ex is System.IO.IOException or UnauthorizedAccessException)
            {warnings.Add("Açılış yedeği oluşturulamadı: "+ex.Message);}
            warnings.AddRange(shell.Outputs.LoadErrors);
            MainWindow=new MainWindow(shell);MainWindow.Show();
            if(warnings.Count>0)
            {
                MessageBox.Show(MainWindow,"Kayıtlar korunuyor; aşağıdaki durumları kontrol edin:\n\n"+string.Join("\n",warnings)+"\n\nVeri klasörü: "+paths.DataDirectory,"Kayıt kontrolü",MessageBoxButton.OK,MessageBoxImage.Warning);
            }
        }
        catch(Exception ex) when(ex is System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or TypeInitializationException)
        {
            MessageBox.Show("Kayıtlar açılamadığı için uygulama boş veriyle başlatılmadı. Mevcut dosyalar korunuyor.\n\n"+ex.Message+"\n\nVeri klasörü: "+paths?.DataDirectory,"Kayıtlar açılamadı",MessageBoxButton.OK,MessageBoxImage.Error);
            Shutdown(1);
        }
    }
    protected override void OnExit(ExitEventArgs e)
    {
        if(ownsInstance)
        {
            try{paths?.BackupData();}
            catch(Exception ex) when(ex is System.IO.IOException or UnauthorizedAccessException)
            {
                MessageBox.Show("Kayıtlar yerinde duruyor ancak kapanış yedeği oluşturulamadı:\n"+ex.Message,"Yedek oluşturulamadı",MessageBoxButton.OK,MessageBoxImage.Warning);
            }
            instance?.ReleaseMutex();
        }
        instance?.Dispose();base.OnExit(e);
    }
}
