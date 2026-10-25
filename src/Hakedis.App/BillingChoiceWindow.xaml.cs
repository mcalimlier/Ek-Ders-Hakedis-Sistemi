using System.Windows;

namespace Hakedis.App;

public partial class BillingChoiceWindow : Window
{
    private readonly BillingChoiceViewModel model;
    public BillingChoiceWindow(BillingChoiceViewModel model)
    {this.model=model;InitializeComponent();DataContext=model;Height=Math.Min(680,SystemParameters.WorkArea.Height);Width=Math.Min(650,SystemParameters.WorkArea.Width);}
    private void Confirm_Click(object sender,RoutedEventArgs e)
    {try{_=model.ReadInput();DialogResult=true;}catch(ArgumentException ex){model.ShowError(ex.Message);}}
}
