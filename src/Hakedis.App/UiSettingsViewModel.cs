using System.ComponentModel;
using Hakedis.Contracts;
using Hakedis.Storage;

namespace Hakedis.App;

public sealed class UiSettingsViewModel : INotifyPropertyChanged
{
    private readonly UiPreferencesStore store;
    private WindowSizeOption selected;
    public IReadOnlyList<WindowSizeOption> Sizes=>UiPreferencesStore.Sizes;
    public WindowSizeOption SelectedSize {get=>selected;set{if(value is null || !Sizes.Contains(value))return;selected=value;PropertyChanged?.Invoke(this,new(nameof(SelectedSize)));}}
    public UiPreferences Preference=>new(selected.Width,selected.Height);
    public string Status {get;private set;}="Pencere boyutunu seçip Uygula düğmesine basın. Seçiminiz sonraki açılışlarda da kullanılır.";
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Applied;
    public UiSettingsViewModel(string root)
    {
        store=new(root);var p=store.Read();selected=Sizes.Single(s=>s.Width==p.Width && s.Height==p.Height);
    }
    public void Save()
    {
        store.Save(Preference);Applied?.Invoke();
        Status=$"{selected.Label} tercihi kaydedildi. Pencere, ekran ölçeği ve görev çubuğu alanına göre sığdırılır.";
        PropertyChanged?.Invoke(this,new(nameof(Status)));
    }
    public void ShowError(string message){Status=message;PropertyChanged?.Invoke(this,new(nameof(Status)));}
}
