using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Hakedis.App;
using Hakedis.Storage;

internal static class VaultChecks
{
    public static void Run(string output)
    {
        var path = Path.Combine(output, Guid.NewGuid().ToString("N"), "test.edhskasa");
        const string password = "Şifre123!?";
        var window = new VaultWindow(path) { ShowActivated = false };
        window.Show(); Pump(window);
        Capture(window, Path.Combine(output, "kasa-ilk-kurulum.png"));
        Input(window, "VaultPassword").Password = password;
        Input(window, "VaultPasswordConfirm").Password = password;
        Click(window, "Kasamı oluştur");
        var recovery = All<TextBox>(window).Single(t => t.Text.Length == 64).Text;
        All<CheckBox>(window).Single().IsChecked = true;
        Click(window, "Kasaya devam et");
        var inputs = All<PasswordBox>(window).Where(p => p.MaxLength == 4096).ToArray();
        Check(inputs.Length == 10, "İki işyeri için on alan yok.");
        for (var i = 0; i < inputs.Length; i++) inputs[i].Password = "SENTETİK-" + i;
        Click(window, "Bilgileri kaydet");
        using (var opened = new VaultStore(path).Unlock(password)) Check(opened.Read().Groups[1].RegistrationNumber == "SENTETİK-9", "Ekran kaydı yanlış.");
        Capture(window, Path.Combine(output, "kasa-iki-grup.png"));
        var show = All<Button>(window).First(b => Equals(b.Content, "Göster"));
        show.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump(window);
        Check(All<TextBox>(window).Any(t => t.Text == "SENTETİK-0"), "Göster çalışmadı.");
        var secretFields = (System.Collections.IEnumerable)typeof(VaultWindow).GetField("fields", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(window)!;
        foreach (var field in secretFields) field.GetType().GetField("hideAt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(field, DateTime.UtcNow.AddSeconds(-1));
        typeof(VaultWindow).GetMethod("Tick", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, [null, EventArgs.Empty]);
        Check(!All<TextBox>(window).Any(t => t.Text == "SENTETİK-0"), "Gösterim süresi dolunca gizlenmedi.");
        // Preserve the existing text clipboard; do not replace a non-text clipboard.
        var previousText = Clipboard.ContainsText() ? Clipboard.GetText() : null;
        if (previousText is not null || Clipboard.GetDataObject() is not { } existing || existing.GetFormats().Length == 0)
        {
            try
            {
                All<Button>(window).First(b => Equals(b.Content, "Kopyala")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(Clipboard.GetText() == "SENTETİK-0", "Kopyalama çalışmadı.");
                Check(Clipboard.GetDataObject()!.GetDataPresent("CanIncludeInClipboardHistory"), "Pano geçmişi kısıtı eksik.");
                Clipboard.SetText("BAŞKA-PANO-İÇERİĞİ");
                typeof(VaultWindow).GetField("clipboardExpiry", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(window, DateTime.UtcNow.AddSeconds(-1));
                typeof(VaultWindow).GetMethod("Tick", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, [null, EventArgs.Empty]);
                Check(Clipboard.GetText() == "BAŞKA-PANO-İÇERİĞİ", "Başka uygulamanın pano içeriği silindi.");
            }
            finally { if (previousText is null) Clipboard.Clear(); else Clipboard.SetText(previousText); }
        }
        Click(window, "Şimdi kilitle");
        Check(All<PasswordBox>(window).All(p => p.Password.Length == 0), "Kilitte parola alanları temizlenmedi.");
        Check(!All<TextBox>(window).Any(t => t.Text.Contains("SENTETİK")), "Kilitte açık bilgi kaldı.");
        Input(window, "VaultPassword").Password = "yanlis"; Click(window, "Kasayı aç");
        Check(All<TextBlock>(window).Any(t => t.Text.Contains("yanlış veya")), "Yanlış parola bildirimi yok.");
        Input(window, "VaultPassword").Password = password; Click(window, "Kasayı aç");
        Check(All<PasswordBox>(window).First(p => p.MaxLength == 4096).Password == "SENTETİK-0", "Yeniden açma yanlış.");
        typeof(VaultWindow).GetField("lastInput", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(window, DateTime.UtcNow.AddMinutes(-3));
        typeof(VaultWindow).GetMethod("Tick", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, [null, EventArgs.Empty]);
        Pump(window); Check(All<PasswordBox>(window).All(p => p.Password.Length == 0), "Hareketsizlik kilidi çalışmadı.");
        Click(window, "Ana parolamı unuttum"); Input(window, "RecoveryCode").Password = recovery; Click(window, "Kurtarma anahtarıyla aç");
        All<Expander>(window).Single().IsExpanded = true; Pump(window);
        Input(window, "NewVaultPassword").Password = "Yeni1234!?";
        Input(window, "NewVaultPasswordConfirm").Password = "Yeni1234!?";
        Click(window, "Yeni ana parolayı kaydet");
        using (var opened = new VaultStore(path).Unlock("Yeni1234!?")) Check(opened.Read().Groups[0].UserCode == "SENTETİK-0", "Kurtarma sonrası veri kaybı.");
        typeof(Window).GetMethod("OnDeactivated", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, [EventArgs.Empty]); Pump(window);
        Check(All<PasswordBox>(window).All(p => p.Password.Length == 0), "Odak kaybı kilidi çalışmadı.");
        Input(window, "VaultPassword").Password = "Yeni1234!?"; Click(window, "Kasayı aç");
        typeof(VaultWindow).GetMethod("SessionSwitch", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, [window, new Microsoft.Win32.SessionSwitchEventArgs(Microsoft.Win32.SessionSwitchReason.SessionLock)]);
        Pump(window); Check(All<PasswordBox>(window).All(p => p.Password.Length == 0), "Windows kilidi alanları temizlemedi.");
        window.Close();
        Console.WriteLine("Kasa UI: boş kurulum, kurtarma anahtarı, iki grup/on alan, kayıt, gösterim süresi, pano/geçmiş kısıtı, kilit, yanlış parola, yeniden açma, hareketsizlik, kurtarma/parola değiştirme, odak kaybı ve Windows kilidi başarılı. Sentetik veri kullanıldı.");
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static PasswordBox Input(Window window, string name) => All<PasswordBox>(window).Single(p => p.Name == name);
    private static void Click(Window window, string label)
    { All<Button>(window).Single(b => Equals(b.Content, label)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump(window); }
    private static IEnumerable<T> All<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T result) yield return result;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in All<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
    private static void Pump(Window window) => window.Dispatcher.Invoke(() => { window.UpdateLayout(); }, DispatcherPriority.ApplicationIdle);
    private static void Capture(Window window, string path)
    {
        var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path); encoder.Save(stream);
    }
}
