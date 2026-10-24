using System.ComponentModel;
using Hakedis.Storage;

namespace Hakedis.App;

public sealed class CommonConstantsViewModel : INotifyPropertyChanged
{
    private readonly CommonConstantsStore store;
    public PeriodConstantsEditor Editor {get;}
    public bool IsEditing {get;private set;}
    public bool IsReadOnly=>!IsEditing;
    public string ButtonText=>IsEditing?"Dönem sabitlerini değiştir":"Güncelle";
    public string Status {get;private set;}="Bu sabitler tüm modüllerin yeni ve aktif hesaplarında kullanılır. Kayıtlı hakedişler kendi sabitlerini korur.";
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Saved;
    public CommonConstantsViewModel(string root){store=new(root);Editor=new(store.Read());}
    public void ToggleSave()
    {
        if(!IsEditing){IsEditing=true;Status="Ortak sabitleri düzenleyin, sonra Dönem sabitlerini değiştir düğmesiyle kaydedin.";}
        else{store.Save(Editor.Read());IsEditing=false;Status="Ortak dönem sabitleri kaydedildi. Önceki hakediş kayıtları değiştirilmedi.";Saved?.Invoke();}
        Notify();
    }
    public void ShowError(string message){Status=message;Notify();}
    private void Notify(){foreach(var name in new[]{nameof(IsEditing),nameof(IsReadOnly),nameof(ButtonText),nameof(Status)})PropertyChanged?.Invoke(this,new(name));}
}
