namespace Hakedis.TemelEgitim.Core;

public sealed record CalendarResult(int CalendarDays, int PremiumDays, int MissingDays);

// Tarih aralığı için matematik yardımcısı. SGK eksik gün uygulama kuralı değildir;
// kullanıcı açıklaması gelene kadar üretim hakedişinde eksik gün olarak kullanılmaz.
public static class TeacherCalendar
{
    public static CalendarResult Calculate(DateTime start, DateTime end, DateTime? employmentStart,
        DateTime? employmentEnd, decimal dayHours, decimal dykHours)
    {
        if (end.Date < start.Date || employmentEnd?.Date < employmentStart?.Date || dayHours < 0 || dykHours < 0)
            throw new ArgumentException("Tarih aralığı veya ders saati geçersiz.");
        var first = employmentStart.HasValue && employmentStart.Value.Date > start.Date ? employmentStart.Value.Date : start.Date;
        var last = employmentEnd.HasValue && employmentEnd.Value.Date < end.Date ? employmentEnd.Value.Date : end.Date;
        var capacity = Math.Max(0, (last - first).Days + 1);
        var premium = checked((int)decimal.Ceiling((dayHours + dykHours) / 7.5m));
        if (premium > capacity) throw new ArgumentException("Çalışma Gün Sayısı tarih aralığındaki gün sayısını aşıyor.");
        return new(capacity, premium, capacity - premium);
    }
}
