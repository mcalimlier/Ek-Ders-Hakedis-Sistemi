using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using Hakedis.Contracts;
using Hakedis.Storage;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.App;

public sealed record MonthOption(int Number,string Name);

public sealed class PeriodWorkspace : INotifyPropertyChanged
{
    private readonly string teacherPath;
    private readonly MonthlyPayrollStore store;
    private ModuleDescriptor selectedModule;
    private int year=2026, month=9;
    private TeacherRecord? teacher;
    private PayrollRow? selectedRow;
    private PayrollRow? hybridRow;
    public PayrollRow? SelectedHybridRow
    {
        get=>hybridRow;
        set{hybridRow=value is not null && Rows.Contains(value)?value:null;Notify();}
    }
    public bool HasHybridSelection=>hybridRow is not null && !IsArchiveEditing;
    public bool HasHybridChanges=>Rows.Any(r=>r.HasChanges);
    public string HybridChangeText=>HasHybridChanges?$"{Rows.Count(r=>r.HasChanges)} satırda kaydedilmemiş değişiklik var.":"Tüm değişiklikler kaydedildi.";
    private bool loading,editing=true,completed;
    public bool IsArchiveEditing {get;private set;}
    public bool IsNormalWork=>!IsArchiveEditing;
    public bool HasActiveWork=>!completed && Rows.Any(r=>r.HasInput || r.HasChanges);
    public event Action? PaymentCreated;
    public void ClearWork()
    {
        IsArchiveEditing=false;completed=false;teacher=null;selectedRow=hybridRow=null;Rows.Clear();Teachers.Clear();
        Status="Çalışma temizlendi. Kayıtlı öğretmenler ve aylık dosyalar korunur. Kayıtları yenile ile tekrar açabilirsiniz.";Notify();
    }
    public void LoadArchive(ArchivedPayment archive)
    {
        IsArchiveEditing=true;loading=true;completed=false;teacher=null;selectedRow=hybridRow=null;Rows.Clear();Teachers.Clear();
        var data=archive.Document.Data;year=data.Year;month=data.Month;selectedModule=Modules.Single(m=>m.Id==archive.Document.ModuleId);
        SetConstants(data.Constants);
        foreach(var item in data.Rows)
        {
            Teachers.Add(item.Teacher);var row=new PayrollRow(item.Teacher,item.Input,()=>Constants.Parameters(),ValidateInput,()=> (year,month));
            row.Sequence=Rows.Count+1;row.Changed+=RowEdited;Rows.Add(row);
        }
        loading=false;Status="Geçmiş hakediş düzenleniyor. Değiştir/Kaydet ile onaylayın veya Vazgeç ile geri dönün.";Notify();
    }
    public PaymentSnapshot EditedSnapshot(PaymentSnapshot original)
    {
        if(!IsArchiveEditing)throw new InvalidOperationException("Geçmiş düzenleme açık değil.");
        var result=new List<PaymentRow>();
        foreach(var row in Rows)
        {
            var record=TeacherDirectory.Normalize(row.Teacher);var input=row.Read();row.Recalculate();
            if(string.IsNullOrEmpty(record.Iban))throw new ArgumentException(record.FullName+": IBAN zorunludur.");
            if(!OccupationCatalog.Contains(record.OccupationCode))throw new ArgumentException("Listeden SGK MESLEK KODU seçin.");
            result.Add(new(record,input,row.Result??throw new ArgumentException(row.Error),row.SgkInformation,row.SgkError));
        }
        return original with {Rows=result.ToArray()};
    }
    private void RequireNormal(){if(IsArchiveEditing)throw new InvalidOperationException("Geçmiş kayıt için Değiştir/Kaydet veya Vazgeç kullanın.");}
    private Action? constantsHandler;
    public IReadOnlyList<ModuleDescriptor> Modules { get; } = new ModuleRegistry([new TeacherPayrollModule()]).Catalog;
    public IReadOnlyList<MonthOption> Months { get; } = Enumerable.Range(1,12).Select(i=>new MonthOption(i,CultureInfo.GetCultureInfo("tr-TR").DateTimeFormat.GetMonthName(i))).ToArray();
    public IReadOnlyList<int> Years { get; } = Enumerable.Range(1900,201).ToArray();
    public ObservableCollection<TeacherRecord> Teachers { get; }=[];
    public ObservableCollection<PayrollRow> Rows { get; }=[];
    public PeriodConstantsEditor Constants { get; private set; } = new(PeriodValues.Source);
    public string Status { get; private set; }="";
    public string PeriodText => $"{Months[month-1].Name}-{year}";
    public bool IsAvailable => SelectedModule.IsAvailable;
    public bool IsEditing => editing;
    public bool HasTeacher => selectedRow is not null;
    public string SgkSummary => selectedRow is null ? "SGK yan bilgisi için öğretmen seçin." :
        selectedRow.SgkError!="" ? "SGK yan bilgisi: "+selectedRow.SgkError :
        $"SGK yan bilgisi · En fazla gün: {selectedRow.MaximumDaysText} · Çalışma Gün Sayısı: {selectedRow.WorkingDaysText} · Eksik Gün Sayısı: {selectedRow.MissingDays}";
    public ModuleDescriptor SelectedModule { get=>selectedModule; set { if(value is null || value.Id==selectedModule.Id)return; RequireNormal();selectedModule=value;Reload(); } }
    public int Year { get=>year; set { if(value is <1900 or >2100 || value==year)return;RequireNormal();year=value;Reload(); } }
    public int Month { get=>month;set { if(value is <1 or >12 || value==month)return;RequireNormal();month=value;Reload(); } }
    public TeacherRecord? SelectedTeacher
    {
        get=>teacher;
        set
        {
            if(value is not null && value.ModuleId != SelectedModule.Id)throw new InvalidOperationException("Başka modülün öğretmeni seçilemez.");
            teacher=value;selectedRow=Rows.SingleOrDefault(r=>r.Teacher.Id==value?.Id);editing=selectedRow?.HasInput!=true;
            Notify();
        }
    }
    public string DayHours { get=>selectedRow?.DayHours??"0";set { if(selectedRow is not null)selectedRow.DayHours=value; } }
    public string DykHours { get=>selectedRow?.DykHours??"0";set { if(selectedRow is not null)selectedRow.DykHours=value; } }
    public string Garnishment { get=>selectedRow?.Garnishment??"0";set { if(selectedRow is not null)selectedRow.Garnishment=value; } }
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<PeriodWorkspace,PayrollRow>? RowChanged;
    public event Action<PeriodWorkspace>? Saved;
    public event Action<PeriodWorkspace>? ConstantsChanged;
    public PeriodWorkspace(string teacherPath)
    {
        this.teacherPath=teacherPath;new PayrollEditStore(teacherPath).Recover();store=new(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(teacherPath))!);
        selectedModule=Modules.Single(m=>m.Id==ModuleId.TemelEgitim);Reload();
    }
    public void Reload()
    {
        RequireNormal();completed=false;loading=true;hybridRow=null;teacher=null;selectedRow=null;Teachers.Clear();Rows.Clear();
        try
        {
            var document=store.Read(SelectedModule.Id,year,month);
            SetConstants(document.Constants);
            foreach(var record in new TeacherDirectory(teacherPath,SelectedModule.Id).Read())
            {
                Teachers.Add(record);var row=new PayrollRow(record,document.Entries.SingleOrDefault(e=>e.TeacherId==record.Id),()=>Constants.Parameters(),ValidateInput,()=> (year,month));
                row.Sequence=Rows.Count+1;row.Changed+=RowEdited;Rows.Add(row);
            }
            Status=$"{PeriodText} · {SelectedModule.Name}. Kayıtlı aylık ders bilgileri yüklendi.";
        }
        catch(Exception ex) when(ex is System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException or ArgumentException)
        { Status=ex.Message; }
        finally { loading=false;editing=true;Notify(); }
    }
    public void RefreshTeachers()
    {
        if(IsArchiveEditing)return;
        var id=teacher?.Id;var hybridId=hybridRow?.Teacher.Id;
        var records=new TeacherDirectory(teacherPath,SelectedModule.Id).Read();
        var old=Rows.ToDictionary(r=>r.Teacher.Id);Teachers.Clear();Rows.Clear();
        foreach(var t in records)
        {
            Teachers.Add(t);
            // Preserve even incomplete text while teacher catalog refreshes.
            PayrollRow r;
            if(old.TryGetValue(t.Id,out var existing)) {r=existing;r.RefreshTeacher(t);}
            else { r=new(t,null,()=>Constants.Parameters(),ValidateInput,()=> (year,month));if(existing is not null)r.CopyPreview(existing);r.Changed+=RowEdited; }
            r.Sequence=Rows.Count+1;Rows.Add(r);
        }
        hybridRow=Rows.SingleOrDefault(r=>r.Teacher.Id==hybridId);teacher=Teachers.SingleOrDefault(t=>t.Id==id);selectedRow=Rows.SingleOrDefault(r=>r.Teacher.Id==id);Notify();
    }
    private void RowEdited(PayrollRow row)
    {
        completed=false;
        Status=row.Result is null && row.HasInput?row.Teacher.FullName+": "+row.Error:
            "Değişiklikler önizlemeye yansıdı. Kalıcı kayıt için Kaydet veya Hakediş oluştur düğmesini kullanın.";
        Notify();if(!loading)RowChanged?.Invoke(this,row);
    }
    public void ApplyPreview(PayrollRow source)
    {
        var target=Rows.SingleOrDefault(r=>r.Teacher.Id==source.Teacher.Id);if(target is null)return;
        // Text is deliberately transferred without parsing: invalid edits invalidate both previews.
        target.CopyPreview(source);
        Status=target.Result is null && target.HasInput?target.Teacher.FullName+": "+target.Error:"Değişiklikler canlı önizlemeye yansıdı; henüz kaydedilmedi.";
        Notify();
    }
    public void SetConstants(PeriodValues values)
    {
        InstallConstants(new(values));
    }
    private void InstallConstants(PeriodConstantsEditor editor)
    {
        if(constantsHandler is not null)Constants.Changed-=constantsHandler;
        Constants=editor;
        constantsHandler=()=>
        {
            foreach(var row in Rows)row.Recalculate();
            var error=Rows.FirstOrDefault(r=>r.HasInput && r.Result is null);
            Status=error is null?"Dönem sabitleri değişti. Kalıcı kayıt için dönem sabitlerini kaydedin.":error.Error;
            Notify();if(!loading)ConstantsChanged?.Invoke(this);
        };
        Constants.Changed+=constantsHandler;
    }
    public void AcceptConstants(PeriodValues values)
    {SetConstants(values);foreach(var row in Rows)row.Recalculate();Notify();}
    public void AdoptConstants(PeriodConstantsEditor editor)
    {
        if(ReferenceEquals(Constants,editor))return;
        InstallConstants(editor);
        foreach(var row in Rows)row.Recalculate();Notify();
    }
    public void AcceptSavedSelection()
    {if(selectedRow is not null) {editing=false;Notify();}}
    public void BeginEdit() {if(selectedRow is null)throw new InvalidOperationException("Önce kayıtlı öğretmen seçin.");editing=true;Notify();}
    public void Save(bool update)
    {
        new PayrollEditStore(teacherPath).Recover();
        RequireNormal();var row=selectedRow??throw new InvalidOperationException("Önce kayıtlı öğretmen seçin.");
        var input=row.Read();ValidateInput(input);
        var teachers=new TeacherDirectory(teacherPath,SelectedModule.Id).Read();
        if(!teachers.Any(t=>t.Id==input.TeacherId))throw new InvalidOperationException("Öğretmen kaydı artık bulunmuyor.");
        var constants=ValidatedConstants();
        if(IsAvailable)_=new TeacherPayrollModule().Calculate(new(input.DayHours,input.DykHours,input.Garnishment),Constants.Parameters());
        store.Update(SelectedModule.Id,year,month,existing=>
        {
            if(!update && existing.Entries.Any(e=>e.TeacherId==input.TeacherId))throw new InvalidOperationException("Bu dönemde kayıt var. Düzenle düğmesini kullanın.");
            return new(year,month,constants,existing.Entries.Where(e=>e.TeacherId!=input.TeacherId).Append(input).ToArray());
        });
        row.Apply(input);row.AcceptInput(input);RowChanged?.Invoke(this,row);editing=false;Status=$"{row.Teacher.FullName} · {PeriodText} kaydedildi.";Notify();Saved?.Invoke(this);
    }
    private PeriodValues ValidatedConstants()
    {
        var values=Constants.Read();
        // Only the matching core validates its parameters; never run another module through Temel Eğitim.
        if(SelectedModule.Id==ModuleId.TemelEgitim) _=new TeacherPayrollModule().Calculate(new(0,0,0),Constants.Parameters());
        return values;
    }
    public void SaveConstants()
    {
        new PayrollEditStore(teacherPath).Recover();
        var constants=ValidatedConstants();
        new CommonConstantsStore(System.IO.Path.GetDirectoryName(teacherPath)!).Save(constants);Status="Ortak dönem sabitleri kaydedildi.";Notify();
    }
    private static void ValidateInput(MonthlyInput input)
    {
        if(input.DayHours<0 || input.DykHours<0 || input.Garnishment<0)throw new ArgumentException("Ders saati ve kesinti negatif olamaz.");
    }
    public void SaveHybridChanges()
    {
        RequireNormal();var changed=Rows.Where(r=>r.HasChanges).ToArray();if(changed.Length==0)return;
        var values=ValidatedConstants();
        foreach(var row in changed)
        {
            _=TeacherDirectory.Normalize(row.Teacher);
            if(!OccupationCatalog.Contains(row.OccupationCode))throw new ArgumentException("SGK MESLEK KODU için listeden kod ve karşılığını seçin.");
            if(row.HasInput){ValidateInput(row.Read());row.Recalculate();
                if(IsAvailable && row.Result is null)throw new ArgumentException(row.FullName+": "+row.Error);}
        }
        var inputs=changed.Where(r=>r.HasInput).Select(r=>r.Read()).ToArray();
        new PayrollEditStore(teacherPath).Save(SelectedModule.Id,year,month,
            changed.Select(r=>r.SourceTeacher).ToArray(),changed.Select(r=>r.Teacher).ToArray(),inputs,values,
            changed.Where(r=>r.HasInput).ToDictionary(r=>r.Teacher.Id,r=>r.SavedInput));
        var persisted=new TeacherDirectory(teacherPath,SelectedModule.Id).Read();
        foreach(var row in changed)
        {row.RefreshTeacher(persisted.Single(t=>t.Id==row.Teacher.Id));row.AcceptChanges(row.HasInput?row.Read():null);RowChanged?.Invoke(this,row);}
        RefreshTeachers();Status="Kayıtlar güncellendi.";Notify();Saved?.Invoke(this);
    }
    public void DeleteHybridTeacher()
    {
        RequireNormal();var row=hybridRow??throw new InvalidOperationException("Önce öğretmen seçin.");
        new TeacherDirectory(teacherPath,SelectedModule.Id).Delete(row.Teacher.Id);
        RefreshTeachers();Status="Öğretmen kaydı silindi. Geçmiş hakedişler korunur.";Notify();Saved?.Invoke(this);
    }
    public void CreatePayment()
    {
        new PayrollEditStore(teacherPath).Recover();
        RequireNormal();if(!IsAvailable)throw new NotSupportedException("Seçilen modülün hesaplama çekirdeği henüz eklenmedi.");
        if(Rows.Any(r=>r.Teacher!=r.SourceTeacher))throw new InvalidOperationException("Öğretmen değişiklikleri için önce Değişiklikleri kaydet düğmesini kullanın.");
        var rows=Rows.Where(r=>r.HasInput).ToArray();
        if(rows.Length==0)throw new ArgumentException("Önce aylık ders bilgilerini girin.");
        var validTeachers=new TeacherDirectory(teacherPath,SelectedModule.Id).Read();
        foreach(var row in rows)
        {
            row.Recalculate();if(row.Result is null)throw new ArgumentException(row.Teacher.FullName+": "+row.Error);
            if(!validTeachers.Contains(row.Teacher))throw new InvalidOperationException("Öğretmen bilgileri değişmiş. Listeyi yenileyin.");
            ValidateInput(row.Read());
            if(string.IsNullOrWhiteSpace(row.Teacher.Iban))throw new ArgumentException(row.Teacher.FullName+": "+ExcelHeadings.Iban);
        }
        var values=ValidatedConstants();var inputs=rows.Select(r=>r.Read()).ToArray();
        // Read first so a corrupted monthly file cannot be replaced by a new calculation.
        store.Update(SelectedModule.Id,year,month,current=>new(year,month,values,current.Entries.Where(e=>inputs.All(i=>i.TeacherId!=e.TeacherId)).Concat(inputs).ToArray()));
        var warningCount=rows.Count(r=>r.SgkError!="");
        var snapshot=new PaymentSnapshot(year,month,values,rows.Select(r=>new PaymentRow(r.Teacher,r.Read(),r.Result!,r.SgkInformation,r.SgkError)).ToArray(),
            "Eksik Gün Sayısı SGK girişi için yan bilgidir; parasal hakedişe etki etmez."+(warningCount>0?$" {warningCount} kayıtta SGK bilgisi kontrol edilmelidir.":""),TeacherCalendar.RuleVersion);
        var archiveStore=new PaymentArchiveStore(System.IO.Path.GetDirectoryName(teacherPath)!);
        snapshot=snapshot with {Details=new ReportDetailsStore(System.IO.Path.GetDirectoryName(teacherPath)!).Read(),Movement=archiveStore.Movement(SelectedModule.Id,year,month,snapshot.Rows)};
        var path=System.IO.Path.Combine(store.Folder(SelectedModule.Id),$"hakediş-{year:D4}-{month:D2}-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json");
        new JsonDraftStore().Save(path,new DraftDocument<PaymentSnapshot>(1,SelectedModule.Id,new TeacherPayrollModule().RuleVersion,DateTimeOffset.UtcNow,snapshot));
        Status=$"{PeriodText} · {rows.Length} öğretmen için hakediş kaydı oluşturuldu."+
            (warningCount>0?$" {warningCount} kayıtta SGK yan bilgisi kontrol edilmelidir; hakediş tutarı bu bilgiden etkilenmez.":" SGK yan bilgileri de kaydedildi.");
        foreach(var row in rows){row.AcceptChanges(row.Read());RowChanged?.Invoke(this,row);}
        completed=true;Notify();Saved?.Invoke(this);PaymentCreated?.Invoke();
    }
    private bool Invalid=>!IsAvailable || Rows.Any(r=>r.HasInput && r.Result is null);
    private IEnumerable<TeacherResult> Results=>Rows.Where(r=>r.HasInput && r.Result is not null).Select(r=>r.Result!);
    public decimal NetTotal=>Results.Sum(r=>r.Net);
    private string Total(Func<TeacherResult,decimal> selector)=>Invalid?"—":Results.Sum(selector).ToString("N2",CultureInfo.GetCultureInfo("tr-TR"))+" TL";
    public string GrossText=>Total(r=>r.Gross);
    public string AccrualText=>Total(r=>r.Accrual);
    public string NetText=>Total(r=>r.Net);
    public string EmployeeText=>Total(r=>r.EmployeePremium);
    public string EmployerText=>Total(r=>r.EmployerPremium);
    public string DeductionsText=>Total(r=>r.Deductions);
    public string CountText=>$"{Rows.Count} öğretmen · {Rows.Count(r=>r.HasInput)} aylık giriş";
    public void ShowError(string text){Status=text;Notify();}
    private void Notify()
    {
        foreach(var name in new[]{nameof(IsArchiveEditing),nameof(IsNormalWork),nameof(HasActiveWork),nameof(SelectedHybridRow),nameof(HasHybridSelection),nameof(HasHybridChanges),nameof(HybridChangeText),nameof(SelectedModule),nameof(Year),nameof(Month),nameof(PeriodText),nameof(Constants),nameof(SelectedTeacher),nameof(DayHours),nameof(DykHours),nameof(Garnishment),nameof(IsEditing),nameof(HasTeacher),nameof(IsAvailable),nameof(Status),nameof(GrossText),nameof(AccrualText),nameof(NetText),nameof(EmployeeText),nameof(EmployerText),nameof(DeductionsText),nameof(CountText),nameof(NetTotal),nameof(SgkSummary)})
            PropertyChanged?.Invoke(this,new(name));
    }
}
