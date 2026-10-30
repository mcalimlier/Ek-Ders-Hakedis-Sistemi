using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Hakedis.App;
using Hakedis.Storage;

internal static class PersonalPayrollChecks
{
    internal static void VerifyMetricRegions(IEnumerable<Border> cards)
    {
        foreach(var card in cards)
        {
            var symbols=Descendants<Image>(card).Select(i=>i.TransformToAncestor(card).TransformBounds(new Rect(0,0,i.ActualWidth,i.ActualHeight))).ToArray();
            foreach(var text in Descendants<TextBlock>(card).Where(t=>t.Text.Contains(" TL",StringComparison.Ordinal) || t.Text.EndsWith(" saat",StringComparison.Ordinal)))
            {
                var bounds=text.TransformToAncestor(card).TransformBounds(new Rect(0,0,text.ActualWidth,text.ActualHeight));
                if(symbols.Any(symbol=>symbol.IntersectsWith(bounds)))throw new InvalidOperationException("Kart tutarı simgeyle çakışıyor.");
                if(bounds.Left<0 || bounds.Right>card.ActualWidth+1 || bounds.Bottom>card.ActualHeight+1)throw new InvalidOperationException("Kart tutarı dışarı taşıyor.");
            }
        }
    }
    internal static void Run(ShellViewModel model,MainWindow window,string output,Action<Window,string> capture,List<string> openedFolders)
    {
        var primary=(StackPanel?)window.FindName("PrimaryNavigation");var secondary=(StackPanel?)window.FindName("SecondaryNavigation");
        if(primary is null || secondary is null)throw new InvalidOperationException("Sol menü iki gruba ayrılmadı.");
        if(!secondary.Children.OfType<Button>().Select(b=>b.Name).SequenceEqual(new[]{"NavPersonal","NavConstants","NavVault","NavSettings"}))throw new InvalidOperationException("Alt grup Kişisel Bordro / Dönem Sabitleri / Şifre Kasası / Ayarlar değil.");
        if(primary.Children.OfType<Button>().Any(b=>b.Tag as string is "Personal" or "Constants"))throw new InvalidOperationException("Alt grup düğmeleri üstte kaldı.");
        if(((TextBlock)window.FindName("SignatureAuthor")).Text!="Created by Murat ÇALIMLIER")throw new InvalidOperationException("İmza eklenmedi.");
        foreach(var width in new[]{1366,1040})
        {
            window.Width=width;window.Height=width==1366?768:728;model.Navigate("Constants");
            window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);window.UpdateLayout();
            var fields=(System.Windows.Controls.Primitives.UniformGrid)window.FindName("ConstantsFields");
            var textBoxes=Descendants<TextBox>(fields).ToArray();
            if(textBoxes.Length!=9)throw new InvalidOperationException("Dokuz sabit alanı yok.");
            for(var row=0;row<3;row++)
            {
                var positions=textBoxes.Skip(row*3).Take(3).Select(t=>t.TranslatePoint(new Point(),fields)).ToArray();
                if(positions.Max(p=>p.Y)-positions.Min(p=>p.Y)>1)throw new InvalidOperationException("Dönem sabitleri yatay hizada değil.");
            }
            if(textBoxes.Max(t=>t.ActualWidth)-textBoxes.Min(t=>t.ActualWidth)>1)throw new InvalidOperationException("Dönem sabiti alanlarının genişlikleri farklı.");
            capture(window,Path.Combine(output,$"Constants-aligned-{width}.png"));
        }
        var record=model.Outputs.Records.Single();var bytes=File.ReadAllBytes(record.Path);
        model.Outputs.Details.Institution="Örnek İlçe Milli Eğitim Müdürlüğü";model.Outputs.Details.Bank="Örnek Banka Şubesi";
        model.Outputs.Details.PreparedBy="Deneme Görevli";model.Outputs.Details.AuthorizedBy="Deneme Yetkili";model.Outputs.SaveDetails();
        ((Button)window.FindName("NavPersonal")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var personal=model.Personal;personal.Year=record.Document.Data.Year;personal.Month=record.Document.Data.Month;
        var teacher=personal.Teachers.Single();
        var grid=(DataGrid)window.FindName("PersonalTeachersGrid");
        if(grid.Columns.Count!=2 || !grid.IsReadOnly)throw new InvalidOperationException("Kişisel öğretmen listesi yalnız ad/soyad ve TC olmalı.");
        var create=(Button)window.FindName("CreatePersonalButton");var folder=(Button)window.FindName("PersonalFolderButton");
        if(create.IsEnabled || folder.IsEnabled)throw new InvalidOperationException("Seçim/PDF öncesi düğmeler açık.");
        grid.SelectedItem=teacher;window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        if(!create.IsEnabled || folder.IsEnabled)throw new InvalidOperationException("Öğretmen seçimi düğmelere yansımadı.");
        create.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        var pdf=personal.LastOutputPath;
        if(!File.Exists(pdf) || !folder.IsEnabled)throw new InvalidOperationException("Kişisel PDF oluşmadı: "+personal.Status);
        if(pdf.Length>=250)throw new InvalidOperationException("Test çıktısının yolu eski Windows araçları için gereksiz uzun.");
        if(Directory.GetFiles(Path.GetDirectoryName(pdf)!,"*.pdf").Length!=1)throw new InvalidOperationException("Kişisel çıktı birden fazla PDF içeriyor.");
        folder.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if(openedFolders.Count!=1 || openedFolders[0]!=Path.GetDirectoryName(pdf))throw new InvalidOperationException("Klasör düğmesi kaydedilen yeri açmadı.");
        File.WriteAllText(Path.Combine(output,"personal-pdf-path.txt"),pdf);
        File.WriteAllBytes(Path.Combine(output,"personal-source.json"),bytes);
        var service=new PaymentReportService();var report=service.PreparePersonal(record,teacher.Id,model.Outputs.Details.Read());
        if(report.Pages.Length!=1 || report.Pages[0].Width>=report.Pages[0].Height)throw new InvalidOperationException("Kişisel PDF tek dikey A4 değil.");
        if(report.Pages[0].Cells.Any(c=>c.X<0 || c.Y<0 || c.X+c.Width>596 || c.Y+c.Height>842))throw new InvalidOperationException("Kişisel bordro sayfa dışına taşıyor.");
        if(!report.Pages[0].Cells.SelectMany(c=>c.Lines).Contains(teacher.FullName))throw new InvalidOperationException("Kişisel PDF seçilen öğretmeni içermiyor.");
        var original=record.Document.Data.Rows.Single();var otherTeacher=original.Teacher with{Id=Guid.NewGuid(),FullName="DİĞER ÖĞRETMEN",IdentityNumber="10000000200"};
        var otherResult=new Hakedis.TemelEgitim.Core.TeacherPayrollModule().Calculate(new(7.5m,0,0),new PeriodConstantsEditor(record.Document.Data.Constants).Parameters());
        var mixed=record with{Document=record.Document with{Data=record.Document.Data with{Rows=[original,new(otherTeacher,new(otherTeacher.Id,7.5m,0,0),otherResult)]}}};
        var mixedReport=service.PreparePersonal(mixed,teacher.Id,model.Outputs.Details.Read());
        var personalText=string.Join("\n",mixedReport.Pages[0].Cells.SelectMany(c=>c.Lines));
        if(personalText.Contains(otherTeacher.FullName,StringComparison.Ordinal) || personalText.Contains(otherTeacher.IdentityNumber,StringComparison.Ordinal))throw new InvalidOperationException("Kişisel bordro başka kişinin bilgisini içeriyor.");
        if(!personalText.Contains(original.Result.Net.ToString("N2",System.Globalization.CultureInfo.GetCultureInfo("tr-TR"))+" TL",StringComparison.Ordinal))throw new InvalidOperationException("Kişisel bordro kişinin netini kullanmıyor.");
        try{service.ExportPersonal(record,Guid.NewGuid(),model.Outputs.Details.Read(),Path.Combine(output,"invalid-personal"));throw new Exception("Başka öğretmen için PDF üretildi.");}
        catch(ArgumentException){}
        if(Directory.Exists(Path.Combine(output,"invalid-personal")))throw new InvalidOperationException("Hatalı kişisel seçim klasör oluşturdu.");
        if(!bytes.SequenceEqual(File.ReadAllBytes(record.Path)))throw new InvalidOperationException("Kişisel bordro arşivi değiştirdi.");
        foreach(var width in new[]{1366,1040})
        {
            window.Width=width;window.Height=width==1366?768:728;window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
            capture(window,Path.Combine(output,$"Personal-{width}.png"));
        }
        personal.Month=personal.Month==12?1:personal.Month+1;
        if(personal.Teachers.Count!=0 || personal.CanCreate || personal.CanOpenFolder)throw new InvalidOperationException("Boş dönemde önceki seçim kaldı.");
        window.Width=1366;window.Height=768;
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T:DependencyObject
    {
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)
        {var child=VisualTreeHelper.GetChild(root,i);if(child is T item)yield return item;foreach(var next in Descendants<T>(child))yield return next;}
    }
}
