using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Hakedis.App;
using Hakedis.Contracts;
using Hakedis.Storage;

internal static class SchoolHistoryChecks
{
    internal static void Run(string output,Action<FrameworkElement,string> capture)
    {
        var root=Path.Combine(output,"history-data");var path=Path.Combine(root,"ogretmenler.json");var model=new ShellViewModel(path);
        var e=model.Registration.Editor;e.FullName="Geçiş Deneme";e.IdentityNumber="10000000146";e.Iban="TR330006100519786457841326";
        e.School="İlk Okul";e.OccupationCode="2341.04";e.StartDate=new(2026,9,1);
        var accept=false;var asked=0;var notifications=new List<string>();
        var targetModule=ModuleId.DinOgretimi;
        var window=new MainWindow(model,notifications.Add,(_,_)=>true,billingChoicePrompt:vm=>
        {asked++;if(!accept)return false;vm.SelectedModule=vm.Modules.Single(m=>m.Id==targetModule);return true;}){ShowActivated=false,ShowInTaskbar=false};
        window.Show();Idle(window);
        var form=Descendants<TeacherForm>(window).Single(f=>ReferenceEquals(f.DataContext,model.Registration));
        var fields=(Border)form.FindName("SchoolTransferFields");var check=(CheckBox)form.FindName("SchoolTransferCheck");
        if(fields.Visibility!=Visibility.Collapsed || check.IsChecked==true)throw new Exception("Normal öğretmende özel alanlar açık.");
        check.IsChecked=true;Idle(window);if(fields.Visibility!=Visibility.Visible)throw new Exception("Geçiş onayı alanları açmadı.");
        ((TextBox)form.FindName("TransferSchoolText")).Text="Yeni Okul";
        ((DatePicker)form.FindName("TransferDatePicker")).SelectedDate=new DateTime(2026,9,25);
        ((ComboBox)form.FindName("TransferModuleCombo")).SelectedValue=ModuleId.DinOgretimi;Idle(window);
        fields.BringIntoView();Idle(window);capture(window,Path.Combine(output,"History-optional-fields.png"));
        model.SaveNewTeacher();var canonical=new TeacherDirectory(path).ReadCanonical().Single();
        if(canonical.Assignments?.Transfers.Length!=1 || new TeacherDirectory(path,ModuleId.DinOgretimi).Read().Single().Id!=canonical.Id)
            throw new Exception("Tek kişi kartı veya görev geçmişi kaydedilmedi.");
        model.Monthly.DayHours="117";model.Navigate("Calculation");Idle(window);
        var create=Descendants<Button>(window).Single(b=>b.IsVisible && b.Content as string=="Hakediş oluştur");
        create.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Idle(window);
        if(asked!=1 || model.Outputs.Records.Count!=0 || new TeacherDirectory(path).ReadCanonical().Single().Assignments!.Choices.Length!=0)
            throw new Exception("Vazgeç ödeme tercihi veya hakediş kaydetti.");
        var current=model.Calculation.Rows.Single();var decision=new BillingChoiceViewModel(current.Teacher,2026,9,current.Read(),root);
        var dialog=new BillingChoiceWindow(decision){Owner=window,ShowActivated=false,ShowInTaskbar=false};dialog.Show();Idle(dialog);
        var confirm=(Button)dialog.FindName("ConfirmBillingChoiceButton");if(confirm.IsEnabled)throw new Exception("Modül seçmeden onay açık.");
        ((ComboBox)dialog.FindName("BillingModuleCombo")).SelectedItem=decision.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);Idle(dialog);
        if(!confirm.IsEnabled)throw new Exception("Ödeme modülü seçimi onayı açmadı.");
        capture(dialog,Path.Combine(output,"History-billing-choice.png"));dialog.Close();
        model.Outputs.Details.Institution="Örnek Kurum";model.Outputs.Details.Bank="Örnek Banka";model.Outputs.Details.PreparedBy="Örnek Görevli";model.Outputs.Details.AuthorizedBy="Örnek Yetkili";model.Outputs.SaveDetails();
        accept=true;create.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Idle(window);
        var record=model.Outputs.Records.Single();var row=record.Document.Data.Rows.Single();
        if(asked!=2 || !model.OutputsVisible || record.Document.ModuleId!=ModuleId.DinOgretimi || row.Input.DayHours!=117 || row.SgkInformation?.CalendarDays!=30 || row.SgkError!="")
            throw new Exception("Tek modül/ay toplamı veya SGK işe giriş tarihi bozuldu.");
        if(new TeacherDirectory(path).ReadForPeriod(2026,9).Count!=0 || new TeacherDirectory(path,ModuleId.DinOgretimi).ReadForPeriod(2026,9).Count!=1)
            throw new Exception("Ödeme tercihi diğer modülü dışlamadı.");
        var workplace=new SgkWorkplace("12345678901234567890120000","TEST KURUM","TEST ADRES","13","00000","A","07");
        var xml=new SgkXmlEditor(record,new(workplace,[])){Confirmed=true}.Build();Hakedis.TemelEgitim.Core.SgkXmlBuilder.Validate(xml);
        File.WriteAllBytes(Path.Combine(output,"history-test.xml"),xml);
        var service=new PaymentReportService();var folder=service.Export(record,model.Outputs.Details.Read(),Path.Combine(output,"history-pdfs"));
        var personal=service.ExportPersonal(record,row.Teacher.Id,model.Outputs.Details.Read(),Path.Combine(output,"history-personal"));
        File.WriteAllLines(Path.Combine(output,"history-pdf-paths.txt"),[folder,personal]);
        var bytes=File.ReadAllBytes(record.Path);var master=new TeacherDirectory(path).Read().Single();
        new TeacherDirectory(path).UpdateBatch([master],[master with {FullName="Sonradan Düzenlenen Ad"}]);
        if(!bytes.SequenceEqual(File.ReadAllBytes(record.Path)))throw new Exception("Görev kartı değişikliği eski bordroyu değiştirdi.");
        model.Navigate("Calculation");model.Calculation.Reload();Idle(window);
        var preference=window.FindName("ChangeBillingChoiceButton") as Button??throw new Exception("Kaydedilmiş ödeme tercihini değiştirme düğmesi yok.");
        if(preference.Visibility!=Visibility.Collapsed)throw new Exception("Satır seçilmeden özel tercih düğmesi açık.");
        model.Calculation.SelectedHybridRow=model.Calculation.Rows.Single();Idle(window);
        if(!preference.IsVisible || !preference.IsEnabled)throw new Exception("Geçiş öğretmeni seçildiğinde tercih düğmesi açılmadı.");
        var limited=new BillingChoiceViewModel(model.Calculation.Rows.Single().Teacher,2026,9,row.Input,root);
        if(limited.Modules.Count!=1 || limited.Modules.Single().Id!=ModuleId.DinOgretimi)throw new Exception("Kayıtlı bordroya rağmen diğer ödeme modülü açık.");
        var archives=new PaymentArchiveStore(root);
        if(archives.Delete(record,false) || !File.Exists(record.Path))throw new Exception("Onaysız bordro silindi.");
        if(!archives.Delete(record,true))throw new Exception("Onaylı bordro silinmedi.");
        targetModule=ModuleId.TemelEgitim;
        preference.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Idle(window);
        if(asked!=3 || model.Calculation.SelectedModule.Id!=ModuleId.TemelEgitim || model.Calculation.Rows.Single().Read().DayHours!=117)
            throw new Exception("Silinen bordro sonrası tercih ve toplam saatler diğer modüle geçmedi.");
        capture(window,Path.Combine(output,"History-change-choice.png"));
        create.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Idle(window);
        var replaced=new PaymentArchiveStore(root).ReadAll().Single();
        if(replaced.Document.ModuleId!=ModuleId.TemelEgitim || replaced.Document.Data.Rows.Single().Input.DayHours!=117)
            throw new Exception("Tercih değişikliği sonrası tek bordro oluşturulmadı.");
        model.ClearCurrentWork();window.Close();
        Console.WriteLine("Görev geçmişi WPF: onayla açılan alanlar, tek kişi, tercih iptali/onayı, tek modülde 117 saat, bölünmeyen SGK tarihleri, PDF ve XML doğrulandı.");
    }
    private static void Idle(Window window){window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);window.UpdateLayout();}
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T:DependencyObject
    {for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is T item)yield return item;foreach(var next in Descendants<T>(child))yield return next;}}
}
