using System.ComponentModel;
using System.Globalization;
using Hakedis.Contracts;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.App;

public sealed class PayrollRow : INotifyPropertyChanged
{
    private string day = "0", dyk = "0", garnishment = "0";
    private DateTime? monthStart, monthEnd;
    private readonly Func<TeacherParameters> parameters;
    private readonly Action<MonthlyInput>? validate;
    private readonly Func<(int Year,int Month)>? period;
    public TeacherRecord Teacher { get; private set; }
    public TeacherRecord SourceTeacher { get; private set; }
    public MonthlyInput? SavedInput { get; private set; }
    private string acceptedDay="0", acceptedDyk="0", acceptedGarnishment="0";
    private DateTime? acceptedStart, acceptedEnd;
    public bool HasChanges => Teacher!=SourceTeacher || day!=acceptedDay || dyk!=acceptedDyk ||
        garnishment!=acceptedGarnishment || monthStart!=acceptedStart || monthEnd!=acceptedEnd;
    public string FullName {get=>Teacher.FullName;set=>EditTeacher(Teacher with {FullName=value});}
    public string IdentityNumber {get=>Teacher.IdentityNumber;set=>EditTeacher(Teacher with {IdentityNumber=value});}
    public string Iban {get=>Teacher.Iban;set=>EditTeacher(Teacher with {Iban=value});}
    public string School {get=>Teacher.School;set=>EditTeacher(Teacher with {School=value});}
    public string OccupationCode {get=>Teacher.OccupationCode;set=>EditTeacher(Teacher with {OccupationCode=value});}
    public DateTime? StartDate {get=>Teacher.StartDate;set=>EditTeacher(Teacher with {StartDate=value});}
    public DateTime? EndDate {get=>Teacher.EndDate;set=>EditTeacher(Teacher with {EndDate=value});}
    private void EditTeacher(TeacherRecord next)
    {if(next==Teacher)return;Teacher=next;Recalculate();Changed?.Invoke(this);}
    public void RefreshTeacher(TeacherRecord saved,bool replacePreview=false)
    {if(saved.Id!=Teacher.Id || saved.ModuleId!=Teacher.ModuleId)throw new InvalidOperationException("Öğretmen uyuşmuyor.");
        if(SourceTeacher!=saved || replacePreview){Teacher=SourceTeacher=saved;Recalculate();}}
    public void AcceptInput(MonthlyInput persisted)
    {
        var draft=Teacher;Teacher=SourceTeacher;AcceptChanges(persisted);Teacher=draft;Recalculate();
    }
    public void AcceptChanges(MonthlyInput? persisted=null)
    {
        SourceTeacher=Teacher;SavedInput=persisted;
        acceptedDay=day;acceptedDyk=dyk;acceptedGarnishment=garnishment;
        acceptedStart=monthStart;acceptedEnd=monthEnd;Recalculate();
    }
    public bool HasInput { get; private set; }
    public TeacherResult? Result { get; private set; }
    public string Error { get; private set; } = "";
    public CalendarResult? SgkInformation { get; private set; }
    public string SgkError { get; private set; } = "";
    public string MissingDays => SgkInformation?.MissingDays.ToString(CultureInfo.GetCultureInfo("tr-TR")) ?? "—";
    public string MaximumDaysText => SgkInformation?.CalendarDays.ToString(CultureInfo.GetCultureInfo("tr-TR")) ?? "—";
    public string WorkingDaysText => (SgkInformation?.PremiumDays ?? Result?.PremiumDays)?.ToString(CultureInfo.GetCultureInfo("tr-TR")) ?? "—";
    public int Sequence { get; set; }
    public string IbanCheck=>string.IsNullOrEmpty(Teacher.Iban)?"-":Teacher.Iban.Length==26?"DOĞRU":"HATALI";
    public string TotalHours
    {
        get {try {var i=Read();return Text(i.DayHours+i.DykHours);}catch(Exception ex) when(ex is ArgumentException or OverflowException){return "—";}}
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<PayrollRow>? Changed;
    public string DayHours { get => day; set { if(day == value) return; day=value; Update(); } }
    public string DykHours { get => dyk; set { if(dyk == value) return; dyk=value; Update(); } }
    public string Garnishment { get => garnishment; set { if(garnishment == value) return; garnishment=value; Update(); } }
    public DateTime? MonthStart { get => monthStart; set { if(monthStart==value) return; monthStart=value; Update(); } }
    public DateTime? MonthEnd { get => monthEnd; set { if(monthEnd==value) return; monthEnd=value; Update(); } }
    public PayrollRow(TeacherRecord teacher, MonthlyInput? input, Func<TeacherParameters> parameters, Action<MonthlyInput>? validate=null,
        Func<(int Year,int Month)>? period=null)
    { Teacher=SourceTeacher=teacher; this.parameters=parameters; this.validate=validate;this.period=period;if(input is not null) Apply(input); else Recalculate(); AcceptChanges(input); }
    private void Update() { HasInput=true; Recalculate(); Changed?.Invoke(this); }
    public void Apply(MonthlyInput input)
    {
        if(input.TeacherId != Teacher.Id) throw new InvalidOperationException("Aylık kayıt öğretmeni uyuşmuyor.");
        day=Text(input.DayHours); dyk=Text(input.DykHours); garnishment=Text(input.Garnishment);
        monthStart=input.MonthStart; monthEnd=input.MonthEnd; HasInput=true; Recalculate();
    }
    public void CopyPreview(PayrollRow source)
    {
        if(source.Teacher.Id!=Teacher.Id)throw new InvalidOperationException("Öğretmen uyuşmuyor.");
        day=source.day;dyk=source.dyk;garnishment=source.garnishment;monthStart=source.monthStart;monthEnd=source.monthEnd;
        Teacher=source.Teacher;SourceTeacher=source.SourceTeacher;SavedInput=source.SavedInput;
        acceptedDay=source.acceptedDay;acceptedDyk=source.acceptedDyk;acceptedGarnishment=source.acceptedGarnishment;
        acceptedStart=source.acceptedStart;acceptedEnd=source.acceptedEnd;
        HasInput=source.HasInput;Recalculate();
    }
    public MonthlyInput Read()
    {
        var input = new MonthlyInput(Teacher.Id,PeriodConstantsEditor.Number(day),PeriodConstantsEditor.Number(dyk),
            PeriodConstantsEditor.Number(garnishment),monthStart,monthEnd);
        if(input.DayHours<0 || input.DykHours<0 || input.Garnishment<0) throw new ArgumentException("Ders saati ve kesinti negatif olamaz.");
        return input;
    }
    public void Recalculate()
    {
        Result=null; Error="";SgkInformation=null;SgkError="";
        try
        {
            if(!HasInput) Error="Aylık ders bilgileri girilmedi.";
            else if(Teacher.ModuleId != ModuleId.TemelEgitim) Error="Bu modülün hesaplama çekirdeği henüz eklenmedi.";
            else { var i=Read();Result=new TeacherPayrollModule().Calculate(new(i.DayHours,i.DykHours,i.Garnishment),parameters()); }
        }
        catch(Exception ex) when(ex is ArgumentException or OverflowException) { Error=ex.Message; }
        // A calendar error must never erase a valid monetary result.
        if(HasInput && Teacher.ModuleId==ModuleId.TemelEgitim)
        {
            try
            {
                var i=Read();validate?.Invoke(i);
                var selected=period?.Invoke() ?? throw new ArgumentException("SGK bilgisi için dönem seçin.");
                SgkInformation=TeacherCalendar.CalculateMonth(selected.Year,selected.Month,
                    i.MonthStart ?? Teacher.StartDate,i.MonthEnd ?? Teacher.EndDate,i.DayHours,i.DykHours);
            }
            catch(Exception ex) when(ex is ArgumentException or OverflowException) { SgkError=ex.Message; }
        }
        foreach(var name in new[]{nameof(Teacher),nameof(FullName),nameof(IdentityNumber),nameof(Iban),nameof(School),nameof(OccupationCode),nameof(StartDate),nameof(EndDate),nameof(IbanCheck),nameof(HasChanges),nameof(DayHours),nameof(DykHours),nameof(Garnishment),nameof(MonthStart),nameof(MonthEnd),nameof(HasInput),nameof(Result),nameof(Error),nameof(MissingDays),nameof(TotalHours),nameof(SgkInformation),nameof(SgkError),nameof(MaximumDaysText),nameof(WorkingDaysText)})
            PropertyChanged?.Invoke(this,new(name));
    }
    private static string Text(decimal number)=>number.ToString(CultureInfo.GetCultureInfo("tr-TR"));
}
