using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hakedis.Contracts;

namespace Hakedis.App;

public partial class SearchableOccupation : UserControl
{
    public static readonly DependencyProperty CodeProperty = DependencyProperty.Register(nameof(Code),typeof(string),typeof(SearchableOccupation),
        new FrameworkPropertyMetadata("",FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,(d,_)=>((SearchableOccupation)d).RefreshLabel()));
    public string Code { get=>(string)GetValue(CodeProperty);set=>SetValue(CodeProperty,value); }
    public SearchableOccupation(){InitializeComponent();RefreshLabel();}
    private void RefreshLabel()
    {if(SelectedText is not null)SelectedText.Text=OccupationCatalog.All.FirstOrDefault(x=>x.Code==Code)?.Display ?? (string.IsNullOrEmpty(Code)?"Meslek kodu seçin":Code);}
    private void Popup_Opened(object sender,EventArgs e)
    {Search.Text="";Filter();Search.Focus();}
    private void Search_Changed(object sender,TextChangedEventArgs e){if(Options is not null)Filter();}
    private void Filter(){var rows=OccupationCatalog.Search(Search.Text);Options.ItemsSource=rows;Count.Text=$"{rows.Count:N0} / {OccupationCatalog.All.Count:N0} meslek · Kod veya sözcüklerle arayın";}
    private void Choose()
    {if(Options.SelectedItem is Occupation occupation){SetCurrentValue(CodeProperty,occupation.Code);Picker.IsChecked=false;Picker.Focus();}}
    private void Option_Click(object sender,MouseButtonEventArgs e)
    {if(ItemsControl.ContainerFromElement(Options,e.OriginalSource as DependencyObject) is ListBoxItem)Choose();}
    private void Option_KeyDown(object sender,KeyEventArgs e)
    {if(e.Key==Key.Enter){Choose();e.Handled=true;}else if(e.Key==Key.Escape){Picker.IsChecked=false;e.Handled=true;}}
}
