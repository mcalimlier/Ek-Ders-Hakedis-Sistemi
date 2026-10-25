namespace Hakedis.EmekliHalkEgitim.Core;

public sealed record EmekliHalkEgitimCalendarResult(int CalendarDays,int PremiumDays,int MissingDays);

public static class EmekliHalkEgitimCalendar
{
    public const string RuleVersion="emekli-halk-egitim.sgk-yan-bilgi.v1";
    public static EmekliHalkEgitimCalendarResult CalculateMonth(int year,int month,DateTime? start,DateTime? end,decimal dayHours,decimal dykHours)
    {
        var first=new DateTime(year,month,1);var last=new DateTime(year,month,DateTime.DaysInMonth(year,month));
        if(end?.Date<start?.Date || dayHours<0 || dykHours<0)throw new ArgumentException("Tarih aralığı veya ders saati geçersiz.");
        if(start.HasValue && start.Value.Date>first)first=start.Value.Date;
        if(end.HasValue && end.Value.Date<last)last=end.Value.Date;
        var capacity=Math.Max(0,(last-first).Days+1);
        var premium=checked((int)decimal.Ceiling((dayHours+dykHours)/7.5m));
        if(premium>capacity)throw new ArgumentException("Çalışma Gün Sayısı tarih aralığındaki gün sayısını aşıyor.");
        return new(capacity,premium,capacity-premium);
    }
}
