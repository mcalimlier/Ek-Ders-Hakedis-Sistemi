using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Hakedis.Storage;
using Microsoft.Win32;

namespace Hakedis.App;

public sealed class VaultWindow : Window
{
    private readonly VaultStore store;
    private VaultSession? session;
    private readonly StackPanel body = new();
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Foreground = ThemeBrush("Gold500Brush"), FontSize = 11, Margin = new Thickness(0, 6, 0, 2) };
    private readonly List<SecretField> fields = [];
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private DateTime lastInput = DateTime.UtcNow;
    private string? clipboardValue;
    private DateTime clipboardExpiry;
    private bool internalDialog;
    private bool dirty;

    public VaultWindow() : this(VaultStore.DefaultPath) { }
    public VaultWindow(string filePath)
    {
        store = new VaultStore(filePath);
        Title = "İşyeri Bilgileri • Şifre Kasası";
        Width = 860; Height = 620; MinWidth = 720; MinHeight = 500;
        MaxHeight = SystemParameters.WorkArea.Height; MaxWidth = SystemParameters.WorkArea.Width;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = ThemeBrush("Navy900Brush"); Foreground = Brushes.White;
        FontFamily = new FontFamily("Segoe UI"); FontSize = 12;
        UseLayoutRounding = true; SnapsToDevicePixels = true;
        var layout = new DockPanel { Margin = new Thickness(16) };
        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        var title = Text("İŞYERİ BİLGİLERİ KASASI", 19, true); title.Foreground = ThemeBrush("Gold500Brush");
        header.Children.Add(title);
        header.Children.Add(Text("Ücretli Öğretmenler ve Usta Öğreticiler için kişisel, şifreli kasa.", 11));
        DockPanel.SetDock(header, Dock.Top); layout.Children.Add(header);
        var footer = new StackPanel();
        footer.Children.Add(status);
        footer.Children.Add(Text("Kasa 2 dakika hareketsizlikte, bu pencereden ayrılınca ve Windows kilitlenince kilitlenir.\nKaydedilmemiş değişiklikler kilitlenirken silinir. Kasa, normal veri yedeklerine dahil değildir.", 11));
        DockPanel.SetDock(footer, Dock.Bottom); layout.Children.Add(footer);
        layout.Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        Content = layout;
        PreviewKeyDown += (_, _) => lastInput = DateTime.UtcNow;
        PreviewMouseDown += (_, _) => lastInput = DateTime.UtcNow;
        PreviewMouseMove += (_, _) => lastInput = DateTime.UtcNow;
        Deactivated += (_, _) => { if (!internalDialog) Lock(); };
        SystemEvents.SessionSwitch += SessionSwitch;
        timer.Tick += Tick; timer.Start();
        Closed += (_, _) => { timer.Stop(); SystemEvents.SessionSwitch -= SessionSwitch; Lock(); ClearClipboard(); };
        ShowLocked();
    }

    private static Brush ThemeBrush(string key) => (Brush)Application.Current.FindResource(key);
    private static TextBlock Text(string text, double size = 12, bool bold = false) => new()
    {
        Text = text, FontSize = size, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
        TextWrapping = TextWrapping.Wrap, Foreground = bold ? Brushes.White : ThemeBrush("PremiumMutedTextBrush"), Margin = new Thickness(0, 2, 0, 5)
    };
    private Button ActionButton(string title, Action action)
    {
        var button = new Button { Content = title, HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(10, 0, 10, 0), Margin = new Thickness(0, 3, 6, 3), MinHeight = 30 };
        var primary = title is "Kasamı oluştur" or "Kasayı aç" or "Bilgileri kaydet" or "Kasaya devam et" or "Yeni ana parolayı kaydet";
        if (TryFindResource(primary ? "VaultPrimaryButton" : "VaultSecondaryButton") is Style style) button.Style = style;
        button.Click += (_, _) => Run(action); return button;
    }
    private static PasswordBox PasswordInput(string name) => new() { Name = name, Style = (Style)Application.Current.FindResource("VaultPassword"), MaxLength = 1024, MaxWidth = 380, HorizontalAlignment = HorizontalAlignment.Left, Width = 340 };
    private void WrapBody()
    {
        var content = new StackPanel();
        var children = body.Children.Cast<UIElement>().ToArray(); body.Children.Clear();
        foreach (var child in children) content.Children.Add(child);
        body.Children.Add(new Border { Style = (Style)FindResource("CardCompact"), Padding = new Thickness(14), MaxWidth = 600, HorizontalAlignment = HorizontalAlignment.Left, Child = content });
    }
    private void Run(Action action)
    {
        status.Text = ""; status.Foreground = ThemeBrush("Gold500Brush");
        try { action(); }
        catch (CryptographicException) { status.Foreground = Brushes.LightSalmon; status.Text = "Parola / kurtarma anahtarı yanlış veya kasa dosyası bozulmuş. Kayıtlı kasa değiştirilmedi."; }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException or ExternalException)
        { status.Foreground = Brushes.LightSalmon; status.Text = ex is ArgumentException or IOException ? ex.Message : "İşlem tamamlanamadı. Kasa dosyası ve erişim izinlerini kontrol edin."; }
    }

    private void ShowLocked()
    {
        body.Children.Clear(); fields.Clear();
        body.Children.Add(Text(store.Exists ? "Kasanız kilitli" : "Kendi kasanızı oluşturun", 17, true));
        body.Children.Add(Text(store.Exists ? "Ana parolanız bu bilgisayarda saklanmaz. Açmak için parolanızı girin." : "İki işyerinin bilgileri başlangıçta boştur. " + VaultStore.PasswordRequirements));
        body.Children.Add(Text("Ana parola"));
        var password = PasswordInput("VaultPassword"); body.Children.Add(password);
        if (!store.Exists)
        {
            body.Children.Add(Text("Ana parola tekrar"));
            var confirm = PasswordInput("VaultPasswordConfirm"); body.Children.Add(confirm);
            body.Children.Add(ActionButton("Kasamı oluştur", () =>
            {
                if (password.Password != confirm.Password) throw new ArgumentException("Parolalar aynı değil.");
                session = store.Create(password.Password, out var recovery);
                password.Clear(); confirm.Clear(); ShowRecovery(recovery);
            }));
        }
        else
        {
            body.Children.Add(ActionButton("Kasayı aç", () =>
            {
                session = store.Unlock(password.Password); password.Clear(); ShowEditor();
            }));
            password.KeyDown += (_, e) => { if (e.Key == Key.Enter) Run(() => { session = store.Unlock(password.Password); password.Clear(); ShowEditor(); }); };
            body.Children.Add(ActionButton("Ana parolamı unuttum", ShowRecover));
        }
        body.Children.Add(new Separator { Margin = new Thickness(0, 12, 0, 8), Background = ThemeBrush("PremiumInputBorderBrush") });
        body.Children.Add(Text("Başka bilgisayardan kasa taşıma", 16, true));
        body.Children.Add(Text("Şifreli .edhskasa dosyanızı seçin. Üstteki ana parola alanına, taşıdığınız kasanın parolasını yazın. Mevcut kasa varsa değiştirmeden önce onay istenir."));
        body.Children.Add(ActionButton("Şifreli kasayı içe aktar…", () =>
        {
            var dialog = new OpenFileDialog { Filter = "Şifreli kasa (*.edhskasa)|*.edhskasa", CheckFileExists = true };
            var selected = Dialog(() => dialog.ShowDialog(this));
            if (selected != true) return;
            var replace = store.Exists;
            if (replace && !Confirm("İçe aktarma bu bilgisayardaki mevcut kasanın yerini alacak. Önce mevcut kasanızı dışa aktararak yedeklediğinizden emin olun. Devam edilsin mi?")) return;
            store.Import(dialog.FileName, password.Password, replace);
            password.Clear(); ShowLocked(); status.Text = "Şifreli kasa içe aktarıldı. Ana parolanızla açabilirsiniz.";
        }));
        WrapBody(); password.Focus();
    }

    private void ShowRecovery(string code)
    {
        body.Children.Clear();
        body.Children.Add(Text("Kasanız oluşturuldu • Kurtarma anahtarınızı saklayın", 17, true));
        body.Children.Add(Text("Bu anahtar yalnızca şimdi gösterilir. Ana parolanızı unutursanız kasayı açabilir. Kağıda yazarak veya ayrı ve güvenli bir yerde saklayın; kasa dosyasıyla birlikte tutmayın."));
        var recovery = new TextBox { Style = (Style)FindResource("VaultReveal"), Height = double.NaN, Text = code, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, FontFamily = new FontFamily("Consolas"), FontSize = 17, Padding = new Thickness(10), Margin = new Thickness(0, 8, 0, 8) };
        recovery.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, (_, e) => { Copy(recovery.SelectedText.Length > 0 ? recovery.SelectedText : recovery.Text); e.Handled = true; }));
        body.Children.Add(recovery);
        body.Children.Add(ActionButton("Kurtarma anahtarını kopyala", () => Copy(recovery.Text)));
        var acknowledge = new CheckBox { Content = "Kurtarma anahtarını ayrı, güvenli bir yere kaydettim.", Foreground = Brushes.White, Margin = new Thickness(0, 10, 0, 8) };
        body.Children.Add(acknowledge);
        body.Children.Add(ActionButton("Kasaya devam et", () =>
        {
            if (acknowledge.IsChecked != true) throw new ArgumentException("Önce kurtarma anahtarınızı saklayın ve kutucuğu işaretleyin.");
            recovery.Clear(); ClearClipboard(); ShowEditor();
        }));
        WrapBody(); lastInput = DateTime.UtcNow;
    }

    private void ShowRecover()
    {
        body.Children.Clear();
        body.Children.Add(Text("Kurtarma anahtarıyla aç", 17, true));
        body.Children.Add(Text("Kasayı oluştururken sakladığınız 64 karakterlik kurtarma anahtarını girin. Açtıktan sonra yeni bir ana parola belirleyin."));
        var recovery = PasswordInput("RecoveryCode"); body.Children.Add(recovery);
        body.Children.Add(ActionButton("Kurtarma anahtarıyla aç", () =>
        { session = store.Recover(recovery.Password.Replace(" ", "").Replace("-", "").Trim()); recovery.Clear(); ShowEditor(); status.Text = "Kasa kurtarma anahtarıyla açıldı. Yeni ana parolanızı belirleyin."; }));
        body.Children.Add(ActionButton("Geri", ShowLocked));
        WrapBody();
    }

    private void ShowEditor()
    {
        if (session is null) return;
        var data = session.Read();
        body.Children.Clear(); fields.Clear(); dirty = false;
        var toolbar = new WrapPanel();
        toolbar.Children.Add(ActionButton("Bilgileri kaydet", Save));
        toolbar.Children.Add(ActionButton("Şimdi kilitle", Lock));
        toolbar.Children.Add(ActionButton("Şifreli kasayı dışa aktar…", Export));
        body.Children.Add(toolbar);
        var grid = new Grid { Margin = new Thickness(0, 8, 0, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition());
        var labels = new[] { "Kullanıcı kodu", "Sistem şifresi", "İşyeri şifresi", "İşyeri kodu", "İşyeri sicil no" };
        for (var group = 0; group < 2; group++)
        {
            var panel = new StackPanel();
            var groupTitle = Text(group == 0 ? "Ücretli Öğretmenler" : "Usta Öğreticiler", 15, true); groupTitle.Foreground = ThemeBrush("Gold500Brush"); groupTitle.Margin = new Thickness(0, 0, 0, 8);
            panel.Children.Add(groupTitle);
            var item = data.Groups[group];
            var values = new[] { item.UserCode, item.SystemPassword, item.WorkplacePassword, item.WorkplaceCode, item.RegistrationNumber };
            for (var field = 0; field < 5; field++)
            {
                var input = new SecretField(labels[field], values[field], Copy, () => { dirty = true; status.Text = "Kaydedilmemiş değişiklikler var. Ayrılmadan önce Bilgileri kaydet düğmesini kullanın."; });
                fields.Add(input); panel.Children.Add(input);
            }
            var card = new Border { Style = (Style)FindResource("CardCompact"), Padding = new Thickness(12), Margin = new Thickness(group == 0 ? 0 : 6, 0, group == 0 ? 6 : 0, 0), Child = panel };
            Grid.SetColumn(card, group); grid.Children.Add(card);
        }
        body.Children.Add(grid);
        var passwordPanel = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        var passwordSection = new Expander { Header = "Ana parolayı değiştir", Foreground = ThemeBrush("PremiumMutedTextBrush"), FontSize = 12, Content = passwordPanel };
        body.Children.Add(new Border { Style = (Style)FindResource("CardCompact"), Padding = new Thickness(10), Child = passwordSection });
        passwordPanel.Children.Add(Text("Yeni parola / tekrar. " + VaultStore.PasswordRequirements + " Eski dışa aktarılmış kopyalar eski parolalarıyla açılmaya devam eder.", 11));
        var password = PasswordInput("NewVaultPassword"); var confirm = PasswordInput("NewVaultPasswordConfirm");
        password.Margin = new Thickness(0, 0, 0, 5);
        passwordPanel.Children.Add(password); passwordPanel.Children.Add(confirm);
        passwordPanel.Children.Add(ActionButton("Yeni ana parolayı kaydet", () =>
        {
            if (dirty) throw new ArgumentException("Önce işyeri bilgilerini kaydedin.");
            if (password.Password != confirm.Password) throw new ArgumentException("Parolalar aynı değil.");
            session!.ChangePassword(password.Password); password.Clear(); confirm.Clear(); status.Text = "Ana parola değiştirildi. Kurtarma anahtarınız geçerliliğini korur.";
        }));
        lastInput = DateTime.UtcNow;
    }

    private void Save()
    {
        if (session is null) throw new InvalidOperationException("Kasa kilitli.");
        VaultWorkplace Group(int offset) => new(fields[offset].Value, fields[offset + 1].Value, fields[offset + 2].Value, fields[offset + 3].Value, fields[offset + 4].Value);
        session.Save(new VaultData([Group(0), Group(5)])); dirty = false; status.Text = "İki işyerinin bilgileri şifrelenerek kaydedildi.";
    }
    private void Export()
    {
        if (dirty) throw new ArgumentException("Önce değişikliklerinizi kaydedin.");
        var dialog = new SaveFileDialog { Filter = "Şifreli kasa (*.edhskasa)|*.edhskasa", FileName = "isyeri-kasa-yedek.edhskasa", DefaultExt = ".edhskasa", AddExtension = true };
        if (Dialog(() => dialog.ShowDialog(this)) != true) return;
        if (session is null) throw new ArgumentException("Kasa kilitlendi. Yeniden açıp dışa aktarın.");
        session.Export(dialog.FileName); status.Text = "Şifreli kopya kaydedildi. Diğer bilgisayarda aynı ana parolayla içe aktarabilirsiniz.";
    }

    private T Dialog<T>(Func<T> action)
    {
        internalDialog = true;
        try { return action(); }
        finally { internalDialog = false; if (DateTime.UtcNow - lastInput >= TimeSpan.FromMinutes(2)) Lock(); }
    }
    private bool Confirm(string message) => Dialog(() => MessageBox.Show(this, message, "Kasa değiştirilecek", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes);
    private void Lock()
    {
        session?.Dispose(); session = null;
        foreach (var field in fields) field.Clear();
        // Remove password/recovery controls as well as workplace controls from the visual tree.
        ClearSecrets(body);
        var hadChanges = dirty; dirty = false; ClearClipboard(); ShowLocked();
        status.Text = hadChanges ? "Kasa kilitlendi. Kaydedilmemiş değişiklikler silindi." : "Kasa kilitli.";
    }
    private static void ClearSecrets(DependencyObject element)
    {
        if (element is PasswordBox password) password.Clear();
        if (element is TextBox text) text.Clear();
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++) ClearSecrets(VisualTreeHelper.GetChild(element, i));
    }
    private void SessionSwitch(object sender, SessionSwitchEventArgs e)
    { if (e.Reason is SessionSwitchReason.SessionLock or SessionSwitchReason.SessionLogoff or SessionSwitchReason.RemoteDisconnect or SessionSwitchReason.ConsoleDisconnect) Dispatcher.Invoke(Lock); }
    private void Tick(object? sender, EventArgs e)
    {
        foreach (var field in fields) field.HideIfExpired();
        if (clipboardValue is not null && DateTime.UtcNow >= clipboardExpiry) ClearClipboard();
        if (session is not null && DateTime.UtcNow - lastInput >= TimeSpan.FromMinutes(2)) Lock();
    }
    private void Copy(string value)
    {
        if (value.Length == 0) return;
        var data = new DataObject(); data.SetText(value);
        data.SetData("CanIncludeInClipboardHistory", new MemoryStream(BitConverter.GetBytes(0)));
        data.SetData("CanUploadToCloudClipboard", new MemoryStream(BitConverter.GetBytes(0)));
        Clipboard.SetDataObject(data, true); clipboardValue = value; clipboardExpiry = DateTime.UtcNow.AddSeconds(20);
        status.Text = "Kopyalandı. Pano 20 saniye sonra, içeriği değişmemişse temizlenir.";
    }
    private void ClearClipboard()
    {
        try { if (clipboardValue is not null && Clipboard.ContainsText() && Clipboard.GetText() == clipboardValue) Clipboard.Clear(); }
        catch (ExternalException) { /* Another application may hold the clipboard. Never log its contents. */ }
        finally { clipboardValue = null; }
    }

    private sealed class SecretField : StackPanel
    {
        private readonly PasswordBox input;
        private readonly TextBox revealed;
        private DateTime hideAt;
        public string Value => input.Password;
        public SecretField(string title, string value, Action<string> copy, Action changed)
        {
            var label = Text(title, 11); label.Margin = new Thickness(0, 0, 0, 3); Children.Add(label);
            var row = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
            var copyButton = new Button { Content = "Kopyala", Margin = new Thickness(4, 0, 0, 0) };
            copyButton.Click += (_, _) => copy(Value); DockPanel.SetDock(copyButton, Dock.Right); row.Children.Add(copyButton);
            var show = new Button { Content = "Göster", Margin = new Thickness(4, 0, 0, 0) };
            if (Application.Current.TryFindResource("VaultSecondaryButton") is Style style) { copyButton.Style = style; show.Style = style; }
            DockPanel.SetDock(show, Dock.Right); row.Children.Add(show);
            input = new PasswordBox { Style = (Style)Application.Current.FindResource("VaultPassword"), Password = value, MaxLength = 4096 };
            revealed = new TextBox { Style = (Style)Application.Current.FindResource("VaultReveal"), IsReadOnly = true, Visibility = Visibility.Collapsed };
            revealed.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, (_, e) => { copy(revealed.SelectedText.Length > 0 ? revealed.SelectedText : revealed.Text); e.Handled = true; }));
            var stack = new Grid(); stack.Children.Add(input); stack.Children.Add(revealed); row.Children.Add(stack);
            input.PasswordChanged += (_, _) => { Hide(); changed(); };
            show.Click += (_, _) =>
            {
                if (revealed.Visibility == Visibility.Visible) Hide();
                else { revealed.Text = Value; input.Visibility = Visibility.Collapsed; revealed.Visibility = Visibility.Visible; hideAt = DateTime.UtcNow.AddSeconds(15); }
            };
            Children.Add(row);
        }
        private void Hide() { revealed.Clear(); revealed.Visibility = Visibility.Collapsed; input.Visibility = Visibility.Visible; }
        public void HideIfExpired() { if (revealed.Visibility == Visibility.Visible && DateTime.UtcNow >= hideAt) Hide(); }
        public void Clear() { input.Clear(); Hide(); }
    }
}
