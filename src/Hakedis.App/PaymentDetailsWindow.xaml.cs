using System.Windows;
using System.Windows.Controls;
using Hakedis.Storage;
using Hakedis.TemelEgitim.Core;
namespace Hakedis.App;
public partial class PaymentDetailsWindow : Window
{
    private readonly PaymentDetailsViewModel model;
    public PaymentDetailsWindow(ArchivedPayment archive)
    {
        model=new(archive);InitializeComponent();DataContext=model;
        MaxWidth=SystemParameters.WorkArea.Width;MaxHeight=SystemParameters.WorkArea.Height;
        Width=System.Math.Min(Width,MaxWidth);Height=System.Math.Min(Height,MaxHeight);
        DetailGrid.SelectedIndex=0;
    }
    private void Person_Selected(object sender,SelectionChangedEventArgs e)
    {if(PersonInfo is not null && DetailGrid.SelectedItem is PaymentRow row)PersonInfo.Text=model.PersonDetails(row);}
    private void Close_Click(object sender,RoutedEventArgs e)=>Close();
}
