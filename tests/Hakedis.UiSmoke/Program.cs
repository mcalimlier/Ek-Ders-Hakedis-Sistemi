using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Hakedis.App;
using System.Windows.Controls;
using System.Windows.Input;

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
        model.Registration.Editor.Branch = "Sınıf Öğretmenliği";
        model.Registration.Editor.Iban="TR330006100519786457841326";
        model.Registration.Editor.OccupationCode="2341.04";model.SaveNewTeacher();
        model.Monthly.DayHours = "117";
        var window = new MainWindow(model) { ShowActivated = false, ShowInTaskbar = false };
        window.Show();
        model.RegistrationModule = model.Workspace.Modules.Single(m => m.Id == Hakedis.Contracts.ModuleId.Ortaogretim);
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        if(model.Monthly.Teachers.Count!=1 || model.Monthly.DayHours!="117")
            throw new InvalidOperationException("Bağımsız modül seçimleri birbirini değiştirdi.");
        model.Registration.Editor.FullName = "Başka Modül Öğretmeni"; model.SaveNewTeacher();
        model.RegistrationModule = model.Workspace.Modules.Single(m => m.Id == Hakedis.Contracts.ModuleId.TemelEgitim);
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        if(model.Calculation.Rows.Single().Result?.Gross!=25806.89m)throw new InvalidOperationException("Canlı tablo güncellenmedi.");
        foreach (var width in new[] { 1440, 1040 })
        {
            window.Width = width; window.Height = width == 1440 ? 920 : 760;
            foreach (var panel in new[] { "Entry", "Calculation", "Teachers", "Constants" })
            {
                model.Navigate(panel);
                if (panel == "Teachers") model.Teachers.Selected = model.Teachers.Records.Single();
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout();
                var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(window);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var file = File.Create(Path.Combine(output, $"{panel}-{width}.png")); encoder.Save(file);
                if(panel=="Calculation")
                {
                    var scroll=(ScrollViewer)window.FindName("CalculationScroll");scroll.ScrollToEnd();
                    window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
                    Capture(window,Path.Combine(output,$"Calculation-cards-{width}.png"));scroll.ScrollToTop();
                }
            }
        }
        model.Navigate("Entry");window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        var picker=Descendants<SearchableOccupation>(window).First();
        ((System.Windows.Controls.Primitives.ToggleButton)picker.FindName("Picker")).IsChecked=true;
        window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        var search=(TextBox)picker.FindName("Search");search.Text="sınıf öğretmen";
        window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        var options=(ListBox)picker.FindName("Options");
        if(options.Items.Count==0 || options.Items.Count>=7190)throw new InvalidOperationException("Meslek araması listeyi filtrelemedi.");
        var popup=(System.Windows.Controls.Primitives.Popup)picker.FindName("DropDown");
        Capture((FrameworkElement)popup.Child,Path.Combine(output,"occupation-search.png"));
        options.SelectedIndex=0;
        options.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(options)!,Environment.TickCount,Key.Enter){RoutedEvent=Keyboard.PreviewKeyDownEvent});
        if(string.IsNullOrEmpty(model.Registration.Editor.OccupationCode))throw new InvalidOperationException("Seçilen meslek kodu kayıt formuna aktarılmadı.");
        model.Navigate("Calculation");window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        var grid=(DataGrid)window.FindName("HybridGrid");
        var row=model.Calculation.Rows.Single();
        var hoursColumn=grid.Columns.OfType<DataGridTextColumn>().Single(c=>(c.Binding as System.Windows.Data.Binding)?.Path.Path=="DayHours");
        grid.CurrentCell=new DataGridCellInfo(row,hoursColumn);grid.BeginEdit();
        window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        var editor=Descendants<TextBox>(grid).FirstOrDefault(t=>t.IsVisible);
        if(editor is null)throw new InvalidOperationException("Hibrit hücre düzenleme açılamadı.");
        editor.Text="108";window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(row.Result?.Gross!=23821.74m || model.Monthly.DayHours!="108")throw new InvalidOperationException("Hücre yazılırken hesap ve aylık panel güncellenmedi.");
        grid.CommitEdit(DataGridEditingUnit.Cell,true);grid.CommitEdit(DataGridEditingUnit.Row,true);
        Descendants<Button>(window).Single(b=>b.IsVisible && b.Content as string=="Hakediş oluştur").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if(!model.Calculation.Status.Contains("oluşturuldu"))throw new InvalidOperationException("Hakediş oluştur düğmesi kalıcı kayıt yapmadı: "+model.Calculation.Status);
        var selectors=Descendants<ComboBox>(window).Where(c=>c.IsVisible).ToArray();
        selectors.Single(c=>c.Items.Count==201).SelectedItem=2027;
        selectors.Single(c=>c.Items.Count==12).SelectedValue=11;
        window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(model.Calculation.PeriodText!="Kasım-2027" || model.Calculation.Rows[0].HasInput)throw new InvalidOperationException("Dönem seçiminde kayıtlar karıştı.");
        Capture(window,Path.Combine(output,"Calculation-Kasim-2027.png"));
        window.Close();
        if (listener.Errors.Count > 0) { foreach (var message in listener.Errors) Console.Error.WriteLine(message); return 1; }
        Console.WriteLine($"8 WPF panel görüntüsü + meslek araması; canlı hücre, bağımsız modül ve Kasım-2027 seçimi doğrulandı; binding hatası yok. {output}"); return 0;
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T:DependencyObject
    {for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is T t)yield return t;foreach(var nested in Descendants<T>(child))yield return nested;}}
    private static void Capture(FrameworkElement element,string path)
    {element.UpdateLayout();var bmp=new RenderTargetBitmap((int)element.ActualWidth,(int)element.ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(element);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bmp));using var file=File.Create(path);encoder.Save(file);}
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
