using Hakedis.Contracts;

namespace Hakedis.Storage;

public sealed class UiPreferencesStore(string root)
{
    public string PathName=>Path.Combine(root,"ArayuzAyarlari.json");
    public static IReadOnlyList<WindowSizeOption> Sizes {get;}=[new(1280,720),new(1366,768),new(1920,1080)];
    public UiPreferences Read()
    {
        var data=File.Exists(PathName)?new JsonDraftStore().Load<UiPreferences>(PathName,ModuleId.TemelEgitim,"arayuz-ayarlari.v1").Data:new();
        Validate(data);return data;
    }
    private static void Validate(UiPreferences data)
    {if(!Sizes.Any(s=>s.Width==data.Width && s.Height==data.Height))throw new ArgumentException("Desteklenen pencere boyutlarından birini seçin.");}
    public void Save(UiPreferences data)
    {Validate(data);new JsonDraftStore().Save(PathName,new DraftDocument<UiPreferences>(1,ModuleId.TemelEgitim,"arayuz-ayarlari.v1",DateTimeOffset.UtcNow,data));}
}
