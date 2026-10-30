using System.ComponentModel;
using System.Globalization;
using Hakedis.Contracts;
using Hakedis.Storage;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.App;

public sealed class PayrollRow : INotifyPropertyChanged
{
    private string day = "0", dyk = "0", garnishment = "0";
    private readonly Func<TeacherParameters> parameters;
    private readonly Action<MonthlyInput>? validate;
    private readonly Func<(int Year,int Month)>? period;
    public TeacherRecord Teacher { get; private set; }
    public TeacherRecord SourceTeacher { get; private set; }
    public MonthlyInput? SavedInput { get; private set; }
    private string acceptedDay="0", acceptedDyk="0", acceptedGarnishment="0";
    public bool HasChanges => Teacher!=SourceTeacher || day!=acceptedDay || dyk!=acceptedDyk ||
        garnishment!=acceptedGarnishment || previousSchoolHours!=acceptedPrevious || newSchoolHours!=acceptedNew;
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
        if(SourceTeacher!=saved || replacePreview)
        {
            var wasSplit=HasSchoolHourSplit;var total=DayHours;
            Teacher=SourceTeacher=saved;
            if(!wasSplit && HasSchoolHourSplit && HasInput){previousSchoolHours=total;newSchoolHours="";}
            if(wasSplit && !HasSchoolHourSplit)day=total;
            Recalculate();
        }}
    public void AcceptInput(MonthlyInput persisted)
    {
        var draft=Teacher;Teacher=SourceTeacher;AcceptChanges(persisted);Teacher=draft;Recalculate();
    }
    public void AcceptChanges(MonthlyInput? persisted=null)
    {
        SourceTeacher=Teacher;SavedInput=persisted;
        acceptedDay=day;acceptedDyk=dyk;acceptedGarnishment=garnishment;acceptedPrevious=previousSchoolHours;acceptedNew=newSchoolHours;
Recalculate();
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
    public string IbanCheck=>string.IsNullOrEmpty(Teacher.Iban)?"-":TeacherDirectory.IsBankAccountFormatValid(Teacher.Iban.Replace(" ", "").ToUpperInvariant())?"BİÇİM UYGUN":"HATALI";
    public string TotalHours
    {
        get {try {var i=Read();return Text(i.DayHours+i.DykHours);}catch(Exception ex) when(ex is ArgumentException or OverflowException){return "—";}}
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<PayrollRow>? Changed;
    private string previousSchoolHours="", newSchoolHours="", acceptedPrevious="", acceptedNew="";
    private SchoolTransfer[] PeriodTransfers=>period is null?[]:Teacher.Assignments?.Transfers.Where(t=>t.Date?.Year==period().Year && t.Date?.Month==period().Month).ToArray()??[];
    public bool HasSchoolHourSplit=>PeriodTransfers.Length>0;
    public bool HasSingleSchoolHours=>!HasSchoolHourSplit;
    public string PreviousSchoolLabel=>PeriodTransfers.FirstOrDefault()?.FromSchool+" · Gündüz ders saati";
    public string NewSchoolLabel=>PeriodTransfers.LastOrDefault()?.ToSchool+" · Gündüz ders saati";
    public string PreviousSchoolDayHours {get=>previousSchoolHours;set{if(previousSchoolHours==value)return;previousSchoolHours=value;Update();}}
    public string NewSchoolDayHours {get=>newSchoolHours;set{if(newSchoolHours==value)return;newSchoolHours=value;Update();}}
    public string SchoolHoursWarning=>!HasSchoolHourSplit?"":string.Join(" ",new[]{
        string.IsNullOrWhiteSpace(previousSchoolHours)?PreviousSchoolLabel+" girilmedi; 0 kabul edildi.":"",
        string.IsNullOrWhiteSpace(newSchoolHours)?NewSchoolLabel+" girilmedi; 0 kabul edildi.":""}.Where(x=>x!=""));
    public string DayHours { get {if(!HasSchoolHourSplit)return day;try{return Text((OptionalHours(previousSchoolHours)??0)+(OptionalHours(newSchoolHours)??0));}catch(ArgumentException){return "—";}} set {if(HasSchoolHourSplit){PreviousSchoolDayHours=value;return;} if(day == value) return; day=value; Update(); } }
    private static decimal? OptionalHours(string text)=>string.IsNullOrWhiteSpace(text)?null:PeriodConstantsEditor.Number(text);

    public string DykHours { get => dyk; set { if(dyk == value) return; dyk=value; Update(); } }
    public string Garnishment { get => garnishment; set { if(garnishment == value) return; garnishment=value; Update(); } }
    public PayrollRow(TeacherRecord teacher, MonthlyInput? input, Func<TeacherParameters> parameters, Action<MonthlyInput>? validate=null,
        Func<(int Year,int Month)>? period=null)
    { Teacher=SourceTeacher=teacher; this.parameters=parameters; this.validate=validate;this.period=period;if(input is not null) Apply(input); else Recalculate(); AcceptChanges(input); }
    private void Update() { HasInput=true; Recalculate(); Changed?.Invoke(this); }
    public void Apply(MonthlyInput input)
    {
        if(input.TeacherId != Teacher.Id) throw new InvalidOperationException("Aylık kayıt öğretmeni uyuşmuyor.");
        previousSchoolHours=input.PreviousSchoolDayHours is decimal previous?Text(previous):"";
        newSchoolHours=input.NewSchoolDayHours is decimal next?Text(next):"";
        if(HasSchoolHourSplit && !input.SchoolHoursSeparated && input.PreviousSchoolDayHours is null && input.NewSchoolDayHours is null)previousSchoolHours=Text(input.DayHours);
        day=Text(input.DayHours); dyk=Text(input.DykHours); garnishment=Text(input.Garnishment);
        HasInput=true; Recalculate();
    }
    public void CopyPreview(PayrollRow source)
    {
        if(source.Teacher.Id!=Teacher.Id)throw new InvalidOperationException("Öğretmen uyuşmuyor.");
        previousSchoolHours=source.previousSchoolHours;newSchoolHours=source.newSchoolHours;acceptedPrevious=source.acceptedPrevious;acceptedNew=source.acceptedNew;
        day=source.day;dyk=source.dyk;garnishment=source.garnishment;
        Teacher=source.Teacher;SourceTeacher=source.SourceTeacher;SavedInput=source.SavedInput;
        acceptedDay=source.acceptedDay;acceptedDyk=source.acceptedDyk;acceptedGarnishment=source.acceptedGarnishment;
        HasInput=source.HasInput;Recalculate();
    }
    public MonthlyInput Read()
    {
        var input = new MonthlyInput(Teacher.Id,PeriodConstantsEditor.Number(DayHours),PeriodConstantsEditor.Number(dyk),
            PeriodConstantsEditor.Number(garnishment),HasSchoolHourSplit?OptionalHours(previousSchoolHours):null,HasSchoolHourSplit?OptionalHours(newSchoolHours):null,HasSchoolHourSplit);
        input.ValidateSchoolHours();
        if(input.DayHours<0 || input.DykHours<0 || input.Garnishment<0) throw new ArgumentException("Ders saati ve kesinti negatif olamaz.");
        return input;
    }
    public void Recalculate()
    {
        Result=null; Error="";SgkInformation=null;SgkError="";
        try
        {
            if(!HasInput) Error="Aylık ders bilgileri girilmedi.";
            else if(!PayrollCores.IsAvailable(Teacher.ModuleId)) Error="Bu modülün hesaplama çekirdeği henüz eklenmedi.";
            else { var i=Read();Result=PayrollCores.Calculate(Teacher.ModuleId,new(i.DayHours,i.DykHours,i.Garnishment),parameters()); }
        }
        catch(Exception ex) when(ex is ArgumentException or OverflowException) { Error=ex.Message; }
        // A calendar error must never erase a valid monetary result.
        if(HasInput && PayrollCores.IsAvailable(Teacher.ModuleId))
        {
            try
            {
                var i=Read();validate?.Invoke(i);
                var selected=period?.Invoke() ?? throw new ArgumentException("SGK bilgisi için dönem seçin.");
                SgkInformation=PayrollCores.Calendar(Teacher.ModuleId,selected.Year,selected.Month,
                    Teacher.StartDate,Teacher.EndDate,i.DayHours,i.DykHours);
            }
            catch(Exception ex) when(ex is ArgumentException or OverflowException) { SgkError=ex.Message; }
        }
        foreach(var name in new[]{nameof(HasSchoolHourSplit),nameof(HasSingleSchoolHours),nameof(PreviousSchoolLabel),nameof(NewSchoolLabel),nameof(PreviousSchoolDayHours),nameof(NewSchoolDayHours),nameof(SchoolHoursWarning),nameof(Teacher),nameof(FullName),nameof(IdentityNumber),nameof(Iban),nameof(School),nameof(OccupationCode),nameof(StartDate),nameof(EndDate),nameof(IbanCheck),nameof(HasChanges),nameof(DayHours),nameof(DykHours),nameof(Garnishment),nameof(HasInput),nameof(Result),nameof(Error),nameof(MissingDays),nameof(TotalHours),nameof(SgkInformation),nameof(SgkError),nameof(MaximumDaysText),nameof(WorkingDaysText)})
            PropertyChanged?.Invoke(this,new(name));
    }
    private static string Text(decimal number)=>number.ToString(CultureInfo.GetCultureInfo("tr-TR"));
}
