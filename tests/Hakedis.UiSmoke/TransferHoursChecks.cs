using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Hakedis.App;
using Hakedis.Contracts;
using Hakedis.Storage;
internal static class TransferHoursChecks
{
    internal static void Run(string output,Action<FrameworkElement,string> capture)
    {
        var root=Path.Combine(output,"split-data");var path=Path.Combine(root,"ogretmenler.json");
        var t=new TeacherRecord(Guid.NewGuid(),"Kurum Saat Denemesi","10000000146","TR330006100519786457841326","İlk Okul","2341.04",new(2026,9,1),null,ModuleId.TemelEgitim,
            new(ModuleId.TemelEgitim,"İlk Okul",[new(new(2026,9,15),ModuleId.TemelEgitim,"İlk Okul",ModuleId.DinOgretimi,"Yeni Okul")],[]));
        new TeacherDirectory(path).Save(t);var model=new ShellViewModel(path);model.Monthly.EntryModule=model.Monthly.SelectedModule;model.Monthly.EntryTeacher=model.Monthly.EntryTeachers.Single();
        var window=new MainWindow(model,_=>{},(_,_)=>true){ShowActivated=false,ShowInTaskbar=false};window.Show();Idle(window);
        TextBox Input(string property)=>Descendants<TextBox>(window).Single(b=>b.IsVisible && b.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path.Path==property);
        if(Descendants<TextBox>(window).Any(b=>b.IsVisible && b.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path.Path=="EntryDayHours"))throw new Exception("Geçiş ayında tek gündüz alanı gizlenmedi.");
        Input("PreviousSchoolDayHours").Text="40";Idle(window);
        if(model.Calculation.Rows.Single().Read().DayHours!=40 || !model.Calculation.Rows.Single().SchoolHoursWarning.Contains("Yeni Okul"))throw new Exception("Boş taraf sıfır/uyarı ya da canlı paylaşım çalışmadı.");
        Input("NewSchoolDayHours").Text="35";Idle(window);
        if(model.Calculation.Rows.Single().Read().DayHours!=75 || model.Calculation.Rows.Single().SchoolHoursWarning!="")throw new Exception("İki kurum toplamı güncellenmedi.");
        capture(window,Path.Combine(output,"Transfer-hours-entry.png"));
        Input("NewSchoolDayHours").Text="";Idle(window);model.Monthly.Save(false);model.Navigate("Calculation");Idle(window);
        var grid=(DataGrid)window.FindName("HybridGrid");var row=model.Calculation.Rows.Single();var warning=grid.Columns.Single(c=>c.Header as string=="KURUM SAAT UYARISI");grid.ScrollIntoView(row,warning);Idle(window);
        if(!Descendants<TextBlock>(grid).Any(b=>b.IsVisible && b.Text.Contains("0 kabul edildi")))throw new Exception("Hibrit uyarı görünmüyor.");
        capture(window,Path.Combine(output,"Transfer-hours-warning.png"));
        model.Calculation.SaveHybridChanges();model.Monthly.Month=10;model.Monthly.SelectedModule=model.Monthly.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);model.Monthly.EntryModule=model.Monthly.SelectedModule;model.Monthly.EntryTeacher=model.Monthly.EntryTeachers.Single();model.Navigate("Entry");Idle(window);
        if(model.Monthly.HasSchoolHourSplit || !Input("EntryDayHours").IsVisible)throw new Exception("Sonraki ay tek alana dönmedi.");
        capture(window,Path.Combine(output,"Transfer-hours-next-month.png"));model.ClearCurrentWork();window.Close();
        Console.WriteLine("Kurum saatleri WPF: iki ayrı alan, toplam, boş taraf sıfır/hibrit uyarı, kayıt ve sonraki ay tek alan doğrulandı.");
    }
    private static void Idle(Window w){w.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);w.UpdateLayout();}
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T:DependencyObject
    {for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is T item)yield return item;foreach(var nested in Descendants<T>(child))yield return nested;}}
}
