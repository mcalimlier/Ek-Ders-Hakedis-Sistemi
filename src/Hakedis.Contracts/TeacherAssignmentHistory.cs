namespace Hakedis.Contracts;

public sealed record SchoolTransfer(DateTime? Date,ModuleId FromModule,string FromSchool,ModuleId ToModule,string ToSchool);
public sealed record MonthlyBillingChoice(int Year,int Month,ModuleId Module);

// Yapısal eşitlik: JSON'dan tekrar okunan geçmiş, beklenen-sürüm denetimini yanlışlıkla bozmaz.
public sealed class TeacherAssignmentHistory : IEquatable<TeacherAssignmentHistory>
{
    public ModuleId HomeModuleId {get;}
    public string HomeSchool {get;}
    private readonly SchoolTransfer[] transfers;
    private readonly MonthlyBillingChoice[] choices;
    public SchoolTransfer[] Transfers=>transfers?.ToArray()!;
    public MonthlyBillingChoice[] Choices=>choices?.ToArray()!;
    public TeacherAssignmentHistory(ModuleId homeModuleId,string homeSchool,SchoolTransfer[] transfers,MonthlyBillingChoice[] choices)
    {HomeModuleId=homeModuleId;HomeSchool=homeSchool;this.transfers=transfers?.ToArray()!;this.choices=choices?.ToArray()!;}
    public TeacherAssignmentHistory Normalize(DateTime? employmentStart,DateTime? employmentEnd)
    {
        if(!Enum.IsDefined(HomeModuleId) || string.IsNullOrWhiteSpace(HomeSchool) || Transfers is null || Choices is null || Transfers.Length==0 ||
           Transfers.Any(t=>t is null || t.Date is null || t.Date.Value.Year<1900 || !Enum.IsDefined(t.FromModule) || !Enum.IsDefined(t.ToModule) ||
               string.IsNullOrWhiteSpace(t.FromSchool) || string.IsNullOrWhiteSpace(t.ToSchool)) || Choices.Any(c=>c is null))
            throw new ArgumentException("Okul/modül değişikliği için okul, geçiş tarihi ve hedef modülü eksiksiz girin.");
        var changes=Transfers.Select(t=>t with {Date=t.Date!.Value.Date,FromSchool=t.FromSchool.Trim(),ToSchool=t.ToSchool.Trim()}).ToArray();
        var currentModule=HomeModuleId;var currentSchool=HomeSchool.Trim();DateTime? previous=null;
        foreach(var t in changes)
        {
            if(t.Date<=previous || t.FromModule!=currentModule || t.FromSchool!=currentSchool ||
               (t.ToModule==t.FromModule && t.ToSchool==t.FromSchool) || t.Date<employmentStart?.Date || t.Date>employmentEnd?.Date)
                throw new ArgumentException("Görev geçmişinin tarihleri veya okul/modül sırası tutarsız. Geçiş tarihini kontrol edin.");
            previous=t.Date;currentModule=t.ToModule;currentSchool=t.ToSchool;
        }
        var result=new TeacherAssignmentHistory(HomeModuleId,HomeSchool.Trim(),changes,Choices.ToArray());
        if(Choices.GroupBy(c=>(c.Year,c.Month)).Any(g=>g.Count()>1) || Choices.Any(c=>c.Year is <1900 or >9999 || c.Month is <1 or >12 ||
            !TeacherRouting.Candidates(result,c.Year,c.Month).Contains(c.Module)))
            throw new ArgumentException("Görev geçmişiyle aylık bordro modülü tercihi uyuşmuyor. Kayıtlı tercihi kontrol edin.");
        return result;
    }
    public TeacherAssignmentHistory WithChoice(int year,int month,ModuleId module)
        =>new(HomeModuleId,HomeSchool,Transfers,Choices.Where(c=>c.Year!=year || c.Month!=month).Append(new(year,month,module)).OrderBy(c=>c.Year).ThenBy(c=>c.Month).ToArray());
    public TeacherAssignmentHistory WithSchool(ModuleId module,string previous,string school)
    {
        var home=HomeSchool;var changes=Transfers.ToArray();
        if(module==HomeModuleId && previous==home)home=school;
        for(var i=0;i<changes.Length;i++)
            changes[i]=changes[i] with {FromSchool=changes[i].FromModule==module && changes[i].FromSchool==previous?school:changes[i].FromSchool,
                ToSchool=changes[i].ToModule==module && changes[i].ToSchool==previous?school:changes[i].ToSchool};
        return new(HomeModuleId,home,changes,Choices);
    }
    public bool Equals(TeacherAssignmentHistory? other)=>other is not null && HomeModuleId==other.HomeModuleId && HomeSchool==other.HomeSchool &&
        transfers.SequenceEqual(other.transfers) && choices.SequenceEqual(other.choices);
    public override bool Equals(object? obj)=>obj is TeacherAssignmentHistory other && Equals(other);
    public override int GetHashCode(){var h=new HashCode();h.Add(HomeModuleId);h.Add(HomeSchool);foreach(var t in transfers)h.Add(t);foreach(var c in choices)h.Add(c);return h.ToHashCode();}
}

public static class TeacherRouting
{
    public static IReadOnlyList<ModuleId> Modules(TeacherRecord teacher)=>teacher.Assignments is { } h
        ?new[]{h.HomeModuleId}.Concat(h.Transfers.Select(t=>t.ToModule)).Distinct().ToArray():[teacher.ModuleId];
    public static IReadOnlyList<ModuleId> Candidates(TeacherRecord teacher,int year,int month)
        =>teacher.Assignments is { } h?Candidates(h,year,month):[teacher.ModuleId];
    public static IReadOnlyList<ModuleId> Candidates(TeacherAssignmentHistory h,int year,int month)
    {
        var first=new DateTime(year,month,1);var last=new DateTime(year,month,DateTime.DaysInMonth(year,month));
        var result=new List<ModuleId>();var current=h.HomeModuleId;var since=DateTime.MinValue;
        foreach(var t in h.Transfers)
        {
            var date=t.Date!.Value.Date;
            if(since<=last && date>first)result.Add(current);
            current=t.ToModule;since=date;
        }
        if(since<=last)result.Add(current);
        return result.Distinct().ToArray();
    }
    public static ModuleId? Choice(TeacherRecord teacher,int year,int month)=>teacher.Assignments?.Choices.SingleOrDefault(c=>c.Year==year && c.Month==month)?.Module;
    public static bool NeedsChoice(TeacherRecord teacher,int year,int month)=>Candidates(teacher,year,month).Count>1 && Choice(teacher,year,month) is null;
    public static bool Included(TeacherRecord teacher,ModuleId module,int year,int month)
        =>Candidates(teacher,year,month).Contains(module) && (Choice(teacher,year,month) is not ModuleId chosen || chosen==module);
    public static TeacherRecord Project(TeacherRecord teacher,ModuleId module)
    {
        if(!Modules(teacher).Contains(module))throw new InvalidOperationException("Öğretmenin bu modülde görev kaydı yok.");
        var school=teacher.Assignments is { } h?h.Transfers.LastOrDefault(t=>t.ToModule==module)?.ToSchool??h.HomeSchool:teacher.School;
        return teacher with {ModuleId=module,School=school};
    }
    public static TeacherRecord ProjectForPeriod(TeacherRecord teacher,ModuleId module,int year,int month)
    {
        var view=Project(teacher,module);if(teacher.Assignments is not { } h)return view;
        var first=new DateTime(year,month,1);var last=new DateTime(year,month,DateTime.DaysInMonth(year,month));
        var current=h.HomeModuleId;var school=h.HomeSchool;var since=DateTime.MinValue;string? found=null;
        foreach(var t in h.Transfers)
        {
            if(current==module && since<=last && t.Date!.Value.Date>first)found=school;
            current=t.ToModule;school=t.ToSchool;since=t.Date!.Value.Date;
        }
        if(current==module && since<=last)found=school;
        return view with {School=found??view.School};
    }
    public static bool Matches(TeacherRecord canonical,TeacherRecord projection,ModuleId module)
    {
        if(!Modules(canonical).Contains(module))return false;
        var view=Project(canonical,module);
        var known=canonical.Assignments is { } h && (h.HomeModuleId==module && h.HomeSchool==projection.School ||
            h.Transfers.Any(t=>t.ToModule==module && t.ToSchool==projection.School));
        return view==projection || (known && view with {School=projection.School}==projection);
    }
    public static TeacherRecord Canonicalize(TeacherRecord original,TeacherRecord replacement,TeacherRecord? expectedProjection=null)
    {
        var history=replacement.Assignments??original.Assignments;
        if(history is not null && history.HomeModuleId!=original.ModuleId)throw new InvalidOperationException("Kişi kartının kayıt modülü değiştirilemez; görev geçmişini kullanın.");
        var oldSchool=expectedProjection?.School??Project(original,replacement.ModuleId).School;
        if(history is not null && replacement.School!=oldSchool)
            history=history.WithSchool(replacement.ModuleId,oldSchool,replacement.School);
        return replacement with {ModuleId=original.ModuleId,School=history?.HomeSchool??replacement.School,Assignments=history};
    }
}
