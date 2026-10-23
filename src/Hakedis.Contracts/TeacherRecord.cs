namespace Hakedis.Contracts;

// Ortak kişi kaydı hesaplama çekirdeğinden bağımsızdır.
public sealed record TeacherRecord(Guid Id, string FullName, string IdentityNumber, string InsuranceNumber,
    string Iban, string School, string Branch, string OccupationCode, DateTime? StartDate, DateTime? EndDate);
