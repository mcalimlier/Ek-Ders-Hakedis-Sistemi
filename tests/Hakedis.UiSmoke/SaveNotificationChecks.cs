using System.IO;
using System.Reflection;
using System.Windows;
using Hakedis.App;
using Hakedis.Storage;

internal static class SaveNotificationChecks
{
    internal static void Run(string output)
    {
        var root=Path.Combine(output,"save-feedback-"+Guid.NewGuid().ToString("N"));
        var path=Path.Combine(root,"ogretmenler.json");
        var model=new ShellViewModel(path);
        var messages=new List<string>();
        var window=new MainWindow(model,messages.Add,(_,_)=>true);
        void Click(string handler,int expected=1)
        {
            var before=messages.Count;
            typeof(MainWindow).GetMethod(handler,BindingFlags.NonPublic|BindingFlags.Instance)!
                .Invoke(window,[window,new RoutedEventArgs()]);
            if(messages.Count!=before+expected)throw new Exception(handler+": kayıt bildirimi sayısı yanlış.");
        }
        void Check(bool condition,string message){if(!condition)throw new Exception(message);}
        Click("RegisterTeacher_Click");
        Check(!File.Exists(path) && messages[^1].Contains("MESLEK KODU"),"Geçersiz öğretmen başarı bildirimi aldı.");
        model.Registration.Editor.FullName="Bildirim Deneme Öğretmeni";
        model.Registration.Editor.IdentityNumber="10000000146";
        model.Registration.Editor.Iban="TR330006100519786457841326";
        model.Registration.Editor.OccupationCode="2341.04";
        Click("RegisterTeacher_Click");
        Check(new TeacherDirectory(path).Read().Count==1 && messages[^1].Contains("kaydedildi"),"Yeni öğretmen kaydı ve bildirim ayrıştı.");
        Click("SaveTeacher_Click");
        Check(messages[^1].Contains("seçin"),"Öğretmen seçimi doğrulaması görünmedi.");
        model.Teachers.Selected=model.Teachers.Records.Single();
        model.Teachers.Editor.School="Bildirim Okulu";
        Click("SaveTeacher_Click");
        Check(new TeacherDirectory(path).Read().Single().School=="Bildirim Okulu","Öğretmen güncellemesi saklanmadı.");
        Click("SaveMonthly_Click");
        Check(messages[^1].Contains("kaydedildi"),"Aylık kayıt başarı bildirimi yok.");
        model.Monthly.DayHours="geçersiz";
        Click("SaveMonthly_Click");
        Check(!messages[^1].Contains("kaydedildi"),"Geçersiz aylık giriş başarı bildirimi aldı.");
        model.Monthly.DayHours="0";
        foreach(var handler in new[]{"SaveConstants_Click","SaveRetiredConstants_Click"})
        {
            Click(handler,0); // First click opens editing; no persistence yet.
            Click(handler);
            Check(messages[^1].Contains("kaydedildi"),"Sabit kaydı başarı bildirimi yok.");
        }
        Click("SaveConstants_Click",0);
        var savedCoefficient=model.CommonConstants.Editor.Coefficient;
        model.CommonConstants.Editor.Coefficient="geçersiz";
        Click("SaveConstants_Click");
        Check(model.CommonConstants.IsEditing && messages[^1].Contains("sayı"),"Geçersiz sabit için açıklayıcı hata yok.");
        model.CommonConstants.Editor.Coefficient=savedCoefficient;
        Click("SaveConstants_Click");
        Check(!model.CommonConstants.IsEditing,"Sabit düzenlemesi sonraki testten önce tamamlanmadı.");
        Click("SaveReportDetails_Click");
        Check(messages[^1].Contains("kaydedildi"),"Belge bilgileri başarı bildirimi yok.");
        Click("ApplySettings_Click");
        Check(messages[^1].Contains("kaydedildi"),"Kalıcı görünüm tercihi bildirimi yok.");
        model.Calculation.Reload();
        Click("SaveHybrid_Click");
        Check(messages[^1]=="Kaydedilecek değişiklik yok.","Değişiklik yokken yanlış başarı bildirimi gösterildi.");
        model.Calculation.Rows.Single().School="Toplu kayıt okulu";
        Click("SaveHybrid_Click");
        Check(new TeacherDirectory(path).Read().Single().School=="Toplu kayıt okulu","Toplu kayıt saklanmadı: "+messages[^1]);
        var savedBytes=File.ReadAllBytes(path);
        model.Teachers.Reload();model.Teachers.Selected=model.Teachers.Records.Single();
        model.Teachers.Editor.School="Kaydedilemeyecek okul";
        using(var locked=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
        {
            Click("SaveTeacher_Click");
            Check(!messages[^1].Contains("güncellendi") && !messages[^1].Contains("kaydedildi"),"Dosya yazma hatasında yanlış başarı bildirimi gösterildi.");
        }
        Check(File.ReadAllBytes(path).SequenceEqual(savedBytes),"Başarısız kaydet dosyayı değiştirdi.");
        window.Close();
        Console.WriteLine("Kaydet bildirimleri: öğretmen, aylık giriş, sabitler, belge bilgileri, görünüm, toplu kayıt; geçersiz girişler ve boş değişiklik doğrulandı.");
    }
}
