using System.IO;
using Hakedis.Contracts;
using Hakedis.Storage;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.App;

public sealed partial class PaymentReportService
{
    public LaidOutReport PreparePersonal(ArchivedPayment record,Guid teacherId,ReportDetails fallback)
    {
        PaymentArchiveStore.Validate(record.Document.Data,record.Document.ModuleId);
        if(record.Document.ModuleId!=ModuleId.TemelEgitim)throw new NotSupportedException("Bu modülün kişisel bordro eşlemesi henüz eklenmedi.");
        var item=record.Document.Data.Rows.SingleOrDefault(r=>r.Teacher.Id==teacherId)
            ??throw new ArgumentException("Seçilen öğretmen kayıtlı hakedişte bulunmuyor.");
        var details=ResolveDetails(record,fallback);var data=record.Document.Data;
        var page=new ReportPage(595.32,841.92);const double left=32,width=531.32;
        void Cell(double x,double y,double w,double h,string text,double font=9,bool bold=false,bool header=false,bool border=true,bool alignLeft=false)
        {
            var lines=Wrap(text,w-8,font,bold);
            while(lines.Length*font*1.22>h-2 && font>7){font-=.25;lines=Wrap(text,w-8,font,bold);}
            if(lines.Length*font*1.22>h-1)throw new ArgumentException("Kişisel bordro alanına bilgi sığmıyor. Uzun öğretmen veya kurum bilgisini kontrol edin.");
            page.Cells.Add(new(x,y,w,h,lines,font,bold,header,border,alignLeft));
        }
        string Money(decimal value)=>value.ToString("N2",Tr)+" TL";
        string Number(decimal value)=>value.ToString("0.############################",Tr);
        string Date(DateTime? value)=>value?.ToString("dd.MM.yyyy",Tr)??"—";
        Cell(left,28,width,24,"KİŞİSEL BORDRO",16,true,border:false);
        Cell(left,54,width,32,details.Institution,10,true,border:false);
        Cell(left,88,width,20,$"{record.Module} · {Tr.DateTimeFormat.GetMonthName(data.Month).ToUpper(Tr)}-{data.Year}",10,true,border:false);
        var teacher=item.Teacher;
        (string Label,string Value,double Height)[] identity=
        [
            (ExcelHeadings.FullName,teacher.FullName,24),
            (ExcelHeadings.IdentityNumber,teacher.IdentityNumber,24),
            (ExcelHeadings.Iban,teacher.Iban,24),
            (ExcelHeadings.School,teacher.School,30),
            (ExcelHeadings.Occupation,teacher.OccupationCode,24),
            (ExcelHeadings.StartDate,Date(teacher.StartDate),24),
            (ExcelHeadings.EndDate,Date(teacher.EndDate),24)
        ];
        var y=118d;
        foreach(var line in identity)
        {
            Cell(left,y,150,line.Height,line.Label,8,true,true,alignLeft:true);
            Cell(left+150,y,width-150,line.Height,line.Value,9,alignLeft:true);y+=line.Height;
        }
        Cell(left,304,width,20,"DERS BİLGİLERİ",10,true,true);
        double[] columns=[left,left+240,left+305,left+415,left+width];
        string[] headers=["DERS BİLGİLERİ","SAAT","BİRİM ÜCRET","TUTAR"];
        for(var i=0;i<4;i++)Cell(columns[i],324,columns[i+1]-columns[i],24,headers[i],8,true,true);
        var r=item.Result;
        string[][] hours=
        [
            [ExcelHeadings.DayHours,Number(item.Input.DayHours),Number(r.DayRate)+" TL",Money(r.DayGross)],
            [ExcelHeadings.DykHours,Number(item.Input.DykHours),Number(r.DykRate)+" TL",Money(r.DykGross)]
        ];
        for(var row=0;row<2;row++)for(var c=0;c<4;c++)Cell(columns[c],348+row*28,columns[c+1]-columns[c],28,hours[row][c],8,alignLeft:c==0);
        Cell(left,414,width,20,"HAKEDİŞ VE KESİNTİLER",10,true,true);
        (string Label,string Value)[] earnings=
        [
            (ExcelHeadings.BordroN6,Money(r.Gross)),(ExcelHeadings.BordroX6,Money(r.EmployerPremium)),
            (ExcelHeadings.BordroP6,Money(r.Accrual)),(ExcelHeadings.BordroQ5,Money(r.IncomeBase)),
            (ExcelHeadings.Hours,Number(item.Input.DayHours+item.Input.DykHours))
        ];
        (string Label,string Value)[] deductions=
        [
            (ExcelHeadings.BordroW6,Money(r.EmployeePremium)),(ExcelHeadings.BordroT6,Money(r.IncomeTax)),
            (ExcelHeadings.BordroV6,Money(r.StampTax)),(ExcelHeadings.BordroY6,Money(r.Garnishment)),
            (ExcelHeadings.BordroZ6,Money(r.Deductions))
        ];
        for(var i=0;i<5;i++)for(var side=0;side<2;side++)
        {
            var value=side==0?earnings[i]:deductions[i];var x=left+side*width/2;
            Cell(x,434+i*28,width/2-100,28,value.Label,8,true,alignLeft:true);
            Cell(x+width/2-100,434+i*28,100,28,value.Value,9,true);
        }
        Cell(left,586,width/2,42,ExcelHeadings.BordroAA5,12,true,true);
        Cell(left+width/2,586,width/2,42,Money(r.Net),16,true,true);
        var sgk=item.SgkInformation;var sgkError=item.SgkError;
        if(sgk is null && string.IsNullOrWhiteSpace(sgkError))
        {
            try{sgk=TeacherCalendar.CalculateMonth(data.Year,data.Month,teacher.StartDate,teacher.EndDate,item.Input.DayHours,item.Input.DykHours);}
            catch(ArgumentException ex){sgkError=ex.Message;}
        }
        var information=sgk is null?"SGK yan bilgisi: "+sgkError:
            $"{ExcelHeadings.BildirimG2}: {sgk.PremiumDays}     {ExcelHeadings.BildirimH2}: {sgk.MissingDays}";
        Cell(left,638,width,34,information+"\nSGK gün bilgileri parasal hakediş tutarını etkilemez.",8,border:false);
        var signatures=new[]{("Gerçekleştirme Görevlisi",details.PreparedBy,details.PreparedTitle),("Harcama Yetkilisi",details.AuthorizedBy,details.AuthorizedTitle)};
        for(var i=0;i<2;i++)
        {
            var lines=new[]{signatures[i].Item1,signatures[i].Item2,signatures[i].Item3};
            for(var j=0;j<lines.Length;j++)Cell(left+i*width/2,694+j*18,width/2,18,lines[j],9,j==1,border:false);
        }
        Cell(left,776,width,22,"Bu bordro seçilen dönemin kayıtlı hakedişinden hazırlanmıştır. Tutarlar TL cinsindedir.",8,border:false);
        return new("Kişisel Bordro.pdf",[page]);
    }
    public string ExportPersonal(ArchivedPayment record,Guid teacherId,ReportDetails fallback,string? outputRoot=null)
    {
        var report=PreparePersonal(record,teacherId,fallback);
        var basePath=Path.GetFullPath(outputRoot??DefaultOutputRoot);
        var folder=Path.Combine(basePath,"Kişisel Bordrolar",record.Document.ModuleId.ToString(),$"{record.Document.Data.Year:D4}-{record.Document.Data.Month:D2}",
            $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}");
        return Path.Combine(ExportDocuments([report],basePath,folder),report.FileName);
    }
}
