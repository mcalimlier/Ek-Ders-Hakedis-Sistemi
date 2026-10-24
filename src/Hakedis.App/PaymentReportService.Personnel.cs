using Hakedis.Contracts;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.App;

public sealed partial class PaymentReportService
{
    private static LaidOutReport LayoutPersonnel(PaymentSnapshot data,ReportDetails details,DateTimeOffset saved)
    {
        var p=new ReportPage(595.32,841.92);
        // Kullanıcının verdiği formun A4 koordinatları. Yılın her rakamı bağımsız hücredir.
        void Cell(double x,double y,double w,double h,string text,double font=8,bool bold=false,bool gray=false,bool border=true,bool left=false)
        {
            var lines=Wrap(text,w-6,font,bold);
            while(lines.Length*font*1.22>h-1 && font>6){font-=.25;lines=Wrap(text,w-6,font,bold);}
            if(lines.Length*font*1.22>h)throw new ArgumentException("Personel bildirimi alanına bilgi sığmıyor: Kurum ve İmza Bilgileri panelindeki uzun metni kontrol edin.");
            p.Cells.Add(new(x,y,w,h,lines,font,bold,gray,border,left,NeutralGray:gray));
        }
        const double l=31.5,r=563,w=r-l,split=233.5;
        Cell(l,84,w,23,"PERSONEL BİLDİRİMİ",16,true,false,false);
        Cell(l,117,w,12,details.Institution,8,false,false,false,true);
        Cell(split,129,r-split,12,"Ait Olduğu Ay",8,false,true);
        Cell(split,141,75,13,"Bütçe Yılı",8);
        var year=data.Year.ToString("D4");
        for(var i=0;i<4;i++)Cell(308.5+i*49,141,49,13,year[i].ToString(),8);
        Cell(504.5,141,r-504.5,13,Tr.DateTimeFormat.GetMonthName(data.Month).ToUpper(Tr),8,true);
        Cell(l,154,202,12,"1 Nolu Tablo",8,false,true);
        Cell(split,154,75,12,"Tahakkuk Toplamı",7,false,true);
        Cell(308.5,154,r-308.5,12,"Net Ele Geçen",8,false,true);
        var m=data.Movement??new PersonnelMovement(0,0,0);
        var counts=new[]{m.Previous,m.Entered,m.Exited,data.Rows.Length,data.Rows.Length};
        var labels=new[]{"Geçen Ay Personel Sayısı","Bu Ay İçinde Giren(x)","Bu Ay İçinde Çıkan","Ödeme Yapılacak Personel","Sayısı(1+2-3)"};
        var accrual=data.Rows.Sum(x=>x.Result.Accrual).ToString("N2",Tr);
        var net=data.Rows.Sum(x=>x.Result.Net).ToString("N2",Tr);
        for(var i=0;i<5;i++)
        {
            var y=166+i*12.4;var gray=i==4;
            Cell(l,y,50,12.4,i<4?(i+1).ToString():"",8,false,gray);
            Cell(81.5,y,102,12.4,labels[i],7.5,false,gray,true,true);
            Cell(183.5,y,50,12.4,counts[i].ToString(),8,false,gray);
            Cell(split,y,75,12.4,i is 0 or 4?accrual:"",8,false,gray);
            Cell(308.5,y,r-308.5,12.4,i is 0 or 4?net:"",8,false,gray);
        }
        Cell(l,240,50,12,"",8,false,true);
        Cell(81.5,240,152,12,"II Nolu Tablo   (xx)",8,false,true);
        Cell(split,240,r-split,12,details.SickLeaveStatement,8,false,true);
        double[] xs=[l,81.5,233.5,308.5,454.5,r];
        string[] headers=["Sıra N","Memurun Adı Soyadı","Geçen Aylarda\nKullandığı Hastalık\nizin süresi","Bu Ay Kullandığı\nHastalık İzni Süresi","Toplam Hastalık\nizni süresi"];
        for(var c=0;c<5;c++)Cell(xs[c],252,xs[c+1]-xs[c],31,headers[c],7.5);
        for(var row=0;row<6;row++)for(var c=0;c<5;c++)Cell(xs[c],283+row*12.4,xs[c+1]-xs[c],12.4,"",8);
        var signatures=new[]{("Gerçekleştirme Görevlisi",details.PreparedBy,details.PreparedTitle),("Harcama Yetkilisi",details.AuthorizedBy,details.AuthorizedTitle)};
        for(var i=0;i<2;i++)
        {
            var lines=new[]{signatures[i].Item1,saved.LocalDateTime.ToString("dd.MM.yyyy"),signatures[i].Item2,signatures[i].Item3};
            for(var line=0;line<lines.Length;line++)Cell(i==0?l:252.5,370.8+line*12.4,i==0?203.2:310.5,12.4,lines[line],8,false,false,false);
        }
        Cell(81.5,448,r-81.5,53,"(x)Ay içinde girenlerin isim listesi eklenecektir\n(xx) II Nolu tablonun düzenlenmesinden sadece personel birimi yetkilisi sorumludur\n(xxx) Müstakil personel birimi olmayan dairelerde personel işleriyle görevlendirilmiş\nolanlar tarafından imzalanır.",8,false,false,false,true);
        p.Cells.Add(new(l,84,w,497,[],8,false,false,true,false,2));
        return new("Personel Bildirimi.pdf",[p]);
    }
}
