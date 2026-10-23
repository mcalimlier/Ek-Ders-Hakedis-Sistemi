using System.Windows;
using Microsoft.Win32;

namespace Hakedis.App;

public partial class MainWindow : Window
{
    private readonly ShellViewModel shell;
    private WorkspaceViewModel model => shell.Workspace;
    public MainWindow() : this(new ShellViewModel(System.IO.Path.Combine(AppContext.BaseDirectory,"Veriler","ogretmenler.json"))) { }
    public MainWindow(ShellViewModel viewModel) {shell=viewModel;InitializeComponent();DataContext=shell;}
    private void Navigate_Click(object sender, RoutedEventArgs e)
        => shell.Navigate((string)((System.Windows.Controls.Button)sender).Tag);
    private void Calculate_Click(object sender, RoutedEventArgs e) => Run(model.Calculate);
    private void NewTeacher_Click(object sender, RoutedEventArgs e) => shell.Registration.Reset();
    private void RegisterTeacher_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if(!Hakedis.Contracts.OccupationCatalog.Contains(shell.Registration.Editor.OccupationCode))
                throw new ArgumentException("SGK MESLEK KODU için listeden kod ve karşılığını seçin.");
            shell.SaveNewTeacher();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException)
        { shell.Registration.ShowStatus(ex.Message); }
    }
    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton != System.Windows.Input.MouseButton.Left) return;
        if (e.ClickCount == 2) WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        else DragMove();
    }
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void SaveTeacher_Click(object sender, RoutedEventArgs e) => RunTeacher(()=>{shell.Teachers.Save();shell.Monthly.RefreshTeachers();shell.Calculation.RefreshTeachers();});
    private void UseTeacher_Click(object sender, RoutedEventArgs e) => RunTeacher(shell.UseSelectedTeacher);
    private void DeleteTeacher_Click(object sender, RoutedEventArgs e)
    {
        var teacher = shell.Teachers.Selected;
        if (teacher is null) return;
        if (MessageBox.Show(this, $"{teacher.FullName} adlı öğretmeni listeden silmek istiyor musunuz?\nGeçmiş hesap taslakları korunur.",
            "Öğretmen kaydını sil", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
            RunTeacher(()=>{shell.Teachers.DeleteSelected();shell.Monthly.RefreshTeachers();shell.Calculation.RefreshTeachers();});
    }
    private void SaveMonthly_Click(object sender,RoutedEventArgs e) => RunPeriod(shell.Monthly,()=>shell.Monthly.Save(true));
    private void EditMonthly_Click(object sender,RoutedEventArgs e) => RunPeriod(shell.Monthly,shell.Monthly.BeginEdit);
    private void SaveConstants_Click(object sender,RoutedEventArgs e) => RunPeriod(shell.Calculation,shell.Calculation.SaveConstants);
    private void ReloadPayment_Click(object sender,RoutedEventArgs e)
    {
        if(MessageBox.Show(this,"Kaydedilmemiş tablo değişiklikleri yerine son kayıtlar yüklensin mi?","Kayıtları yenile",MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)==MessageBoxResult.Yes)
            RunPeriod(shell.Calculation,shell.Calculation.Reload);
    }
    private void CreatePayment_Click(object sender,RoutedEventArgs e)
    {
        if(!HybridGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Cell,true) ||
           !HybridGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row,true) || HasValidationError(HybridGrid))
        {shell.Calculation.ShowError("Tablodaki hücre girişini tamamlayın.");return;}
        RunPeriod(shell.Calculation,shell.Calculation.CreatePayment);
    }
    private static bool HasValidationError(DependencyObject element)
    {
        if(System.Windows.Controls.Validation.GetHasError(element))return true;
        for(var i=0;i<System.Windows.Media.VisualTreeHelper.GetChildrenCount(element);i++)
            if(HasValidationError(System.Windows.Media.VisualTreeHelper.GetChild(element,i)))return true;
        return false;
    }
    private static void RunPeriod(PeriodWorkspace period,Action action)
    {
        try{action();}
        catch(Exception ex) when(ex is ArgumentException or InvalidOperationException or NotSupportedException or
            System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException or OverflowException)
        {period.ShowError(ex.Message);}
    }
    private void RunTeacher(Action action)
    {
        try { action(); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException)
        { shell.Teachers.ShowError(ex.Message); }
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "Hakediş taslağı (*.json)|*.json", FileName = "temel-egitim-taslak.json" };
        if (dialog.ShowDialog() == true) Run(() => model.Save(dialog.FileName));
    }
    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Hakediş taslağı (*.json)|*.json" };
        if (dialog.ShowDialog() == true) { Run(() => model.Open(dialog.FileName)); shell.Navigate("Entry"); }
    }
    private void Run(Action action)
    {
        try { action(); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException
            or System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException or OverflowException)
        { model.ShowError(ex.Message); }
    }
}
