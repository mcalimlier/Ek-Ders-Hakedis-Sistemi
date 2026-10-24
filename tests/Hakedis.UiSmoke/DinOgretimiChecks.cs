using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Hakedis.App;
using Hakedis.Contracts;
using Hakedis.Storage;

internal static class DinOgretimiChecks
{
    internal static void Run(string output,Action<FrameworkElement,string> capture)
    {
        var root=Path.Combine(output,"din-data");var model=new ShellViewModel(Path.Combine(root,"ogretmenler.json"));
        var notifications=new List<string>();var opened=new List<string>();
        var window=new MainWindow(model,notifications.Add,(_,_)=>true,opened.Add,Path.Combine(output,"din-personal")){ShowActivated=false,ShowInTaskbar=false};
        window.Show();
        var selector=Descendants<ComboBox>(window).Single(c=>c.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedItemProperty)?.ParentBinding.Path.Path=="RegistrationModule");
        selector.SelectedItem=model.Workspace.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);
        window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(model.RegistrationModule.Id!=ModuleId.DinOgretimi || !model.RegistrationModule.IsAvailable)throw new Exception("Din Öğretimi seçilemiyor.");
        model.Registration.Editor.FullName="Din Deneme Öğretmeni";model.Registration.Editor.IdentityNumber="10000000200";
        model.Registration.Editor.Iban="TR330006100519786457841326";model.Registration.Editor.OccupationCode="2341.04";
        model.Registration.Editor.StartDate=new(2026,9,14);model.SaveNewTeacher();
        model.Monthly.SelectedModule=model.Monthly.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);
        model.Monthly.SelectedTeacher=model.Monthly.Teachers.Single();model.Monthly.DayHours="117";model.Monthly.Save(false);
        model.Calculation.SelectedModule=model.Calculation.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);
        model.Outputs.Details.Institution="Örnek Din Öğretimi Kurumu";model.Outputs.Details.Bank="Örnek Banka";
        model.Outputs.Details.PreparedBy="Deneme Görevli";model.Outputs.Details.AuthorizedBy="Deneme Yetkili";model.Outputs.SaveDetails();
        model.Navigate("Calculation");window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        capture(window,Path.Combine(output,"Din-Calculation-1366.png"));
        var create=Descendants<Button>(window).Single(b=>b.IsVisible && b.Content is string text && text=="Hakediş oluştur");
        create.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(!model.OutputsVisible || notifications.Count==0 || model.Outputs.Records.Count!=1)throw new Exception("Din hakediş oluştur/onay/çıktı akışı başarısız.");
        var record=model.Outputs.Records.Single();var bytes=File.ReadAllBytes(record.Path);var svc=new PaymentReportService();
        var prepared=svc.Prepare(record,model.Outputs.Details.Read());
        if(prepared.Length!=3 || prepared[0].Pages.Length!=1 || prepared[0].Pages[0].Width<prepared[0].Pages[0].Height)throw new Exception("Din üç belge/tek A4 bordro koşulu bozuk.");
        var pdfFolder=svc.Export(record,model.Outputs.Details.Read(),Path.Combine(output,"din-pdfs"));
        model.Navigate("Personal");model.Personal.Year=2026;model.Personal.Month=9;
        model.Personal.SelectedModule=model.Personal.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);
        var grid=(DataGrid)window.FindName("PersonalTeachersGrid");grid.SelectedItem=model.Personal.Teachers.Single();
        window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        ((Button)window.FindName("CreatePersonalButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if(!File.Exists(model.Personal.LastOutputPath))throw new Exception("Din kişisel PDF oluşturulamadı.");
        ((Button)window.FindName("PersonalFolderButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if(opened.Single()!=model.Personal.OutputFolder)throw new Exception("Din klasör düğmesi çalışmıyor.");
        capture(window,Path.Combine(output,"Din-Personal-1366.png"));
        var workplace=new SgkWorkplace("12345678901234567890120000","TEST KURUM","TEST ADRES","13","00000","İ","07");
        var editor=new SgkXmlEditor(record,new(workplace,[])){Confirmed=true};var xml=editor.Build();
        Hakedis.TemelEgitim.Core.SgkXmlBuilder.Validate(xml);File.WriteAllBytes(Path.Combine(output,"din-test.xml"),xml);
        if(!bytes.SequenceEqual(File.ReadAllBytes(record.Path)))throw new Exception("Din çıktısı kaynak hakedişi değiştirdi.");
        File.WriteAllLines(Path.Combine(output,"din-output-paths.txt"),[pdfFolder,model.Personal.LastOutputPath]);
        model.ClearCurrentWork();window.Close();
        Console.WriteLine("Din Öğretimi WPF: modül seçimi, aylık giriş, canlı tablo, hakediş bildirimi, üç PDF, kişisel PDF/klasör ve İ mahiyetli XML/XSD doğrulandı.");
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T:DependencyObject
    {for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is T item)yield return item;foreach(var next in Descendants<T>(child))yield return next;}}
}
