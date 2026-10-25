using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Hakedis.App;
using Hakedis.Contracts;
using Hakedis.Storage;

internal static class HalkEgitimChecks
{
    internal static void Run(string output,Action<FrameworkElement,string> capture)
    {
        const ModuleId module=ModuleId.HalkEgitimUstaOgretici;
        var root=Path.Combine(output,"halk-data");var model=new ShellViewModel(Path.Combine(root,"ogretmenler.json"));
        var notifications=new List<string>();var opened=new List<string>();
        var window=new MainWindow(model,notifications.Add,(_,_)=>true,opened.Add,Path.Combine(output,"halk-personal")){ShowActivated=false,ShowInTaskbar=false};
        window.Show();
        var selector=Descendants<ComboBox>(window).Single(c=>c.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedItemProperty)?.ParentBinding.Path.Path=="RegistrationModule");
        selector.SelectedItem=model.Workspace.Modules.Single(m=>m.Id==module);Idle(window);
        if(!model.RegistrationModule.IsAvailable)throw new Exception("Usta öğretici modülü kapalı.");
        var e=model.Registration.Editor;e.FullName="Deneme Usta Öğretici";e.IdentityNumber="10000000146";
        e.Iban="TR330006100519786457841326";e.OccupationCode="2360.43";e.School="Örnek Halk Eğitim Merkezi";e.StartDate=new(2026,9,1);model.SaveNewTeacher();
        model.Monthly.SelectedModule=model.Monthly.Modules.Single(m=>m.Id==module);model.Monthly.Month=10;
        model.Monthly.SelectedTeacher=model.Monthly.Teachers.Single();model.Monthly.DayHours="60";model.Monthly.Save(false);
        model.Calculation.SelectedModule=model.Calculation.Modules.Single(m=>m.Id==module);model.Calculation.Month=10;
        model.Outputs.Details.Institution="Örnek Halk Eğitim Merkezi";model.Outputs.Details.Bank="Örnek Banka";
        model.Outputs.Details.PreparedBy="Deneme Görevli";model.Outputs.Details.AuthorizedBy="Deneme Yetkili";model.Outputs.SaveDetails();
        model.Navigate("Calculation");Idle(window);
        if(!Descendants<TextBlock>(window).Any(t=>t.IsVisible && t.Text.EndsWith("(USTA ÖĞRETİCİ)")))throw new Exception("Usta öğretici bordro başlığı gösterilmiyor.");
        capture(window,Path.Combine(output,"Halk-Calculation-1366.png"));
        var create=Descendants<Button>(window).Single(b=>b.IsVisible && b.Content as string=="Hakediş oluştur");
        create.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Idle(window);
        var record=model.Outputs.Records.Single();var row=record.Document.Data.Rows.Single();
        if(!model.OutputsVisible || notifications.Count==0 || record.Document.ModuleId!=module || row.Result.Net!=11381.50m || row.SgkInformation?.MissingDays!=23)
            throw new Exception("Usta öğretici oluştur/onay/çıktı veya dönem günleri hatalı.");
        var bytes=File.ReadAllBytes(record.Path);var service=new PaymentReportService();var reports=service.Prepare(record,model.Outputs.Details.Read());
        if(reports.Length!=3 || reports[0].Pages.Length!=1 || reports[0].Pages[0].Width<reports[0].Pages[0].Height ||
            !reports[0].Pages[0].Cells.SelectMany(c=>c.Lines).Any(t=>t.Contains("USTA ÖĞRETİCİ")))throw new Exception("Usta öğretici PDF başlığı/tek A4 yanlış.");
        var folder=service.Export(record,model.Outputs.Details.Read(),Path.Combine(output,"halk-pdfs"));
        model.Navigate("Personal");model.Personal.Month=10;model.Personal.SelectedModule=model.Personal.Modules.Single(m=>m.Id==module);
        ((DataGrid)window.FindName("PersonalTeachersGrid")).SelectedItem=model.Personal.Teachers.Single();Idle(window);
        ((Button)window.FindName("CreatePersonalButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        ((Button)window.FindName("PersonalFolderButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if(!File.Exists(model.Personal.LastOutputPath) || opened.Single()!=model.Personal.OutputFolder)throw new Exception("Usta kişisel bordro/klasör akışı hatalı.");
        var store=new SgkXmlStore(root);var workplace=new SgkWorkplace("12345678901234567890197000","TEST KURUM","TEST ADRES","13","00000","A","07");
        store.Save(module,new(workplace,[]));if(store.Read(ModuleId.TemelEgitim).Workplace.RegistrationNumber!="")throw new Exception("Usta profili öğretmen SGK profilini değiştirdi.");
        var editor=new SgkXmlEditor(record,store.Read(module)){Confirmed=true};var xml=editor.Build();Hakedis.TemelEgitim.Core.SgkXmlBuilder.Validate(xml);
        File.WriteAllBytes(Path.Combine(output,"halk-test.xml"),xml);File.WriteAllLines(Path.Combine(output,"halk-output-paths.txt"),[folder,model.Personal.LastOutputPath]);
        if(!bytes.SequenceEqual(File.ReadAllBytes(record.Path)))throw new Exception("Usta çıktı akışı arşivi değiştirdi.");
        model.ClearCurrentWork();window.Close();
        Console.WriteLine("Halk Eğitim WPF: kayıt, Ekim 2026, 60 saat, 11.381,50 TL net, 8 prim/23 eksik gün, usta başlığı, üç PDF/kişisel bordro ve ayrı SGK XML profili doğrulandı.");
    }
    private static void Idle(Window window){window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);window.UpdateLayout();}
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T:DependencyObject
    {for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is T item)yield return item;foreach(var next in Descendants<T>(child))yield return next;}}
}
