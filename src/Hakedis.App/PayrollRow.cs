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
    public TeacherRecord Teacher { get; }
    public bool HasInput { get; private set; }
    public TeacherResult? Result { get; private set; }
    public string Error { get; private set; } = "";
    public string MissingDays => "Açıklama bekleniyor";
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
    public PayrollRow(TeacherRecord teacher, MonthlyInput? input, Func<TeacherParameters> parameters, Action<MonthlyInput>? validate=null)
    { Teacher=teacher; this.parameters=parameters; this.validate=validate;if(input is not null) Apply(input); else Recalculate(); }
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
        HasInput=source.HasInput;Recalculate();
    }
    public MonthlyInput Read()
    {
        if(monthEnd?.Date < monthStart?.Date) throw new ArgumentException("AY İÇİNDE İŞTEN AYRILIŞ TARİHİ girişten önce olamaz.");
        var input = new MonthlyInput(Teacher.Id,PeriodConstantsEditor.Number(day),PeriodConstantsEditor.Number(dyk),
            PeriodConstantsEditor.Number(garnishment),monthStart,monthEnd);
        if(input.DayHours<0 || input.DykHours<0 || input.Garnishment<0) throw new ArgumentException("Ders saati ve kesinti negatif olamaz.");
        return input;
    }
    public void Recalculate()
    {
        Result=null; Error="";
        try
        {
            if(!HasInput) Error="Aylık ders bilgileri girilmedi.";
            else if(Teacher.ModuleId != ModuleId.TemelEgitim) Error="Bu modülün hesaplama çekirdeği henüz eklenmedi.";
            else { var i=Read();validate?.Invoke(i); Result=new TeacherPayrollModule().Calculate(new(i.DayHours,i.DykHours,i.Garnishment),parameters()); }
        }
        catch(Exception ex) when(ex is ArgumentException or OverflowException) { Error=ex.Message; }
        foreach(var name in new[]{nameof(DayHours),nameof(DykHours),nameof(Garnishment),nameof(MonthStart),nameof(MonthEnd),nameof(HasInput),nameof(Result),nameof(Error),nameof(MissingDays),nameof(TotalHours)})
            PropertyChanged?.Invoke(this,new(name));
    }
    private static string Text(decimal number)=>number.ToString(CultureInfo.GetCultureInfo("tr-TR"));
}
