using Hakedis.App;
using Hakedis.Contracts;
using Hakedis.Storage;

namespace Hakedis.Tests;

public sealed class SinglePersonPayrollTests : IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"EdhsSinglePayroll",Guid.NewGuid().ToString("N"));
    private ShellViewModel Setup()
    {
        var s=new ShellViewModel(Path.Combine(root,"ogretmenler.json"));
        Register(s,ModuleId.TemelEgitim,"10000000146");s.Calculation.Rows[0].DayHours="75";s.Calculation.CreatePayment();return s;
    }
    private static void Register(ShellViewModel s,ModuleId module,string tc)
    {
        s.RegistrationModule=s.Workspace.Modules.Single(m=>m.Id==module);s.Registration.Editor.FullName="Bordro Deneme";
        s.Registration.Editor.IdentityNumber=tc;s.Registration.Editor.OccupationCode="2341.04";s.Registration.Editor.Iban="TR330006100519786457841326";s.SaveNewTeacher();
    }
    [Fact] public void Deleting_and_registering_in_another_module_cannot_create_a_second_payroll_for_same_tc_and_month()
    {
        var s=Setup();var record=s.Outputs.Records.Single();var bytes=File.ReadAllBytes(record.Path);
        new TeacherDirectory(Path.Combine(root,"ogretmenler.json")).Delete(s.Calculation.Rows[0].Teacher.Id);
        Register(s,ModuleId.DinOgretimi,"10000000146");s.Calculation.SelectedModule=s.Calculation.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);
        s.Calculation.Rows[0].DayHours="75";
        var error=Assert.Throws<InvalidOperationException>(()=>s.Calculation.CreatePayment());Assert.Contains("aynı dönemde",error.Message);
        Assert.Single(new PaymentArchiveStore(root).ReadAll());Assert.Equal(bytes,File.ReadAllBytes(record.Path));
    }
    [Fact] public void Editing_a_saved_payroll_cannot_change_its_tc_to_a_person_paid_by_another_module()
    {
        var s=Setup();Register(s,ModuleId.DinOgretimi,"10000000200");
        s.Calculation.SelectedModule=s.Calculation.Modules.Single(m=>m.Id==ModuleId.DinOgretimi);s.Calculation.Rows[0].DayHours="75";s.Calculation.CreatePayment();
        var record=s.Outputs.Records.Single(r=>r.Document.ModuleId==ModuleId.DinOgretimi);var bytes=File.ReadAllBytes(record.Path);
        var data=record.Document.Data;var row=data.Rows[0];data=data with {Rows=[row with {Teacher=row.Teacher with {IdentityNumber="10000000146"}}]};
        Assert.Throws<InvalidOperationException>(()=>new PaymentArchiveStore(root).Replace(record,data,true));
        Assert.Equal(bytes,File.ReadAllBytes(record.Path));
    }
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
}
