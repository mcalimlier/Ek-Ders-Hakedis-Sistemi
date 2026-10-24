using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Hakedis.Contracts;
using Hakedis.Storage;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.App;

public sealed record ReportCell(double X,double Y,double Width,double Height,string[] Lines,double FontSize,bool Bold=false,bool Header=false,bool Border=false,bool Left=false,double BorderWidth=.4,bool NeutralGray=false);
public sealed class ReportPage(double width,double height)
{
    public double Width {get;}=width;
    public double Height {get;}=height;
    public List<ReportCell> Cells {get;}=[];
}
public sealed record LaidOutReport(string FileName,ReportPage[] Pages);

public sealed partial class PaymentReportService
{
    private static readonly CultureInfo Tr=CultureInfo.GetCultureInfo("tr-TR");
    public static string DefaultOutputRoot=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"ÜcretliUsta Öğretici Hakediş Sistemi","Çıktılar");
    public LaidOutReport[] Prepare(ArchivedPayment record,ReportDetails fallback)
    {
        PaymentArchiveStore.Validate(record.Document.Data,record.Document.ModuleId);
        var details=ResolveDetails(record,fallback);
        var data=record.Document.Data;
        if(data.Movement?.RuleVersion!="employment-dates.v2")
        {
            var moduleFolder=Directory.GetParent(record.Path)!;
            var root=moduleFolder.Parent!.Parent!.FullName;
            data=data with {Movement=new PaymentArchiveStore(root).Movement(record.Document.ModuleId,data.Year,data.Month,data.Rows)};
        }
        return PaymentReports.Build(data,details).Select(report=>report.FileName switch
        {
            "Bordro.pdf"=>LayoutPayroll(report,details,record.SavedAt),
            "Personel Bildirimi.pdf"=>LayoutPersonnel(data,details,record.SavedAt),
            _=>Layout(report,details,record.SavedAt)
        }).ToArray();
    }
    public string Export(ArchivedPayment record,ReportDetails fallback,string? outputRoot=null)
    {
        var documents=Prepare(record,fallback);var basePath=Path.GetFullPath(outputRoot??DefaultOutputRoot);
        var folder=Path.Combine(basePath,record.Document.ModuleId.ToString(),$"{record.Document.Data.Year:D4}-{record.Document.Data.Month:D2}",
            $"{DateTime.Now:yyyyMMdd-HHmmss}-{record.Version[..8]}-{Guid.NewGuid():N}");
        return ExportDocuments(documents,basePath,folder);
    }
    private static ReportDetails ResolveDetails(ArchivedPayment record,ReportDetails fallback)
    {
        var details=record.Document.Data.Details;
        if(details is null || string.IsNullOrWhiteSpace(details.Institution))details=fallback;
        if(string.IsNullOrWhiteSpace(details.Institution) || string.IsNullOrWhiteSpace(details.Bank) ||
           string.IsNullOrWhiteSpace(details.PreparedBy) || string.IsNullOrWhiteSpace(details.AuthorizedBy))
            throw new ArgumentException("Önce Kurum ve İmza Bilgileri panelindeki kurum, banka ve imzacı bilgilerini doldurun ve kaydedin.");
        return details;
    }
    private static string ExportDocuments(LaidOutReport[] documents,string basePath,string folder)
    {
        var stage=folder+".hazirlaniyor";Directory.CreateDirectory(stage);
        try
        {
        // Ayrı klasör tamamlanmadan başarı bildirilmez; mevcut çıktılar ezilmez.
        foreach(var document in documents)
        {
            var path=Path.Combine(stage,document.FileName);var tmp=path+".tmp";
            using(var pdf=new PdfDocument())
            {
                pdf.Info.Title=Path.GetFileNameWithoutExtension(document.FileName);pdf.Info.Creator="Ücretli Ek Ders Hakediş Sistemi";
                foreach(var sheet in document.Pages)
                {
                    var page=pdf.AddPage();page.Width=XUnit.FromPoint(sheet.Width);page.Height=XUnit.FromPoint(sheet.Height);
                    using var g=XGraphics.FromPdfPage(page);
                    foreach(var cell in sheet.Cells)
                    {
                        var rect=new XRect(cell.X,cell.Y,cell.Width,cell.Height);
                        if(cell.Header)g.DrawRectangle(new XSolidBrush(cell.NeutralGray?XColor.FromArgb(192,192,192):XColor.FromArgb(235,239,244)),rect);
                        if(cell.Border)g.DrawRectangle(new XPen(XColors.Black,cell.BorderWidth),rect);
                        var font=new XFont("Arial",cell.FontSize,cell.Bold?XFontStyleEx.Bold:XFontStyleEx.Regular);
                        var lineHeight=cell.FontSize*1.22;var top=cell.Y+(cell.Height-cell.Lines.Length*lineHeight)/2;
                        for(var i=0;i<cell.Lines.Length;i++)g.DrawString(cell.Lines[i],font,XBrushes.Black,
                            new XRect(cell.X+3,top+i*lineHeight,cell.Width-6,lineHeight),cell.Left?XStringFormats.TopLeft:XStringFormats.TopCenter);
                    }
                }
                pdf.Save(tmp);
            }
            File.Move(tmp,path);
        }
        Directory.Move(stage,folder);return folder;
        }
        finally
        {
            var full=Path.GetFullPath(stage);
            if(Directory.Exists(full) && full.StartsWith(basePath+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase) && full.EndsWith(".hazirlaniyor",StringComparison.Ordinal))Directory.Delete(full,true);
        }
    }
    public FixedDocument PrintDocument(LaidOutReport report)
    {
        var document=new FixedDocument();
        foreach(var sheet in report.Pages)
        {
            var page=new FixedPage{Width=sheet.Width*96/72,Height=sheet.Height*96/72,Background=Brushes.White};
            foreach(var cell in sheet.Cells)
            {
                var border=new Border{Width=cell.Width*96/72,Height=cell.Height*96/72,BorderBrush=Brushes.Black,
                    BorderThickness=new Thickness(cell.Border?cell.BorderWidth*96/72:0),Background=cell.Header?new SolidColorBrush(cell.NeutralGray?Color.FromRgb(192,192,192):Color.FromRgb(235,239,244)):Brushes.Transparent};
                border.Child=new TextBlock{Text=string.Join("\n",cell.Lines),FontFamily=new FontFamily("Arial"),FontSize=cell.FontSize*96/72,
                    FontWeight=cell.Bold?FontWeights.Bold:FontWeights.Normal,TextAlignment=cell.Left?TextAlignment.Left:TextAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(3,0,3,0),Foreground=Brushes.Black};
                FixedPage.SetLeft(border,cell.X*96/72);FixedPage.SetTop(border,cell.Y*96/72);page.Children.Add(border);
            }
            var content=new PageContent();((System.Windows.Markup.IAddChild)content).AddChild(page);document.Pages.Add(content);
        }
        return document;
    }
    public bool Print(ArchivedPayment record,ReportDetails fallback)
    {
        var reports=Prepare(record,fallback);var dialog=new PrintDialog();if(dialog.ShowDialog()!=true)return false;
        dialog.PrintDocument(CreatePrintPaginator(reports,new Size(dialog.PrintableAreaWidth,dialog.PrintableAreaHeight)),
            $"Hakediş · {record.Period} · Bordro, Banka Listesi, Personel Bildirimi");return true;
    }
    public DocumentPaginator CreatePrintPaginator(LaidOutReport[] reports,Size size)
    {
        var sequence=new FixedDocumentSequence();
        foreach(var report in reports)
        {
            var reference=new DocumentReference();reference.SetDocument(PrintDocument(report));sequence.References.Add(reference);
        }
        sequence.DocumentPaginator.ComputePageCount();return new FitPaginator(sequence.DocumentPaginator,size);
    }
    private sealed class FitPaginator(DocumentPaginator source,Size size):DocumentPaginator
    {
        public override bool IsPageCountValid=>source.IsPageCountValid;
        public override int PageCount=>source.PageCount;
        public override Size PageSize{get=>size;set=>source.PageSize=value;}
        public override IDocumentPaginatorSource Source=>source.Source;
        public override DocumentPage GetPage(int number)
        {
            var original=source.GetPage(number);var normalScale=Math.Min(size.Width/original.Size.Width,size.Height/original.Size.Height);
            var rotatedScale=Math.Min(size.Width/original.Size.Height,size.Height/original.Size.Width);
            var rotate=rotatedScale>normalScale;var scale=rotate?rotatedScale:normalScale;
            var visual=new DrawingVisual();using(var drawing=visual.RenderOpen())
            {
                drawing.PushTransform(new ScaleTransform(scale,scale));
                if(rotate)drawing.PushTransform(new MatrixTransform(0,1,-1,0,original.Size.Height,0));
                drawing.DrawRectangle(new VisualBrush(original.Visual){Stretch=Stretch.Fill},null,new Rect(original.Size));
                if(rotate)drawing.Pop();
                drawing.Pop();
            }
            return new(visual,size,new Rect(size),new Rect(size));
        }
    }
    private static string[] Wrap(string text,double width,double size,bool bold)
    {
        var lines=new List<string>();
        foreach(var paragraph in text.Replace("\r","").Split('\n'))
        {
            var line="";
            foreach(var word in paragraph.Split(' ',StringSplitOptions.RemoveEmptyEntries))
            {
                var trial=line.Length==0?word:line+" "+word;
                if(Measure(trial,size,bold)<=width){line=trial;continue;}
                if(line.Length>0){lines.Add(line);line="";}
                foreach(var c in word)
                {if(line.Length>0 && Measure(line+c,size,bold)>width){lines.Add(line);line="";}line+=c;}
            }
            lines.Add(line);
        }
        return lines.Count==0?[""]:lines.ToArray();
    }
    private static double Measure(string text,double size,bool bold)=>new FormattedText(text,Tr,FlowDirection.LeftToRight,
        new Typeface(new FontFamily("Arial"),FontStyles.Normal,bold?FontWeights.Bold:FontWeights.Normal,FontStretches.Normal),size,Brushes.Black,1).WidthIncludingTrailingWhitespace;
    private static LaidOutReport Layout(PaymentReport report,ReportDetails details,DateTimeOffset savedAt)
    {
        var width=report.Landscape?842d:595d;var height=report.Landscape?595d:842d;const double margin=22;
        var pages=new List<ReportPage>();ReportPage page=null!;var y=0d;
        void AddText(string text,double font=9,bool bold=false)
        {
            var lines=Wrap(text,width-2*margin-6,font,bold);var h=lines.Length*font*1.22+8;
            if(y+h>height-44)NewPage();page.Cells.Add(new(margin,y,width-2*margin,h,lines,font,bold));y+=h;
        }
        void NewPage()
        {
            page=new(width,height);pages.Add(page);y=margin;
            if(report.FileName=="Banka Listesi.pdf")
            {
                page.Cells.Add(new(margin,y,width-2*margin,26,[report.Title],10,true,false,true));y+=26;
                var labels=new[]{"DAİRENİN ADI","BANKANIN ADI","AİT OLDUĞU AY"};
                var values=new[]{details.Institution,details.Bank,report.Information[2].Split("             ").Last().Split('-')[0].Trim().ToUpper(Tr)};
                for(var i=0;i<3;i++)
                {
                    var wrapped=Wrap(values[i],width-2*margin-216,9,true);var h=Math.Max(20,wrapped.Length*11+5);
                    page.Cells.Add(new(margin,y,210,h,[labels[i]],9,true,false,true,true));
                    page.Cells.Add(new(margin+210,y,width-2*margin-210,h,wrapped,9,true,false,true,i!=2));y+=h;
                }
            }
            else
            {
            var lines=Wrap(report.Title,width-2*margin-6,10,true);var titleHeight=lines.Length*12.2+12;
            page.Cells.Add(new(margin,y,width-2*margin,titleHeight,lines,10,true));y+=titleHeight;
            foreach(var line in report.Information)
            {var wrapped=Wrap(line,width-2*margin-6,8,false);var h=wrapped.Length*9.76+4;page.Cells.Add(new(margin,y,width-2*margin,h,wrapped,8));y+=h;}
            y+=8;
            }
        }
        NewPage();
        foreach(var table in report.Tables)
        {
            var font=report.Landscape?7d:9d;var available=width-2*margin;var widths=table.Widths.Select(w=>w/table.Widths.Sum()*available).ToArray();
            var heads=table.Headers.Select((h,i)=>Wrap(h,widths[i]-6,font,true)).ToArray();
            var headerHeight=heads.Max(l=>l.Length)*font*1.22+14;
            void Draw(string[][] lines,double h,bool header,bool bold)
            {
                if(report.FileName=="Banka Listesi.pdf" && !header && bold)
                {
                    var span=widths.Take(4).Sum();h=22;
                    page.Cells.Add(new(margin,y,span,h,["TOPLAM"],font,true,false,true));
                    page.Cells.Add(new(margin+span,y,widths[4],h,lines[4],font,true,false,true));y+=h;return;
                }
                var x=margin;for(var c=0;c<widths.Length;c++){page.Cells.Add(new(x,y,widths[c],h,lines[c],font,bold,header,true));x+=widths[c];}y+=h;
            }
            void Header()
            {
                if(y+headerHeight+38>height-44)NewPage();
                if(table.Title!="")AddText(table.Title,9,true);
                Draw(heads,headerHeight,true,true);
            }
            Header();
            foreach(var row in table.Rows.Concat(table.Total is null?[]:new[]{table.Total}))
            {
                var lines=row.Select((t,i)=>Wrap(t,widths[i]-6,font,ReferenceEquals(row,table.Total))).ToArray();
                var h=Math.Max(22,lines.Max(l=>l.Length)*font*1.22+10);
                if(y+h>height-44){NewPage();Header();}
                if(y+h>height-44)throw new ArgumentException("Rapor satırı bir sayfaya sığmayacak kadar uzun. Öğretmen / kurum bilgilerini kontrol edin.");
                Draw(lines,h,false,ReferenceEquals(row,table.Total));
            }
            y+=12;
        }
        foreach(var note in report.Notes)AddText(note,8);
        var signatures=new[]{("Gerçekleştirme Görevlisi",details.PreparedBy,details.PreparedTitle),("Harcama Yetkilisi",details.AuthorizedBy,details.AuthorizedTitle)}.ToList();
        if(report.FileName=="Banka Listesi.pdf")signatures.Add(("SAYMAN",details.Treasurer,""));
        var sw=(width-2*margin)/signatures.Count;
        var signatureLines=signatures.Select(sig=>Wrap($"{sig.Item1}\n{savedAt.LocalDateTime:dd.MM.yyyy}\n\n{sig.Item2}\n{sig.Item3}",sw-8,9,false)).ToArray();
        var signatureHeight=signatureLines.Max(l=>l.Length)*11+14;
        if(y+signatureHeight>height-44)NewPage();
        for(var i=0;i<signatures.Count;i++)page.Cells.Add(new(margin+i*sw,y,sw,signatureHeight,signatureLines[i],9));
        for(var i=0;i<pages.Count;i++)pages[i].Cells.Add(new(margin,height-29,width-2*margin,15,[$"{Path.GetFileNameWithoutExtension(report.FileName)} · {i+1}/{pages.Count} · Kayıt: {savedAt.LocalDateTime:dd.MM.yyyy HH:mm}"],7));
        return new(report.FileName,pages.ToArray());
    }
}
