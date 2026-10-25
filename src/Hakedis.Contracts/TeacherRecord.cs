namespace Hakedis.Contracts;

using System.Text.Json.Serialization;

// Tek kişi kartı; modül görünümleri ve aylık ödeme tercihi görev geçmişinden türetilir.
public sealed record TeacherRecord(Guid Id, string FullName, string IdentityNumber,
    string Iban, string School, string OccupationCode, DateTime? StartDate, DateTime? EndDate,
    ModuleId ModuleId = ModuleId.TemelEgitim,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] TeacherAssignmentHistory? Assignments=null);
