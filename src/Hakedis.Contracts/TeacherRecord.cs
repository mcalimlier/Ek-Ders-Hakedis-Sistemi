namespace Hakedis.Contracts;

// Öğretmenin modül sahipliği değiştirilemez.
public sealed record TeacherRecord(Guid Id, string FullName, string IdentityNumber,
    string Iban, string School, string OccupationCode, DateTime? StartDate, DateTime? EndDate,
    ModuleId ModuleId = ModuleId.TemelEgitim);
