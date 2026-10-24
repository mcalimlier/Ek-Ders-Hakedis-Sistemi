using Hakedis.Contracts;

namespace Hakedis.TemelEgitim.Core;

public sealed record PaymentRow(TeacherRecord Teacher,MonthlyInput Input,TeacherResult Result,CalendarResult? SgkInformation=null,string SgkError="");
public sealed record PaymentSnapshot(int Year,int Month,PeriodValues Constants,PaymentRow[] Rows,string MissingDaysStatus,string SgkRuleVersion="",
    ReportDetails? Details=null,PersonnelMovement? Movement=null);
