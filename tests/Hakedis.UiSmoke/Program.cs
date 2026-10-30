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
        // Load only UI resources: production App.OnStartup opens the real desktop data.
        var app = new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};
        foreach(var theme in new[]{"Oehs","Workspace","PaymentCards"})
            app.Resources.MergedDictionaries.Add(new ResourceDictionary {Source=new Uri($"/Ek Ders Hakediş Sistemi;component/Themes/{theme}.xaml",UriKind.Relative)});
        if(args.Length==3 && args[1]=="--verify-data-compat")
        {
            var root=args[2];var teachers=new Hakedis.Storage.TeacherDirectory(Path.Combine(root,"ogretmenler.json")).ReadCanonical();
            var months=new Hakedis.Storage.MonthlyPayrollStore(root);var monthlyCount=0;
            var monthlyRoot=Path.Combine(root,"AylikKayitlar");
            foreach(var file in Directory.EnumerateFiles(monthlyRoot,"????-??.json",SearchOption.AllDirectories))
            {
                var module=Enum.Parse<Hakedis.Contracts.ModuleId>(new DirectoryInfo(Path.GetDirectoryName(file)!).Name);
                var parts=Path.GetFileNameWithoutExtension(file).Split('-');
                monthlyCount+=months.Read(module,int.Parse(parts[0]),int.Parse(parts[1])).Entries.Length;
            }
            var archives=new Hakedis.Storage.PaymentArchiveStore(root).ReadAll();
            Console.WriteLine($"Eski veri uyumluluğu: {teachers.Count} kişi, {monthlyCount} aylık öğretmen girdisi, {archives.Count} hakediş arşivi başarıyla okundu.");return 0;
        }
        if(args.Length==2 && args[1]=="--verify-save-notifications")
        {
            SaveNotificationChecks.Run(output);return 0;
        }
        if(args.Length==3 && args[1]=="--verify-local-sgk")
        {
            var store=new Hakedis.Storage.PaymentArchiveStore(args[2]);var record=store.ReadAll().First();
            var original=File.ReadAllBytes(record.Path);var settings=new Hakedis.Storage.SgkXmlStore(args[2]).Read(record.Document.ModuleId);
            var sgkEditor=new SgkXmlEditor(record,settings){Confirmed=true};var bytes=sgkEditor.Build();
            var path=Hakedis.Storage.SgkXmlStore.Export(Path.Combine(output,"private-xml"),record.Document.ModuleId,record.Document.Data,sgkEditor.Workplace(),sgkEditor.ReadPeople());
            if(!original.SequenceEqual(File.ReadAllBytes(record.Path)))throw new InvalidOperationException("XML kaynak hakedişi değiştirdi.");
            if(!bytes.SequenceEqual(File.ReadAllBytes(path)))throw new InvalidOperationException("Dosya doğrulanan XML'den farklı.");
            var view=new SgkXmlWindow(record,new Hakedis.Storage.SgkXmlStore(args[2]),Path.Combine(output,"private-xml"));view.Show();
            view.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);Capture(view,Path.Combine(output,"SGK-local-review.png"));view.Close();
            File.WriteAllText(Path.Combine(output,"xml-local-path.txt"),path);
            Console.WriteLine($"Yerel SGK: {record.Count} sigortalı; özgün XSD doğrulaması geçti; kayıt dosyası değişmedi. Yükleme yapılmadı.");return 0;
        }
        if(args.Length==3 && args[1]=="--verify-local-archive")
        {
            var store=new Hakedis.Storage.PaymentArchiveStore(args[2]);var record=store.ReadAll().First();
            var original=File.ReadAllBytes(record.Path);var svc=new PaymentReportService();
            var fallback=new Hakedis.Storage.ReportDetailsStore(args[2]).Read();
            var folder=svc.Export(record,fallback,Path.Combine(output,"private-pdfs"));
            if(!original.SequenceEqual(File.ReadAllBytes(record.Path)))throw new InvalidOperationException("Çıktı kaynak kaydı değiştirdi.");
            VerifyReportPages(svc,record,fallback,output,"local");
            File.WriteAllText(Path.Combine(output,"pdf-local-folder.txt"),folder);
            Console.WriteLine($"Yerel kayıt: {record.Count} öğretmen, üç PDF; kaynak dosya değişmedi.");return 0;
        }
        var model = new ShellViewModel(Path.Combine(output, Guid.NewGuid().ToString("N"), "ogretmenler.json"));
        model.Registration.Editor.FullName = "Deneme Öğretmeni"; model.Registration.Editor.School = "Örnek İlkokulu";
        model.Registration.Editor.Iban="TR330006100519786457841326";
        model.Registration.Editor.OccupationCode="2341.04";model.Registration.Editor.IdentityNumber="10000000146";model.SaveNewTeacher();
        model.Monthly.DayHours = "117";
        var notifications=new List<string>();var archiveConfirm=false;var archivePrompts=new List<string>();
        var openedFolders=new List<string>();
        var window = new MainWindow(model,notifications.Add,(message,title)=>{archivePrompts.Add(message);return archiveConfirm;},openedFolders.Add,Path.Combine(output,"personal-pdfs")) { ShowActivated = false, ShowInTaskbar = false };
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
            foreach (var panel in new[] { "Entry", "Calculation", "Teachers", "Constants", "Details", "Outputs" })
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
                    Capture(window,Path.Combine(output,$"Calculation-cards-{width}.png"));
                    var metrics=(System.Windows.Controls.Primitives.UniformGrid)window.FindName("PaymentMetrics");
                    var cards=metrics.Children.OfType<Border>().ToArray();
                    if(cards.Length!=6 || cards.Select(c=>c.ActualHeight).Distinct().Count()!=1)throw new InvalidOperationException("Altı eş boyutlu kart korunmadı.");
                    var baseline=new Border{Style=(Style)window.FindResource("MetricCard"),Padding=new Thickness(16),Margin=new Thickness(0,0,10,10)};
                    var originalContent=new StackPanel();originalContent.Children.Add(new TextBlock{Text=Hakedis.Contracts.ExcelHeadings.BordroW6,Style=(Style)window.FindResource("CardSubtitle"),TextWrapping=TextWrapping.Wrap,MinHeight=45});
                    originalContent.Children.Add(new TextBlock{Text="3.612,96 TL",Style=(Style)window.FindResource("MetricValue"),FontSize=23});baseline.Child=originalContent;
                    baseline.Measure(new Size(cards[0].ActualWidth+10,double.PositiveInfinity));
                    if(Math.Abs(baseline.DesiredSize.Height-cards[0].DesiredSize.Height)>1.5)throw new InvalidOperationException($"Kart boyutu değişti: {baseline.DesiredSize.Height} / {cards[0].DesiredSize.Height}");
                    PersonalPayrollChecks.VerifyMetricRegions(cards);
                    var originalHours=model.Calculation.Rows[0].DayHours;
                    model.Calculation.Rows[0].DayHours="100000";window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);window.UpdateLayout();
                    PersonalPayrollChecks.VerifyMetricRegions(cards);
                    Capture(window,Path.Combine(output,$"Calculation-large-amounts-{width}.png"));
                    model.Calculation.Rows[0].DayHours=originalHours;window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
                    scroll.ScrollToTop();
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
        var hoursColumn=grid.Columns.Single(c=>c.Header as string=="GÜNDÜZ SAAT TOPLAMI");
        grid.SelectedItem=row;grid.ScrollIntoView(row,hoursColumn);
        window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);grid.UpdateLayout();
        grid.CurrentCell=new DataGridCellInfo(row,hoursColumn);grid.Focus();
        // Template column exposes a directly editable TextBox.
        window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        var editor=Descendants<TextBox>(hoursColumn.GetCellContent(row)).SingleOrDefault(t=>t.IsVisible);
        if(editor is null)throw new InvalidOperationException("Hibrit hücre düzenleme açılamadı.");
        editor.Text="108";window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(row.Result?.Gross!=23821.74m || model.Monthly.DayHours!="108")throw new InvalidOperationException("Hücre yazılırken hesap ve aylık panel güncellenmedi.");
        if(row.MissingDays!="15")throw new InvalidOperationException("Hücre yazılırken SGK bilgisi güncellenmedi.");
        grid.CommitEdit(DataGridEditingUnit.Cell,true);grid.CommitEdit(DataGridEditingUnit.Row,true);
        var beforeCreateNotifications=notifications.Count;
        Descendants<Button>(window).Single(b=>b.IsVisible && b.Content as string=="Hakediş oluştur").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if(!model.OutputsVisible || notifications.Count!=beforeCreateNotifications+1)throw new InvalidOperationException("Hakediş sonrası bildirim ve Çıktı yönlendirmesi yok.");
        Capture(window,Path.Combine(output,"Outputs-saved.png"));
        var outputsGrid=(DataGrid)window.FindName("OutputsGrid");
        if(outputsGrid.Columns.OfType<DataGridTextColumn>().Any(c=>c.ElementStyle?.Setters.OfType<Setter>().Any(v=>v.Property==TextBlock.TextAlignmentProperty && Equals(v.Value,TextAlignment.Center))!=true))throw new InvalidOperationException("Çıktı hücreleri ortalı değil.");
        model.Navigate("Calculation");window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(!model.Calculation.Status.Contains("oluşturuldu"))throw new InvalidOperationException("Hakediş oluştur düğmesi kalıcı kayıt yapmadı: "+model.Calculation.Status);
        var expectedHeaders=new[]{Hakedis.Contracts.ExcelHeadings.Sequence,Hakedis.Contracts.ExcelHeadings.FullName,
            Hakedis.Contracts.ExcelHeadings.IdentityNumber,Hakedis.Contracts.ExcelHeadings.Iban,Hakedis.Contracts.ExcelHeadings.StartDate,
            Hakedis.Contracts.ExcelHeadings.EndDate,
            Hakedis.Contracts.ExcelHeadings.Occupation,Hakedis.Contracts.ExcelHeadings.School,Hakedis.Contracts.ExcelHeadings.IbanCheck,
            "GÜNDÜZ SAAT TOPLAMI","ÖNCEKİ KURUM GÜNDÜZ","YENİ KURUM GÜNDÜZ","KURUM SAAT UYARISI",Hakedis.Contracts.ExcelHeadings.DykHours,Hakedis.Contracts.ExcelHeadings.Hours,
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
        var beforeRegistrationNotifications=notifications.Count;
        saveRegistration.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(!model.CalculationVisible || row.FullName!="Güncellenen Öğretmen" || notifications.Count!=beforeRegistrationNotifications+1)
            throw new InvalidOperationException("Öğretmen kaydı / dönüş / bildirim başarısız.");
        if(saveRegistration.Content as string!="Öğretmeni Kaydet" || cancelRegistration.Content as string!="Formu Temizle")
            throw new InvalidOperationException("Öğretmen düğmeleri normal moda dönmedi.");
        row.School="Yeni okul";row.DayHours="90";window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(!saveHybrid.IsEnabled)throw new InvalidOperationException("Değişiklikleri kaydet etkinleşmedi.");
        var beforeHybridNotifications=notifications.Count;
        saveHybrid.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(saveHybrid.IsEnabled || notifications.Count!=beforeHybridNotifications+1)throw new InvalidOperationException("Toplu kayıt değişiklikleri kabul etmedi.");
        grid.ScrollIntoView(row,grid.Columns[1]);window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        foreach(var text in Descendants<TextBlock>(grid).Where(t=>System.Windows.Data.BindingOperations.GetBinding(t,TextBlock.TextProperty)?.Path?.Path=="FullName"))
            if(text.TextAlignment!=TextAlignment.Center || text.VerticalAlignment!=VerticalAlignment.Center)
                throw new InvalidOperationException("Hibrit hücre ortalanmadı.");
        Capture(window,Path.Combine(output,"Calculation-edited.png"));
        // Yeni panel yerleşimi ve salt okunur ortak sabitler.
        model.Navigate("Teachers");window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);window.UpdateLayout();
        var listCard=(FrameworkElement)window.FindName("TeacherListCard");var detailsCard=(FrameworkElement)window.FindName("TeacherDetailsCard");
        if(detailsCard.TranslatePoint(new Point(0,0),window).X<=listCard.TranslatePoint(new Point(0,0),window).X || ((DataGrid)window.FindName("TeacherListGrid")).Columns.Count!=3)
            throw new InvalidOperationException("Öğretmen kartları yan yana / ad, TC, IBAN sütunları eksik.");
        model.Navigate("Constants");window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(Descendants<ComboBox>(window).Any(c=>c.IsVisible))throw new InvalidOperationException("Ortak sabit panelinde modül/dönem seçimi kalmış.");
        if(Descendants<TextBox>(window).Where(t=>t.IsVisible).Any(t=>!t.IsReadOnly))throw new InvalidOperationException("Sabitler başlangıçta salt okunur değil.");
        var update=(Button)window.FindName("ConstantsUpdateButton");update.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(model.CommonConstants.IsReadOnly || update.Content as string!="Dönem sabitlerini değiştir")throw new InvalidOperationException("Sabit güncelleme modu açılmadı.");
        update.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(!model.CommonConstants.IsReadOnly || update.Content as string!="Güncelle")throw new InvalidOperationException("Sabit kaydı kilitlemedi.");
        model.Registration.Reset();model.ClearCurrentWork();model.Navigate("Outputs");model.Outputs.Selected=model.Outputs.Records.Single();
        var archived=model.Outputs.ReadSelected();var archiveBytes=File.ReadAllBytes(archived.Path);
        window.Width=1366;window.Height=768;window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        var rowButtons=Descendants<Button>((DataGrid)window.FindName("OutputsGrid")).Where(b=>b.Content is string text && new[]{"PDF oluştur","Yazdır","Düzenle","Sil","Detay","XML oluştur"}.Contains(text)).ToArray();
        if(rowButtons.Length!=6 || rowButtons.Any(b=>Math.Abs(b.ActualWidth-100)>1 || Math.Abs(b.ActualHeight-32)>1))throw new InvalidOperationException("Çıktı düğmeleri eşit ölçüde değil.");
        Capture(window,Path.Combine(output,"Outputs-five-actions.png"));
        var sgkStore=new Hakedis.Storage.SgkXmlStore(model.Outputs.DataRoot);
        sgkStore.Save(Hakedis.Contracts.ModuleId.TemelEgitim,new(new("12345678901234567890120000","ÖRNEK KURUM","ÖRNEK ADRES","13","00000",DefaultMissingReason:"07"),[]));
        var xmlDialog=new SgkXmlWindow(archived,sgkStore,Path.Combine(output,"xml-test"),notifications.Add){Owner=window};
        Exception? xmlFailure=null;
        xmlDialog.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>{
            try{
                var button=(Button)xmlDialog.FindName("ExportXmlButton");button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if(xmlDialog.OutputPath is not null || string.IsNullOrWhiteSpace(((TextBlock)xmlDialog.FindName("ErrorText")).Text))throw new InvalidOperationException("SGK inceleme onayı atlandı.");
                Capture(xmlDialog,Path.Combine(output,"SGK-review-required.png"));
                ((CheckBox)xmlDialog.FindName("ReviewCheck")).IsChecked=true;
                var nature=(ComboBox)xmlDialog.FindName("NatureField");
                nature.SelectedValue="İ";
                if(nature.SelectedItem is not SgkNatureOption {Code:"İ",Label:"İ — İptal"})throw new InvalidOperationException("İptal mahiyeti seçilemedi.");
                Capture(xmlDialog,Path.Combine(output,"SGK-cancellation.png"));
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if(xmlDialog.OutputPath is null)throw new InvalidOperationException("SGK XML oluşturulmadı: "+((TextBlock)xmlDialog.FindName("ErrorText")).Text);
            }catch(Exception ex){xmlFailure=ex;xmlDialog.Close();}
        }));
        if(xmlDialog.ShowDialog()!=true || xmlFailure is not null)throw new InvalidOperationException("SGK XML ekranı başarısız.",xmlFailure);
        Hakedis.TemelEgitim.Core.SgkXmlBuilder.Validate(File.ReadAllBytes(xmlDialog.OutputPath!));
        if(System.Xml.Linq.XDocument.Load(xmlDialog.OutputPath!).Root!.Element("BORDRO")!.Attribute("BELGEMAHIYET")!.Value!="İ")throw new InvalidOperationException("İptal seçimi XML'e Türkçe İ olarak yazılmadı.");
        if(!archiveBytes.SequenceEqual(File.ReadAllBytes(archived.Path)))throw new InvalidOperationException("SGK çıktısı hakedişi değiştirdi.");
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
        var many=Enumerable.Range(1,10).Select(i=>baseRow with {Teacher=baseRow.Teacher with {Id=Guid.NewGuid(),IdentityNumber=$"100000{i:D5}",FullName=$"ÇĞİÖŞÜ Deneme Öğretmeni {i} Uzun İsim Soyisim"}})
            .Select(r=>r with {Input=r.Input with {TeacherId=r.Teacher.Id}}).ToArray();
        var large=archived with {Document=archived.Document with {Data=archived.Document.Data with {Rows=many,Movement=new(0,many.Length,0,many.Select(r=>r.Teacher.Id).ToArray())}}};
        var largeExports=service.Export(large,details with {Institution="Çok uzun kurum adı olan Örnek İlçe Milli Eğitim Müdürlüğü - Okul Öncesi ve İlköğretim Okulları"},Path.Combine(output,"pdf-large-test"));
        File.WriteAllText(Path.Combine(output,"pdf-large-folder.txt"),largeExports);
        VerifyReportPages(service,large,details,output,"large");
        var oversized=large with {Document=large.Document with {Data=large.Document.Data with {Rows=Enumerable.Range(1,65).Select(i=>baseRow with {Teacher=baseRow.Teacher with {Id=Guid.NewGuid(),IdentityNumber=$"100000{i:D5}",FullName=$"Çok Uzun Ad Soyad Deneme Öğretmeni {i}"}}).Select(r=>r with {Input=r.Input with {TeacherId=r.Teacher.Id}}).ToArray()}}};
        try{service.Prepare(oversized,details);throw new Exception("Okunamayacak bordro için kapasite kontrolü yok.");}
        catch(ArgumentException ex) when(ex.Message.Contains("tek yatay A4")){}
        var legacy=archived with {Document=archived.Document with {Data=archived.Document.Data with {Movement=new(0,99,99)}}};
        var corrected=service.Prepare(legacy,details)[2];
        if(corrected.Pages[0].Cells.Where(c=>c.Y==178.4).SelectMany(c=>c.Lines).Contains("99"))throw new InvalidOperationException("Eski personel hesabı düzeltilmedi.");
        PersonalPayrollChecks.Run(model,window,output,Capture,openedFolders);
        model.Navigate("Calculation");model.Calculation.Reload();window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        var selectors=Descendants<ComboBox>(window).Where(c=>c.IsVisible).ToArray();
        selectors.Single(c=>c.Items.Count==201).SelectedItem=2027;
        selectors.Single(c=>c.Items.Count==12).SelectedValue=11;
        window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(model.Calculation.PeriodText!="Kasım-2027" || model.Calculation.Rows[0].HasInput)throw new InvalidOperationException("Dönem seçiminde kayıtlar karıştı.");
        Capture(window,Path.Combine(output,"Calculation-Kasim-2027.png"));
        model.ClearCurrentWork();model.Navigate("Outputs");model.Outputs.Selected=model.Outputs.Records.Single();
        model.BeginArchiveEdit();window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        model.Calculation.Rows[0].DayHours="80";archiveConfirm=false;
        ((Button)window.FindName("SaveArchiveButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if(!model.Calculation.IsArchiveEditing || !archiveBytes.SequenceEqual(File.ReadAllBytes(archived.Path)))throw new InvalidOperationException("Onaysız düzenleme eski kaydı değiştirdi.");
        archiveConfirm=true;((Button)window.FindName("SaveArchiveButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(!model.OutputsVisible || model.Outputs.Records.Count!=1 || model.Outputs.Records[0].Path!=archived.Path || archiveBytes.SequenceEqual(File.ReadAllBytes(archived.Path)))throw new InvalidOperationException("Onaylı düzenleme tek kaydın yerine yazmadı.");
        Capture(window,Path.Combine(output,"Outputs-replaced-single-record.png"));
        model.Outputs.Selected=model.Outputs.Records.Single();var updatedBytes=File.ReadAllBytes(archived.Path);
        var deleteArchive=Descendants<Button>((DataGrid)window.FindName("OutputsGrid")).Single(b=>b.Content as string=="Sil");
        archiveConfirm=false;deleteArchive.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if(model.Outputs.Records.Count!=1 || !updatedBytes.SequenceEqual(File.ReadAllBytes(archived.Path)))throw new InvalidOperationException("Sil Hayır kaydı değiştirdi.");
        archiveConfirm=true;deleteArchive.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(model.Outputs.Records.Count!=0 || File.Exists(archived.Path))throw new InvalidOperationException("Sil Evet kaydı kaldırmadı.");
        if(archivePrompts.Count!=4 || !archivePrompts[0].Contains("yalnızca bir hakediş"))throw new InvalidOperationException("Onay pencereleri eksik.");
        Capture(window,Path.Combine(output,"Outputs-after-confirmed-delete.png"));

        foreach(var pendingKind in new[]{"hours","teacher","constants","teacher-list","details"})
        {
            var pending=new ShellViewModel(Path.Combine(output,Guid.NewGuid().ToString("N"),"ogretmenler.json"));
            pending.Registration.Editor.FullName="Kaydedilmemiş Öğretmen";
            if(pendingKind=="hours"){pending.SaveNewTeacher();pending.Monthly.DayHours="45";}
            if(pendingKind=="constants") {pending.Registration.Reset();pending.CommonConstants.ToggleSave();pending.CommonConstants.Editor.Coefficient="2";}
            if(pendingKind=="teacher-list"){pending.SaveNewTeacher();pending.Teachers.Selected=pending.Teachers.Records.Single();pending.Teachers.Editor.FullName="Değişen öğretmen";}
            if(pendingKind=="details"){pending.Registration.Reset();pending.Outputs.Details.Institution="Kaydedilmemiş kurum";}
            var allowClose=false;var closeAsked=0;
            var pendingWindow=new MainWindow(pending,notifications.Add,(_,_)=>{closeAsked++;return allowClose;}){ShowActivated=false,ShowInTaskbar=false};
            pendingWindow.Show();pendingWindow.Close();
            if(!pendingWindow.IsVisible || closeAsked!=1)throw new InvalidOperationException("Kaydedilmemiş "+pendingKind+" bilgileri için kapanış Hayır çalışmadı.");
            allowClose=true;pendingWindow.Close();
            if(pendingWindow.IsVisible || closeAsked!=2)throw new InvalidOperationException("Kapanış onayı çalışmadı.");
        }
        window.Close();
        DinOgretimiChecks.Run(output,Capture);
        HalkEgitimChecks.Run(output,Capture);
        RetiredHalkChecks.Run(output,Capture);
        MeslekiEgitimChecks.Run(output,Capture);
        LayoutSettingsChecks.Run(output,Capture);
        SaveNotificationChecks.Run(output);
        SchoolHistoryChecks.Run(output,Capture);
        TransferHoursChecks.Run(output,Capture);
        OutputEntryChecks.Run(output,Capture);
        if (listener.Errors.Count > 0) { foreach (var message in listener.Errors) Console.Error.WriteLine(message); return 1; }
        Console.WriteLine($"12 WPF panel görüntüsü, eşit altı çıktı düğmesi, onaylı silme ve tek kayıtla düzenleme, SGK XML ekranı/onayı/şema testi, ortak sabit kilidi, yan yana öğretmen kartları, arşiv iptali, üç PDF ve çok sayfalı yazıcı işi doğrulandı; binding hatası yok. {output}"); return 0;
    }
    private static void VerifyReportPages(PaymentReportService service,Hakedis.Storage.ArchivedPayment record,Hakedis.Contracts.ReportDetails details,string output,string prefix)
    {
        var prepared=service.Prepare(record,details);
        if(prepared[0].Pages.Length!=1 || prepared[0].Pages[0].Width<prepared[0].Pages[0].Height)throw new InvalidOperationException("Bordro tek yatay A4 değil.");
        if(prepared.Any(r=>r.FileName.Contains("Çıktı-")))throw new InvalidOperationException("Sıra etiketi kaldı.");
        if(prepared[2].Pages.Length!=1)throw new InvalidOperationException("Personel bildirimi tek sayfa değil.");
        var digits=prepared[2].Pages[0].Cells.Where(c=>c.Y==141 && c.Width==49).ToArray();
        if(digits.Length!=4 || string.Concat(digits.Select(c=>c.Lines[0]))!=record.Document.Data.Year.ToString())throw new InvalidOperationException("Personel yılı ayrı hücrelerde değil.");
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
