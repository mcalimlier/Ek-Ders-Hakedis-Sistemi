using System.Windows;
using Microsoft.Win32;

namespace Hakedis.App;

public partial class MainWindow : Window
{
    private readonly WorkspaceViewModel model = new();
    public MainWindow() { InitializeComponent(); DataContext = model; }
    private void Calculate_Click(object sender, RoutedEventArgs e) => Run(model.Calculate);
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "Hakediş taslağı (*.json)|*.json", FileName = "temel-egitim-taslak.json" };
        if (dialog.ShowDialog() == true) Run(() => model.Save(dialog.FileName));
    }
    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Hakediş taslağı (*.json)|*.json" };
        if (dialog.ShowDialog() == true) Run(() => model.Open(dialog.FileName));
    }
    private void Run(Action action)
    {
        try { action(); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException
            or System.IO.IOException or UnauthorizedAccessException or OverflowException)
        { model.ShowError(ex.Message); }
    }
}
