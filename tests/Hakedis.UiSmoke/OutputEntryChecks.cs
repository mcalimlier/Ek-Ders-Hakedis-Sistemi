using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Hakedis.App;
using Hakedis.Contracts;
using Hakedis.Storage;
internal static class OutputEntryChecks
{
    internal static void Run(string output,Action<FrameworkElement,string> capture)
    {
        var root=Path.Combine(output,"output-entry-data");var path=Path.Combine(root,"ogretmenler.json");var model=new ShellViewModel(path);var notices=new List<string>();
        model.Registration.Editor.FullName="IBAN Bekleyen Öğretmen";model.Registration.Editor.OccupationCode="2341.04";model.SaveNewTeacher();
        var window=new MainWindow(model,notices.Add,(_,_)=>true){ShowActivated=false,ShowInTaskbar=false};window.Show();Idle(window);
        var module=(ComboBox)window.FindName("EntryModuleCombo");
        if(module.SelectedItem is not null || model.Monthly.EntryTeacher is not null)throw new Exception("Aylık giriş seçimsiz açılmadı.");
        var hours=Descendants<TextBox>(window).Single(t=>t.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path?.Path=="EntryDayHours");
        if(hours.Text!="" || hours.IsEnabled)throw new Exception("Seçimden önce aylık saat alanı boş/kapalı değil.");
        capture(window,Path.Combine(output,"Entry-empty-selection.png"));
        module.SelectedItem=model.Monthly.Modules.Single(m=>m.Id==ModuleId.TemelEgitim);Idle(window);
        var teacherCombo=Descendants<ComboBox>(window).Single(t=>t.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedItemProperty)?.ParentBinding.Path?.Path=="EntryTeacher");
        teacherCombo.SelectedItem=model.Monthly.EntryTeachers.Single();Idle(window);
        hours.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left){RoutedEvent=UIElement.PreviewMouseLeftButtonDownEvent});
        if(hours.Text!="")throw new Exception("Sıfır tıklanınca temizlenmedi.");
        hours.Text="25";Idle(window);if(model.Calculation.Rows.Single().Read().DayHours!=25)throw new Exception("25 saat canlı önizlemeye yansımadı.");
        model.Navigate("Calculation");Idle(window);
        var create=Descendants<Button>(window).Single(b=>b.IsVisible && b.Content as string=="Hakediş oluştur");create.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Idle(window);
        if(!notices.Any(n=>n.Contains("IBAN") && n.Contains("güncelle")) || model.Outputs.Records.Count!=0)throw new Exception("IBAN eksikliği açıklayan popup/hakediş engeli yok.");
        model.Navigate("Teachers");Idle(window);var teacherGrid=(DataGrid)window.FindName("TeacherListGrid");teacherGrid.ScrollIntoView(model.Teachers.Records.Single(),teacherGrid.Columns.Last());Idle(window);
        var missing=Descendants<TextBlock>(teacherGrid).SingleOrDefault(t=>t.Text=="IBAN GİRİLMEDİ");
        if(missing is null || missing.Foreground is not SolidColorBrush color || color.Color!=Color.FromRgb(198,40,40))throw new Exception("Eksik IBAN kırmızı görünmüyor.");
        capture(window,Path.Combine(output,"Teachers-missing-iban.png"));
        model.Teachers.Selected=model.Teachers.Records.Single();model.UseSelectedTeacher();Idle(window);
        if(model.Monthly.EntryTeacher?.Id!=model.Teachers.Selected.Id)throw new Exception("Veri Girişine Al seçimi aylık karta taşımadı.");
        model.Teachers.Editor.Iban="TR330006100519786457841326";
        model.Teachers.Save();model.Monthly.RefreshTeachers();model.Calculation.RefreshTeachers();model.Navigate("Calculation");Idle(window);
        create.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Idle(window);if(model.Outputs.Records.Count!=1)throw new Exception("IBAN güncellendikten sonra hakediş oluşmadı.");
        var original=model.Outputs.Records.Single();var bytes=File.ReadAllBytes(original.Path);var filter=(ComboBox)window.FindName("OutputModuleCombo");
        filter.SelectedItem=model.Outputs.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);Idle(window);if(model.Outputs.Records.Count!=0)throw new Exception("Çıktı modül filtresi listeyi süzmedi.");
        filter.SelectedItem=model.Outputs.Modules.Single(m=>m.Id==ModuleId.TemelEgitim);Idle(window);
        var outputs=(DataGrid)window.FindName("OutputsGrid");outputs.ScrollIntoView(model.Outputs.Records.Single(),outputs.Columns.Last());Idle(window);
        var actions=Descendants<Button>(outputs).Where(b=>b.IsVisible).ToArray();
        if(Array.FindIndex(actions,b=>b.Content as string=="Detay")!=Array.FindIndex(actions,b=>b.Content as string=="Sil")+1)throw new Exception("Detay düğmesi Sil ile XML arasında değil.");
        var detailSeen=false;
        window.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>
        {
            var detail=Application.Current.Windows.OfType<PaymentDetailsWindow>().Single();var grid=(DataGrid)detail.FindName("DetailGrid");
            if(!grid.IsReadOnly || grid.Items.Count!=1 || grid.Columns.Count<25)throw new Exception("Detay salt okunur tam bordro listesini göstermiyor.");
            capture(detail,Path.Combine(output,"Output-details.png"));detailSeen=true;
            ((Button)detail.FindName("CloseDetailsButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }));
        actions.Single(b=>b.Content as string=="Detay").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Idle(window);
        if(!detailSeen || !bytes.SequenceEqual(File.ReadAllBytes(original.Path)))throw new Exception("Detay görüntüleme arşivi değiştirdi veya açılmadı.");
        capture(window,Path.Combine(output,"Outputs-module-filter.png"));
        model.ClearCurrentWork();window.Close();Console.WriteLine("Çıktı/giriş WPF: boş seçim, sıfır temizleme, IBAN kaydı/popup/kırmızı liste, modül filtresi, salt okunur detay ve Kapat, arşiv özeti doğrulandı.");
    }
    private static void Idle(Window w){w.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);w.UpdateLayout();}
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T:DependencyObject
    {for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is T item)yield return item;foreach(var nested in Descendants<T>(child))yield return nested;}}
}
