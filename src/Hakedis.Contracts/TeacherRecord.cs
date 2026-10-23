namespace Hakedis.Contracts;

// Ortak veri tipi; her öğretmen yalnızca kendi modülüne aittir.
public sealed record TeacherRecord(Guid Id, string FullName, string IdentityNumber, string InsuranceNumber,
    string Iban, string School, string Branch, string OccupationCode, DateTime? StartDate, DateTime? EndDate,
    ModuleId ModuleId = ModuleId.TemelEgitim, string School2 = "", string Branch2 = "");
