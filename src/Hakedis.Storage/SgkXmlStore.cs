using Hakedis.Contracts;
using Hakedis.TemelEgitim.Core;

namespace Hakedis.Storage;

public sealed class SgkXmlStore(string root)
{
    private static ModuleId Group(ModuleId module)=>module==ModuleId.HalkEgitimUstaOgretici?module:ModuleId.TemelEgitim;
    private string PathName(ModuleId module)=>Path.Combine(root,Group(module)==ModuleId.TemelEgitim?"SgkXml-Ogretmenler.json":"SgkXml-UstaOgreticiler.json");
    public SgkXmlSettings Read(ModuleId module)
    {
        var data=File.Exists(PathName(module))?new JsonDraftStore().Load<SgkXmlSettings>(PathName(module),Group(module),"sgk-xml.v1").Data:new(new(),[]);
        if(data.Workplace is null || data.Names is null || data.Names.Any(n=>n is null))throw new InvalidDataException("SGK işyeri bilgi dosyası eksik veya tutarsız.");
        var w=data.Workplace;
        if(new[]{w.RegistrationNumber,w.Title,w.Address,w.DocumentType,w.LawCode,w.DocumentNature,w.DefaultMissingReason,w.TaxNumber}.Any(v=>v is null))
            throw new InvalidDataException("SGK işyeri bilgi alanları geçersiz; yedeği kontrol edin.");
        return data;
    }
    public void Save(ModuleId module,SgkXmlSettings settings)
    {SgkXmlBuilder.ValidateWorkplace(settings.Workplace);new JsonDraftStore().Save(PathName(module),new DraftDocument<SgkXmlSettings>(1,Group(module),"sgk-xml.v1",DateTimeOffset.UtcNow,settings));}
    public void EnsureCurrent(ArchivedPayment record)
    {
        if(new PaymentArchiveStore(root).Read(record.Path).Version!=record.Version)
            throw new InvalidOperationException("Hakediş kaydı bu ekran açıkken değişmiş. XML ekranını kapatıp Çıktı listesini yenileyin.");
    }
    public static string Export(string outputRoot,ModuleId module,PaymentSnapshot snapshot,SgkWorkplace workplace,IReadOnlyList<SgkPerson> people)
    {
        PaymentArchiveStore.Validate(snapshot,module);
        var bytes=Build(module,snapshot,workplace,people);
        var folder=Path.Combine(outputRoot,module.ToString(),$"{snapshot.Year:D4}-{snapshot.Month:D2}");Directory.CreateDirectory(folder);
        var path=Path.Combine(folder,$"SGK-{snapshot.Year:D4}-{snapshot.Month:D2}-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.xml");
        var temporary=path+".tmp";
        try{using(var file=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){file.Write(bytes);file.Flush(true);}File.Move(temporary,path);return path;}
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
    public static byte[] Build(ModuleId module,PaymentSnapshot snapshot,SgkWorkplace workplace,IReadOnlyList<SgkPerson> people)
    {
        PaymentArchiveStore.Validate(snapshot,module);
        return SgkXmlBuilder.Build(snapshot,workplace,people,row=>PayrollCores.Calendar(module,snapshot.Year,snapshot.Month,
            row.Teacher.StartDate,row.Teacher.EndDate,row.Input.DayHours,row.Input.DykHours),PayrollCores.CalendarRuleVersion(module));
    }
}
