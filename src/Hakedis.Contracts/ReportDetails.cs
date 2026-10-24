namespace Hakedis.Contracts;

public sealed record ReportDetails(string Institution="",string Bank="",string PreparedBy="",string PreparedTitle="Şube Müdürü",
    string AuthorizedBy="",string AuthorizedTitle="İlçe Milli Eğitim Müdürü",string Treasurer="");
public sealed record PersonnelMovement(int Previous,int Entered,int Exited,Guid[]? EnteredIds=null);
