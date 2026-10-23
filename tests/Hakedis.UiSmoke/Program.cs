using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Hakedis.App;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var output = Path.GetFullPath(args.FirstOrDefault() ?? "artifacts/ui-review"); Directory.CreateDirectory(output);
        var listener = new BindingErrors(); PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
        var app = new Hakedis.App.App(); app.InitializeComponent();
        var model = new ShellViewModel(Path.Combine(output, Guid.NewGuid().ToString("N"), "ogretmenler.json"));
        model.Registration.Editor.FullName = "Deneme Öğretmeni"; model.Registration.Editor.School = "Örnek İlkokulu";
        model.Registration.Editor.Branch = "Sınıf Öğretmenliği"; model.SaveNewTeacher();
        model.Workspace.DayHours = "117";
        var window = new MainWindow { DataContext = model, ShowActivated = false, ShowInTaskbar = false };
        window.Show();
        foreach (var width in new[] { 1440, 1040 })
        {
            window.Width = width; window.Height = width == 1440 ? 920 : 760;
            foreach (var panel in new[] { "Entry", "Calculation", "Teachers" })
            {
                model.Navigate(panel);
                if (panel == "Calculation") model.Workspace.Calculate();
                if (panel == "Teachers") model.Teachers.Selected = model.Teachers.Records.Single();
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout();
                var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(window);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var file = File.Create(Path.Combine(output, $"{panel}-{width}.png")); encoder.Save(file);
            }
        }
        window.Close();
        if (listener.Errors.Count > 0) { foreach (var message in listener.Errors) Console.Error.WriteLine(message); return 1; }
        Console.WriteLine($"6 WPF görüntüsü oluşturuldu; kaynaklar yüklendi, binding hatası yok. {output}"); return 0;
    }
    private sealed class BindingErrors : TraceListener
    {
        public List<string> Errors { get; } = [];
        public override void Write(string? message) { }
        public override void WriteLine(string? message)
        {
            if (message?.Contains("Error:", StringComparison.OrdinalIgnoreCase) == true) Errors.Add(message);
        }
    }
}
