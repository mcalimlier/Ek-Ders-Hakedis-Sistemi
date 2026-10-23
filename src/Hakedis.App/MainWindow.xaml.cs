using System.Windows;
using Microsoft.Win32;

namespace Hakedis.App;

public partial class MainWindow : Window
{
    private readonly ShellViewModel shell = new(System.IO.Path.Combine(AppContext.BaseDirectory, "Veriler", "ogretmenler.json"));
    private WorkspaceViewModel model => shell.Workspace;
    public MainWindow() { InitializeComponent(); DataContext = shell; }
    private void Navigate_Click(object sender, RoutedEventArgs e)
        => shell.Navigate((string)((System.Windows.Controls.Button)sender).Tag);
    private void Calculate_Click(object sender, RoutedEventArgs e) => Run(model.Calculate);
    private void NewTeacher_Click(object sender, RoutedEventArgs e) => shell.Teachers.New();
    private void SaveTeacher_Click(object sender, RoutedEventArgs e) => RunTeacher(shell.Teachers.Save);
    private void UseTeacher_Click(object sender, RoutedEventArgs e) => RunTeacher(shell.UseSelectedTeacher);
    private void DeleteTeacher_Click(object sender, RoutedEventArgs e)
    {
        var teacher = shell.Teachers.Selected;
        if (teacher is null) return;
        if (MessageBox.Show(this, $"{teacher.FullName} adlı öğretmeni listeden silmek istiyor musunuz?\nGeçmiş hesap taslakları korunur.",
            "Öğretmen kaydını sil", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
            RunTeacher(shell.Teachers.DeleteSelected);
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
