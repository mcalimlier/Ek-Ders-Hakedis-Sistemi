using Hakedis.Contracts;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.App;

public sealed partial class PaymentReportService
{
    // Bütün mali alanlar iki yatay bantta korunur. Tek sayfa şartı yüzünden satır atılmaz.
    private static LaidOutReport LayoutPayroll(PaymentReport report,ReportDetails details,DateTimeOffset date)
    {
        const double width=842,height=595,margin=7,available=width-margin*2;
        for(var font=8d;font>=6; font-=.25)
        {
            var page=new ReportPage(width,height);var y=margin;
            void Text(string text,double size,bool bold=false)
            {
                var lines=Wrap(text,available-6,size,bold);var h=lines.Length*size*1.22+3;
                page.Cells.Add(new(margin,y,available,h,lines,size,bold));y+=h;
            }
            Text(report.Title,10,true);
            foreach(var info in report.Information)Text(info,7);
            foreach(var table in report.Tables)
            {
                Text(table.Title,8,true);
                var widths=table.Widths.Select(w=>w/table.Widths.Sum()*available).ToArray();
                void Draw(string[] values,bool header,bool total)
                {
                    var size=total?font+.3:font;
                    var wrapped=values.Select((v,i)=>Wrap(v,widths[i]-6,size,header||total)).ToArray();
                    var h=Math.Max(size*1.22+3,wrapped.Max(l=>l.Length)*size*1.22+3);
                    var x=margin;
                    for(var i=0;i<widths.Length;i++)
                    {page.Cells.Add(new(x,y,widths[i],h,wrapped[i],size,header||total,header||total,true));x+=widths[i];}
                    y+=h;
                }
                Draw(table.Headers,true,false);
                foreach(var row in table.Rows)Draw(row,false,false);
                if(table.Total is not null)Draw(table.Total,false,true);
                y+=4;
            }
            Text("Bordroda yazılı alacaklar kayıtlara uygundur.",7);
            var signatures=new[]{("Gerçekleştirme Görevlisi",details.PreparedBy,details.PreparedTitle),("Harcama Yetkilisi",details.AuthorizedBy,details.AuthorizedTitle)};
            var signatureLines=signatures.Select(s=>Wrap($"{s.Item1}\n{date.LocalDateTime:dd.MM.yyyy}\n{s.Item2}\n{s.Item3}",available/2-6,8,false)).ToArray();
            var signatureHeight=signatureLines.Max(l=>l.Length)*8*1.22+4;
            if(y+signatureHeight>height-margin)continue;
            for(var i=0;i<2;i++)page.Cells.Add(new(margin+i*available/2,y,available/2,signatureHeight,signatureLines[i],8));
            return new(report.FileName,[page]);
        }
        throw new ArgumentException("Bordrodaki bütün satırlar okunaklı yazıyla tek yatay A4 sayfasına sığmıyor. PDF oluşturulmadı; satır veya tutar kesilmedi. Öğretmen sayısını ve uzun alanları kontrol edin.");
    }
}
