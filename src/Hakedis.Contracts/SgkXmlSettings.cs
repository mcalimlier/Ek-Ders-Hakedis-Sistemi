namespace Hakedis.Contracts;

public sealed record SgkWorkplace(string RegistrationNumber="",string Title="",string Address="",string DocumentType="",string LawCode="",
    string DocumentNature="A",string DefaultMissingReason="",string TaxNumber="");
public sealed record SgkName(Guid TeacherId,string GivenName,string FamilyName);
public sealed record SgkXmlSettings(SgkWorkplace Workplace,SgkName[] Names);
public sealed record SgkPerson(Guid TeacherId,string GivenName,string FamilyName,string MissingReason="",string ExitReason="",
    string WorkedOnReport="",string FirstSurname="",string Bonus="",string PaidLeaveDays="",string PaidLeaveGross="");
