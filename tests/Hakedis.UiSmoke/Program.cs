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
        model.Registration.Editor.Iban="TR330006100519786457841326";
        model.Registration.Editor.OccupationCode="2341.04";model.SaveNewTeacher();
        model.Monthly.DayHours = "117";
        var notifications=new List<string>();
        var window = new MainWindow(model,notifications.Add) { ShowActivated = false, ShowInTaskbar = false };
        if(window.Width!=1366 || window.Height!=768)throw new InvalidOperationException("Varsayılan laptop boyutu 1366×768 değil.");
        window.Show();
        if(window.ActualWidth>SystemParameters.WorkArea.Width+1 || window.ActualHeight>SystemParameters.WorkArea.Height+1)
            throw new InvalidOperationException("Açılış penceresi görev çubuğu dışındaki çalışma alanına sığmadı.");
        model.RegistrationModule = model.Workspace.Modules.Single(m => m.Id == Hakedis.Contracts.ModuleId.Ortaogretim);
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        if(model.Monthly.Teachers.Count!=1 || model.Monthly.DayHours!="117")
            throw new InvalidOperationException("Bağımsız modül seçimleri birbirini değiştirdi.");
        model.Registration.Editor.FullName = "Başka Modül Öğretmeni"; model.SaveNewTeacher();
        model.RegistrationModule = model.Workspace.Modules.Single(m => m.Id == Hakedis.Contracts.ModuleId.TemelEgitim);
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        if(model.Calculation.Rows.Single().Result?.Gross!=25806.89m)throw new InvalidOperationException("Canlı tablo güncellenmedi.");
        if(model.Calculation.Rows.Single().MissingDays!="14")throw new InvalidOperationException("Eylül ayı SGK yan bilgisi yanlış.");
        foreach (var width in new[] { 1366, 1040 })
        {
            window.Width = width; window.Height = width == 1366 ? 768 : 728;
            foreach (var panel in new[] { "Entry", "Calculation", "Teachers", "Constants", "Outputs" })
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
        grid.SelectedItem=row;grid.ScrollIntoView(row,hoursColumn);
        window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);grid.UpdateLayout();
        grid.CurrentCell=new DataGridCellInfo(row,hoursColumn);grid.Focus();
        if(!grid.BeginEdit())throw new InvalidOperationException("Görünür saat hücresinde BeginEdit başarısız.");
        window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        var editor=Descendants<TextBox>(grid).FirstOrDefault(t=>t.IsVisible);
        if(editor is null)throw new InvalidOperationException("Hibrit hücre düzenleme açılamadı.");
        editor.Text="108";window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(row.Result?.Gross!=23821.74m || model.Monthly.DayHours!="108")throw new InvalidOperationException("Hücre yazılırken hesap ve aylık panel güncellenmedi.");
        if(row.MissingDays!="15")throw new InvalidOperationException("Hücre yazılırken SGK bilgisi güncellenmedi.");
        grid.CommitEdit(DataGridEditingUnit.Cell,true);grid.CommitEdit(DataGridEditingUnit.Row,true);
        Descendants<Button>(window).Single(b=>b.IsVisible && b.Content as string=="Hakediş oluştur").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if(!model.Calculation.Status.Contains("oluşturuldu"))throw new InvalidOperationException("Hakediş oluştur düğmesi kalıcı kayıt yapmadı: "+model.Calculation.Status);
        var expectedHeaders=new[]{Hakedis.Contracts.ExcelHeadings.Sequence,Hakedis.Contracts.ExcelHeadings.FullName,
            Hakedis.Contracts.ExcelHeadings.IdentityNumber,Hakedis.Contracts.ExcelHeadings.Iban,Hakedis.Contracts.ExcelHeadings.StartDate,
            Hakedis.Contracts.ExcelHeadings.EndDate,
            Hakedis.Contracts.ExcelHeadings.Occupation,Hakedis.Contracts.ExcelHeadings.School,Hakedis.Contracts.ExcelHeadings.IbanCheck,
            Hakedis.Contracts.ExcelHeadings.DayHours,Hakedis.Contracts.ExcelHeadings.DykHours,Hakedis.Contracts.ExcelHeadings.Hours,
            Hakedis.Contracts.ExcelHeadings.Garnishment};
        if(!grid.Columns.Select(c=>c.Header as string).SequenceEqual(expectedHeaders))throw new InvalidOperationException("VERİ GİRİŞİ sütunları ve sırası uyuşmuyor.");
        if(grid.RowHeight!=30)throw new InvalidOperationException("Hibrit satır yüksekliği 30 değil.");
        var edit=(Button)window.FindName("EditHybridButton");var delete=(Button)window.FindName("DeleteHybridButton");
        var saveHybrid=(Button)window.FindName("SaveHybridButton");
        grid.SelectedItem=null;window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(edit.IsEnabled || delete.IsEnabled)throw new InvalidOperationException("Seçim yokken satır düğmeleri açık.");
        grid.SelectedItem=row;window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(!edit.IsEnabled || !delete.IsEnabled)throw new InvalidOperationException("Seçili satır düğmeleri pasif.");
        edit.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(!model.EntryVisible || !model.Registration.IsEditingRecord || model.Registration.Editor.FullName!=row.FullName)
            throw new InvalidOperationException("Satır düzenlemesi öğretmen formunu açmadı.");
        var saveRegistration=(Button)window.FindName("SaveRegistrationButton");var cancelRegistration=(Button)window.FindName("CancelRegistrationButton");
        if(saveRegistration.Content as string!="Öğretmen Değişiklik kaydet" || cancelRegistration.Content as string!="Vazgeç")
            throw new InvalidOperationException("Düzenleme düğmeleri değişmedi.");
        model.Registration.Editor.FullName="Kaydedilmez";cancelRegistration.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(!model.CalculationVisible || row.FullName!="Deneme Öğretmeni" || model.Registration.IsEditingRecord)
            throw new InvalidOperationException("Vazgeç kayıtları değiştirdi.");
        edit.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));model.Registration.Editor.FullName="Güncellenen Öğretmen";
        saveRegistration.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(!model.CalculationVisible || row.FullName!="Güncellenen Öğretmen" || notifications.Count!=1)
            throw new InvalidOperationException("Öğretmen kaydı / dönüş / bildirim başarısız.");
        if(saveRegistration.Content as string!="Öğretmeni Kaydet" || cancelRegistration.Content as string!="Formu Temizle")
            throw new InvalidOperationException("Öğretmen düğmeleri normal moda dönmedi.");
        row.School="Yeni okul";row.DayHours="90";window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(!saveHybrid.IsEnabled)throw new InvalidOperationException("Değişiklikleri kaydet etkinleşmedi.");
        saveHybrid.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(saveHybrid.IsEnabled || notifications.Count!=2)throw new InvalidOperationException("Toplu kayıt değişiklikleri kabul etmedi.");
        grid.ScrollIntoView(row,grid.Columns[1]);window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        foreach(var text in Descendants<TextBlock>(grid).Where(t=>System.Windows.Data.BindingOperations.GetBinding(t,TextBlock.TextProperty)?.Path?.Path=="FullName"))
            if(text.TextAlignment!=TextAlignment.Center || text.VerticalAlignment!=VerticalAlignment.Center)
                throw new InvalidOperationException("Hibrit hücre ortalanmadı.");
        Capture(window,Path.Combine(output,"Calculation-edited.png"));
        // Yeni panel yerleşimi ve salt okunur ortak sabitler.
        model.Navigate("Teachers");window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);window.UpdateLayout();
        var listCard=(FrameworkElement)window.FindName("TeacherListCard");var detailsCard=(FrameworkElement)window.FindName("TeacherDetailsCard");
        if(detailsCard.TranslatePoint(new Point(0,0),window).X<=listCard.TranslatePoint(new Point(0,0),window).X || ((DataGrid)window.FindName("TeacherListGrid")).Columns.Count!=2)
            throw new InvalidOperationException("Öğretmen kartları yan yana / iki sütun değil.");
        model.Navigate("Constants");window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(Descendants<ComboBox>(window).Any(c=>c.IsVisible))throw new InvalidOperationException("Ortak sabit panelinde modül/dönem seçimi kalmış.");
        if(Descendants<TextBox>(window).Where(t=>t.IsVisible).Any(t=>!t.IsReadOnly))throw new InvalidOperationException("Sabitler başlangıçta salt okunur değil.");
        var update=(Button)window.FindName("ConstantsUpdateButton");update.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(model.CommonConstants.IsReadOnly || update.Content as string!="Dönem sabitlerini değiştir")throw new InvalidOperationException("Sabit güncelleme modu açılmadı.");
        update.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(!model.CommonConstants.IsReadOnly || update.Content as string!="Güncelle")throw new InvalidOperationException("Sabit kaydı kilitlemedi.");
        model.Registration.Reset();model.ClearCurrentWork();model.Navigate("Outputs");model.Outputs.Selected=model.Outputs.Records.Single();
        var archived=model.Outputs.ReadSelected();var archiveBytes=File.ReadAllBytes(archived.Path);
        model.BeginArchiveEdit();window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(!((Button)window.FindName("SaveArchiveButton")).IsVisible || ((Button)window.FindName("SaveHybridButton")).IsVisible)
            throw new InvalidOperationException("Geçmiş düzenlemede düğmeler doğru değişmedi.");
        if(Descendants<Button>(window).Any(b=>b.IsVisible && b.Content as string=="Hakediş oluştur"))throw new InvalidOperationException("Geçmiş düzenlemede yeni kayıt düğmesi açık.");
        model.Calculation.Rows[0].DayHours="75";
        Capture(window,Path.Combine(output,"Archive-edit.png"));
        ((Button)window.FindName("CancelArchiveButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if(!archiveBytes.SequenceEqual(File.ReadAllBytes(archived.Path)))throw new InvalidOperationException("Geçmiş Vazgeç dosyayı değiştirdi.");
        var details=new Hakedis.Contracts.ReportDetails("Örnek İlçe Milli Eğitim Müdürlüğü","Örnek Banka Şubesi","Deneme Görevli","Şube Müdürü","Deneme Yetkili","İlçe Milli Eğitim Müdürü","Deneme Sayman");
        var service=new PaymentReportService();var exports=service.Export(archived,details,Path.Combine(output,"pdf-test"));
        if(Directory.GetFiles(exports,"*.pdf").Length!=3)throw new InvalidOperationException("Üç çıktı PDF oluşmadı.");
        File.WriteAllText(Path.Combine(output,"pdf-folder.txt"),exports);
        VerifyReportPages(service,archived,details,output,"normal");
        var baseRow=archived.Document.Data.Rows[0];
        var many=Enumerable.Range(1,65).Select(i=>baseRow with {Teacher=baseRow.Teacher with {Id=Guid.NewGuid(),FullName=$"ÇĞİÖŞÜ Deneme Öğretmeni {i} Uzun İsim Soyisim"}})
            .Select(r=>r with {Input=r.Input with {TeacherId=r.Teacher.Id}}).ToArray();
        var large=archived with {Document=archived.Document with {Data=archived.Document.Data with {Rows=many,Movement=new(0,many.Length,0,many.Select(r=>r.Teacher.Id).ToArray())}}};
        var largeExports=service.Export(large,details with {Institution="Çok uzun kurum adı olan Örnek İlçe Milli Eğitim Müdürlüğü - Okul Öncesi ve İlköğretim Okulları"},Path.Combine(output,"pdf-large-test"));
        File.WriteAllText(Path.Combine(output,"pdf-large-folder.txt"),largeExports);
        VerifyReportPages(service,large,details,output,"large");
        model.Navigate("Calculation");model.Calculation.Reload();window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        var selectors=Descendants<ComboBox>(window).Where(c=>c.IsVisible).ToArray();
        selectors.Single(c=>c.Items.Count==201).SelectedItem=2027;
        selectors.Single(c=>c.Items.Count==12).SelectedValue=11;
        window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(model.Calculation.PeriodText!="Kasım-2027" || model.Calculation.Rows[0].HasInput)throw new InvalidOperationException("Dönem seçiminde kayıtlar karıştı.");
        Capture(window,Path.Combine(output,"Calculation-Kasim-2027.png"));
        window.Close();
        if (listener.Errors.Count > 0) { foreach (var message in listener.Errors) Console.Error.WriteLine(message); return 1; }
        Console.WriteLine($"10 WPF panel görüntüsü, ortak sabit kilidi, yan yana öğretmen kartları, arşiv iptali, üç PDF ve çok sayfalı yazıcı işi doğrulandı; binding hatası yok. {output}"); return 0;
    }
    private static void VerifyReportPages(PaymentReportService service,Hakedis.Storage.ArchivedPayment record,Hakedis.Contracts.ReportDetails details,string output,string prefix)
    {
        var prepared=service.Prepare(record,details);
        var print=service.CreatePrintPaginator(prepared,new Size(793,1122));
        if(print.PageCount!=prepared.Sum(r=>r.Pages.Length))throw new InvalidOperationException("Yazıcı işi üç belgenin tüm sayfalarını içermiyor.");
        for(var n=0;n<print.PageCount;n++)if(print.GetPage(n).Visual is null)throw new InvalidOperationException("Yazıcı sayfası boş.");
        var reportIndex=0;
        foreach(var report in prepared)
        {
            var fixedDocument=service.PrintDocument(report);fixedDocument.DocumentPaginator.ComputePageCount();
            if(fixedDocument.DocumentPaginator.PageCount!=report.Pages.Length)throw new InvalidOperationException("Yazdırma ve PDF sayfa sayıları farklı.");
            for(var i=0;i<report.Pages.Length;i++)
            {
                var sheet=report.Pages[i];
                if(sheet.Cells.Any(c=>c.X<0 || c.Y<0 || c.X+c.Width>sheet.Width+.1 || c.Y+c.Height>sheet.Height+.1))throw new InvalidOperationException("Rapor sayfa sınırını aşıyor.");
                var page=fixedDocument.Pages[i].GetPageRoot(false);page.Measure(new Size(page.Width,page.Height));page.Arrange(new Rect(0,0,page.Width,page.Height));page.UpdateLayout();
                Capture(page,Path.Combine(output,$"report-{prefix}-{reportIndex}-{i+1}.png"));
            }
            reportIndex++;
        }
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
