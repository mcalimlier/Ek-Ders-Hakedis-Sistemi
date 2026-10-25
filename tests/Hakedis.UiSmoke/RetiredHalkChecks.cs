using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Hakedis.App;
using Hakedis.Contracts;
using Hakedis.Storage;

internal static class RetiredHalkChecks
{
    internal static void Run(string output,Action<FrameworkElement,string> capture)
    {
        const ModuleId module=ModuleId.HalkEgitimEmekliUstaOgretici;
        var root=Path.Combine(output,"retired-data");var model=new ShellViewModel(Path.Combine(root,"ogretmenler.json"));
        var notifications=new List<string>();var opened=new List<string>();
        var window=new MainWindow(model,notifications.Add,(_,_)=>true,opened.Add,Path.Combine(output,"retired-personal")){ShowActivated=false,ShowInTaskbar=false};
        window.Show();
        var selector=Descendants<ComboBox>(window).Single(c=>c.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedItemProperty)?.ParentBinding.Path.Path=="RegistrationModule");
        selector.SelectedItem=model.Workspace.Modules.Single(m=>m.Id==module);Idle(window);
        if(!model.RegistrationModule.IsAvailable)throw new Exception("Usta öğretici modülü kapalı.");
        var e=model.Registration.Editor;e.FullName="Deneme Emekli Usta";e.IdentityNumber="10000000146";
        e.Iban="TR330006100519786457841326";e.OccupationCode="2360.43";e.School="Örnek Halk Eğitim Merkezi";e.StartDate=new(2026,9,1);model.SaveNewTeacher();
        model.Monthly.SelectedModule=model.Monthly.Modules.Single(m=>m.Id==module);model.Monthly.Month=9;
        model.Monthly.SelectedTeacher=model.Monthly.Teachers.Single();model.Monthly.DayHours="12";model.Monthly.Save(false);
        model.Calculation.SelectedModule=model.Calculation.Modules.Single(m=>m.Id==module);model.Calculation.Month=9;
        model.Outputs.Details.Institution="Örnek Halk Eğitim Merkezi";model.Outputs.Details.Bank="Örnek Banka";
        model.Outputs.Details.PreparedBy="Deneme Görevli";model.Outputs.Details.AuthorizedBy="Deneme Yetkili";model.Outputs.SaveDetails();
        model.Navigate("Calculation");Idle(window);
        if(!Descendants<TextBlock>(window).Any(t=>t.IsVisible && t.Text.EndsWith("( EMEKLİ USTA ÖĞRETİCİ)")))throw new Exception("Usta öğretici bordro başlığı gösterilmiyor.");
        capture(window,Path.Combine(output,"Retired-Calculation-1366.png"));
        var create=Descendants<Button>(window).Single(b=>b.IsVisible && b.Content as string=="Hakediş oluştur");
        create.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Idle(window);
        var record=model.Outputs.Records.Single();var row=record.Document.Data.Rows.Single();
        if(!model.OutputsVisible || notifications.Count==0 || record.Document.ModuleId!=module || row.Result.Net!=2448.35m || row.SgkInformation?.MissingDays!=28)
            throw new Exception("Usta öğretici oluştur/onay/çıktı veya dönem günleri hatalı.");
        var bytes=File.ReadAllBytes(record.Path);var service=new PaymentReportService();var reports=service.Prepare(record,model.Outputs.Details.Read());
        if(reports.Length!=3 || reports[0].Pages.Length!=1 || reports[0].Pages[0].Width<reports[0].Pages[0].Height ||
            !reports[0].Pages[0].Cells.SelectMany(c=>c.Lines).Any(t=>t.Contains("EMEKLİ USTA ÖĞRETİCİ")))throw new Exception("Usta öğretici PDF başlığı/tek A4 yanlış.");
        var folder=service.Export(record,model.Outputs.Details.Read(),Path.Combine(output,"retired-pdfs"));
        model.Navigate("Personal");model.Personal.Month=9;model.Personal.SelectedModule=model.Personal.Modules.Single(m=>m.Id==module);
        ((DataGrid)window.FindName("PersonalTeachersGrid")).SelectedItem=model.Personal.Teachers.Single();Idle(window);
        ((Button)window.FindName("CreatePersonalButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        ((Button)window.FindName("PersonalFolderButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if(!File.Exists(model.Personal.LastOutputPath) || opened.Single()!=model.Personal.OutputFolder)throw new Exception("Usta kişisel bordro/klasör akışı hatalı.");
        var store=new SgkXmlStore(root);var workplace=new SgkWorkplace("12345678901234567890197000","TEST KURUM","TEST ADRES","13","00000","A","07");
        store.Save(ModuleId.HalkEgitimUstaOgretici,new(workplace,[]));if(store.Read(ModuleId.TemelEgitim).Workplace.RegistrationNumber!="")throw new Exception("Usta profili öğretmen SGK profilini değiştirdi.");
        var settings=store.Read(module);if(settings.Workplace.DocumentType!="02")throw new Exception("Emekli belge türü 02 değil.");
        var editor=new SgkXmlEditor(record,settings){Confirmed=true};var xml=editor.Build();Hakedis.TemelEgitim.Core.SgkXmlBuilder.Validate(xml);
        File.WriteAllBytes(Path.Combine(output,"retired-test.xml"),xml);File.WriteAllLines(Path.Combine(output,"retired-output-paths.txt"),[folder,model.Personal.LastOutputPath]);
        if(!bytes.SequenceEqual(File.ReadAllBytes(record.Path)))throw new Exception("Usta çıktı akışı arşivi değiştirdi.");
        model.Navigate("Constants");Idle(window);
        var fields=(UniformGrid)window.FindName("RetiredConstantsFields");fields.BringIntoView();Idle(window);
        var boxes=Descendants<TextBox>(fields).ToArray();if(boxes.Length!=9 || boxes.Any(b=>!b.IsReadOnly))throw new Exception("Emekli sabit alanları kilitli değil.");
        var update=(Button)window.FindName("RetiredConstantsUpdateButton");update.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Idle(window);
        if(boxes.Any(b=>b.IsReadOnly))throw new Exception("Emekli sabit güncelleme açılmadı.");
        boxes.Single(b=>b.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path.Path=="EmployeeRate").Text="0,08";Idle(window);
        update.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Idle(window);
        if(boxes.Any(b=>!b.IsReadOnly) || model.CommonConstants.Editor.EmployeeRate!="0,14" || model.Calculation.Constants.Read().EmployeeRate!=.08m)
            throw new Exception("Emekli sabit kaydı/kilit/ayrım hatalı.");
        if(!bytes.SequenceEqual(File.ReadAllBytes(record.Path)))throw new Exception("Emekli sabit düzenlemesi eski bordroyu değiştirdi.");
        capture(window,Path.Combine(output,"Retired-Constants-1366.png"));
        model.ClearCurrentWork();window.Close();
        Console.WriteLine("Emekli Halk Eğitim WPF: Eylül 2026, 12 saat, 2.448,35 TL net, 2 prim/28 eksik gün, ayrı kilitli sabit kartı, eski arşiv koruması, üç PDF/kişisel bordro ve SGK belge 02 doğrulandı.");
    }
    private static void Idle(Window window){window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);window.UpdateLayout();}
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T:DependencyObject
    {for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is T item)yield return item;foreach(var next in Descendants<T>(child))yield return next;}}
}
