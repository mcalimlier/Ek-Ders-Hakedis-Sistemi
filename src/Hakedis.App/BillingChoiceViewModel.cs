using System.ComponentModel;
using System.Globalization;
using Hakedis.Contracts;
using Hakedis.Storage;

namespace Hakedis.App;

public sealed class BillingChoiceViewModel : INotifyPropertyChanged
{
    public TeacherRecord Teacher {get;}
    public int Year {get;}
    public int Month {get;}
    public string PersonText=>$"{Teacher.FullName} · {Teacher.IdentityNumber}";
    public string PeriodText=>$"{CultureInfo.GetCultureInfo("tr-TR").DateTimeFormat.GetMonthName(Month)}-{Year}";
    public string HistoryText=>TeacherEditor.From(Teacher).HistoryText;
    public string ExistingInputsText {get;}
    public IReadOnlyList<ModuleDescriptor> Modules {get;}
    private ModuleDescriptor? selected;
    public ModuleDescriptor? SelectedModule {get=>selected;set{selected=value is not null && Modules.Contains(value)?value:null;Notify();}}
    public bool CanAccept=>SelectedModule is not null;
    private readonly MonthlyInput originalInput;
    public bool HasSplit=>originalInput.SchoolHoursSeparated || originalInput.PreviousSchoolDayHours is not null || originalInput.NewSchoolDayHours is not null;
    public string DayHours {get;set;}
    public string DykHours {get;set;}
    public string Garnishment {get;set;}
    public string Status {get;private set;}="Ödeme modülünü seçin ve ayın tamamına ait toplam saatleri kontrol edin. Kurum gündüz saatleri toplamı kullanılır.";
    public event PropertyChangedEventHandler? PropertyChanged;
    public BillingChoiceViewModel(TeacherRecord teacher,int year,int month,MonthlyInput current,string root)
    {
        originalInput=current;Teacher=teacher;Year=year;Month=month;
        var candidates=TeacherRouting.Candidates(teacher,year,month);
        var paid=new PaymentArchiveStore(root).ReadAll().Where(a=>a.Document.Data.Year==year && a.Document.Data.Month==month &&
            a.Document.Data.Rows.Any(r=>r.Teacher.Id==teacher.Id || (!string.IsNullOrEmpty(teacher.IdentityNumber) && r.Teacher.IdentityNumber==teacher.IdentityNumber))).ToArray();
        Modules=PayrollCores.Catalog.Where(m=>m.IsAvailable && candidates.Contains(m.Id) && (paid.Length==0 || paid.All(a=>a.Document.ModuleId==m.Id))).ToArray();
        DayHours=Number(current.DayHours);DykHours=Number(current.DykHours);Garnishment=Number(current.Garnishment);
        var store=new MonthlyPayrollStore(root);
        ExistingInputsText=string.Join(Environment.NewLine,PayrollCores.Catalog.Where(m=>TeacherRouting.Modules(teacher).Contains(m.Id)).Select(m=>
        {
            var input=store.Read(m.Id,year,month).Entries.SingleOrDefault(i=>i.TeacherId==teacher.Id);
            return input is null?$"{m.Name}: kayıtlı aylık giriş yok.":$"{m.Name}: {Number(input.DayHours)} gündüz / {Number(input.DykHours)} DYK / {Number(input.Garnishment)} TL icra.";
        }));
        if(paid.Length>0)Status="Bu ay kayıtlı bordro bulundu. Ödeme modülü o bordroyla aynı olmalıdır; başka modül için önce kayıtlı bordroyu kontrol edin.";
    }
    public MonthlyInput ReadInput()
    {
        if(SelectedModule is null)throw new ArgumentException("Bu ay için tek ödeme modülünü seçin.");
        var i=originalInput with {DayHours=HasSplit?originalInput.DayHours:PeriodConstantsEditor.Number(DayHours),DykHours=PeriodConstantsEditor.Number(DykHours),Garnishment=PeriodConstantsEditor.Number(Garnishment)};
        i.ValidateSchoolHours();
        if(i.DayHours<0 || i.DykHours<0 || i.Garnishment<0)throw new ArgumentException("Aylık saatler ve kesinti negatif olamaz.");
        return i;
    }
    public void ShowError(string text){Status=text;Notify();}
    private void Notify(){foreach(var name in new[]{nameof(SelectedModule),nameof(CanAccept),nameof(Status)})PropertyChanged?.Invoke(this,new(name));}
    private static string Number(decimal v)=>v.ToString(CultureInfo.GetCultureInfo("tr-TR"));
}
