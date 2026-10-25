using System.Windows;
using Microsoft.Win32;

namespace Hakedis.App;

public partial class MainWindow : Window
{
    private readonly ShellViewModel shell;
    private readonly Action<string>? notify;
    private readonly Func<string,string,bool>? confirmation;
    private readonly Action<string> openFolder;
    private readonly string? personalOutputRoot;
    private readonly Func<BillingChoiceViewModel,bool>? billingChoicePrompt;
    private WorkspaceViewModel model => shell.Workspace;
    public MainWindow() : this(new ShellViewModel(System.IO.Path.Combine(AppContext.BaseDirectory,"Veriler","ogretmenler.json"))) { }
    public MainWindow(ShellViewModel viewModel,Action<string>? notification=null,Func<string,string,bool>? confirmation=null,
        Action<string>? folderOpener=null,string? personalOutputRoot=null,Func<BillingChoiceViewModel,bool>? billingChoicePrompt=null)
    {
        shell=viewModel;notify=notification;this.confirmation=confirmation;this.personalOutputRoot=personalOutputRoot;this.billingChoicePrompt=billingChoicePrompt;
        openFolder=folderOpener??(folder=>{_=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(folder){UseShellExecute=true});});
        InitializeComponent();DataContext=shell;shell.Settings.Applied+=ApplyWindowPreference;
    }
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplyWindowPreference();
    }
    private void ApplyWindowPreference()
    {
        var workArea=SystemParameters.WorkArea;
        var size=Hakedis.Contracts.WindowSizing.Fit(shell.Settings.Preference,workArea.Width,workArea.Height);
        WindowState=WindowState.Normal;
        MinWidth=Math.Min(1040,workArea.Width);MinHeight=Math.Min(580,workArea.Height);
        Width=size.Width;Height=size.Height;
        Left=workArea.Left+(workArea.Width-Width)/2;Top=workArea.Top+(workArea.Height-Height)/2;
    }
    private void ApplySettings_Click(object sender,RoutedEventArgs e)
    {try{shell.Settings.Save();}catch(Exception ex) when(ex is ArgumentException or System.IO.IOException or UnauthorizedAccessException){shell.Settings.ShowError(ex.Message);}}
    protected override void OnClosed(EventArgs e)
    {shell.Settings.Applied-=ApplyWindowPreference;base.OnClosed(e);}
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if(shell.Monthly.HasHybridChanges || shell.Calculation.HasHybridChanges || shell.Calculation.IsArchiveEditing ||
           shell.Registration.HasDraft || shell.Registration.IsEditingRecord || shell.CommonConstants.IsEditing || shell.RetiredConstants.IsEditing ||
           shell.Teachers.HasChanges || shell.Outputs.HasDetailsChanges)
            e.Cancel=!ConfirmArchive("Kaydedilmemiş giriş veya tamamlanmamış düzenleme var.\nProgram kapatılırsa bu değişiklikler kaybolur; kayıtlı dosyalar korunur.\nYine de kapatılsın mı?","Kaydedilmemiş değişiklikler");
        base.OnClosing(e);
    }
    private void Navigate_Click(object sender, RoutedEventArgs e)
        {try{shell.Navigate((string)((System.Windows.Controls.Button)sender).Tag);}catch(InvalidOperationException ex){MessageBox.Show(this,ex.Message,"Bilgi",MessageBoxButton.OK,MessageBoxImage.Information);}}
    private void Calculate_Click(object sender, RoutedEventArgs e) => Run(model.Calculate);
    private void NewTeacher_Click(object sender, RoutedEventArgs e) {if(shell.Registration.IsEditingRecord)shell.CancelTeacherEdit();else shell.Registration.Reset();}
    private void RegisterTeacher_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if(!Hakedis.Contracts.OccupationCatalog.Contains(shell.Registration.Editor.OccupationCode))
                throw new ArgumentException("SGK MESLEK KODU için listeden kod ve karşılığını seçin.");
            if(shell.Registration.IsEditingRecord){shell.SaveTeacherEdit();ShowSaved();}else shell.SaveNewTeacher();
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
        var note=teacher.Assignments is null?"Geçmiş hesap taslakları korunur.":"Tek kişi kartı ve görev geçmişi tüm modül listelerinden kaldırılır. Kayıtlı hakedişler korunur.";
        if (MessageBox.Show(this, $"{teacher.FullName} adlı öğretmeni listeden silmek istiyor musunuz?\n{note}",
            "Öğretmen kaydını sil", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
            RunTeacher(()=>{shell.Teachers.DeleteSelected();shell.Monthly.RefreshTeachers();shell.Calculation.RefreshTeachers();});
    }
    private void ShowSaved()
    {if(notify is not null)notify("Kayıtlar güncellendi.");else MessageBox.Show(this,"Kayıtlar güncellendi.","Bilgi",MessageBoxButton.OK,MessageBoxImage.Information);}
    private bool CommitHybrid()
    {
        if(HybridGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Cell,true) &&
           HybridGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row,true) && !HasValidationError(HybridGrid))return true;
        shell.Calculation.ShowError("Tablodaki hücre girişini tamamlayın.");return false;
    }
    private void EditHybrid_Click(object sender,RoutedEventArgs e)
    {if(CommitHybrid())RunPeriod(shell.Calculation,()=>{shell.BeginHybridTeacherEdit();TeacherEntryScroll.ScrollToTop();});}
    private void SaveHybrid_Click(object sender,RoutedEventArgs e)
    {if(CommitHybrid())RunPeriod(shell.Calculation,()=>{shell.Calculation.SaveHybridChanges();ShowSaved();});}
    private void DeleteHybrid_Click(object sender,RoutedEventArgs e)
    {
        if(!CommitHybrid())return;var row=shell.Calculation.SelectedHybridRow;if(row is null)return;
        var note=row.Teacher.Assignments is null?"Geçmiş hakedişler korunur.":"Tek kişi kartı ve görev geçmişi tüm modül listelerinden kaldırılır. Kayıtlı hakedişler korunur.";
        if(MessageBox.Show(this,$"{row.FullName} adlı öğretmeni listeden silmek istiyor musunuz?\n{note}","Öğretmen kaydını sil",MessageBoxButton.YesNo,MessageBoxImage.Warning,MessageBoxResult.No)==MessageBoxResult.Yes)
            RunPeriod(shell.Calculation,shell.Calculation.DeleteHybridTeacher);
    }
    private void SaveMonthly_Click(object sender,RoutedEventArgs e) => RunPeriod(shell.Monthly,()=>shell.Monthly.Save(true));
    private void EditMonthly_Click(object sender,RoutedEventArgs e) => RunPeriod(shell.Monthly,shell.Monthly.BeginEdit);
    private void SaveConstants_Click(object sender,RoutedEventArgs e)
    {try{shell.CommonConstants.ToggleSave();}catch(Exception ex) when(ex is ArgumentException or System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException){shell.CommonConstants.ShowError(ex.Message);}}
    private void SaveRetiredConstants_Click(object sender,RoutedEventArgs e)
    {try{shell.RetiredConstants.ToggleSave();}catch(Exception ex) when(ex is ArgumentException or System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException){shell.RetiredConstants.ShowError(ex.Message);}}
    private void ClearWork_Click(object sender,RoutedEventArgs e)
    {
        if(MessageBox.Show(this,"Ekrandaki çalışma temizlensin mi? Kaydedilmemiş girişler bırakılır; kayıtlı hakedişler ve aylık dosyalar silinmez.","Çalışmayı temizle",MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)==MessageBoxResult.Yes)
            RunPeriod(shell.Calculation,shell.ClearCurrentWork);
    }
    private void ReloadOutputs_Click(object sender,RoutedEventArgs e)=>shell.Outputs.Reload();
    private void ReloadPersonal_Click(object sender,RoutedEventArgs e)=>shell.Personal.Reload();
    private void CreatePersonal_Click(object sender,RoutedEventArgs e)=>RunPersonal(()=>
    {
        var source=shell.Personal.ReadSelection();var teacher=shell.Personal.SelectedTeacher!;
        var path=new PaymentReportService().ExportPersonal(source,teacher.Id,shell.Outputs.Details.Read(),personalOutputRoot);
        shell.Personal.AcceptOutput(path);
    });
    private void OpenPersonalFolder_Click(object sender,RoutedEventArgs e)=>RunPersonal(()=>
    {
        var folder=shell.Personal.OutputFolder;
        if(!shell.Personal.CanOpenFolder || folder is null || !System.IO.Directory.Exists(folder))throw new InvalidOperationException("Önce kişisel bordroyu oluşturun. Kaydedilen dosya veya klasör taşınmış olabilir.");
        openFolder(folder);
    });
    private void RunPersonal(Action action)
    {
        try{action();}
        catch(Exception ex) when(ex is ArgumentException or InvalidOperationException or System.IO.IOException or UnauthorizedAccessException or NotSupportedException or System.ComponentModel.Win32Exception)
        {shell.Personal.ShowError(ex.Message);}
    }
    private void SaveReportDetails_Click(object sender,RoutedEventArgs e)=>RunOutput(shell.Outputs.SaveDetails);
    private void SelectOutput(object sender)
    {if(((FrameworkElement)sender).DataContext is Hakedis.Storage.ArchivedPayment record)shell.Outputs.Selected=record;}
    private void EditArchive_Click(object sender,RoutedEventArgs e)
    {SelectOutput(sender);RunOutput(shell.BeginArchiveEdit);}
    private bool ConfirmArchive(string message,string title)=>confirmation?.Invoke(message,title)??
        MessageBox.Show(this,message,title,MessageBoxButton.YesNo,MessageBoxImage.Warning,MessageBoxResult.No)==MessageBoxResult.Yes;
    private void DeleteArchive_Click(object sender,RoutedEventArgs e)
    {
        SelectOutput(sender);RunOutput(()=>{
            var record=shell.Outputs.ReadSelected();
            if(!ConfirmArchive($"{record.Module} · {record.Period}\n{record.Count} öğretmene ait kayıtlı hakediş silinsin mi?\nÖğretmen bilgileri ve aylık ders girişleri korunur. Hayır seçilirse işlem yapılmaz.","Hakedişi sil"))return;
            shell.Outputs.DeleteSelected(true);
        });
    }
    private void SaveArchive_Click(object sender,RoutedEventArgs e)
    {
        if(!CommitHybrid())return;
        if(!ConfirmArchive("Eski hakedişin yerine düzenlediğiniz hakediş kaydedilsin mi?\nOnaylarsanız eski kayıt yenisiyle değiştirilir; listede yalnızca bir hakediş kalır.\nKurtarma yedeği listede gösterilmez. Hayır seçilirse eski kayıt korunur.","Hakedişi değiştir"))return;
        RunPeriod(shell.Calculation,()=>{if(shell.SaveArchiveEdit(true))ShowSaved();});
    }
    private void CancelArchive_Click(object sender,RoutedEventArgs e)=>shell.CancelArchiveEdit();
    private void ExportOutputs_Click(object sender,RoutedEventArgs e)
    {SelectOutput(sender);RunOutput(()=>{var folder=new PaymentReportService().Export(shell.Outputs.ReadSelected(),shell.Outputs.Details.Read());shell.Outputs.ShowStatus("Üç PDF kaydedildi: "+folder);MessageBox.Show(this,"Üç PDF kaydedildi:\n"+folder,"Çıktı",MessageBoxButton.OK,MessageBoxImage.Information);});}
    private void PrintOutputs_Click(object sender,RoutedEventArgs e)
    {SelectOutput(sender);RunOutput(()=>{if(new PaymentReportService().Print(shell.Outputs.ReadSelected(),shell.Outputs.Details.Read()))shell.Outputs.ShowStatus("Üç belge yazıcıya gönderildi.");});}
    private void XmlOutput_Click(object sender,RoutedEventArgs e)
    {
        SelectOutput(sender);RunOutput(()=>{
            var dialog=new SgkXmlWindow(shell.Outputs.ReadSelected(),new Hakedis.Storage.SgkXmlStore(shell.Outputs.DataRoot)){Owner=this};
            if(dialog.ShowDialog()==true)
            {shell.Outputs.ShowStatus("SGK şemasına uygun XML kaydedildi: "+dialog.OutputPath);MessageBox.Show(this,"XML kaydedildi:\n"+dialog.OutputPath,"SGK XML",MessageBoxButton.OK,MessageBoxImage.Information);}
        });
    }
    private void RunOutput(Action action)
    {try{action();}catch(Exception ex) when(ex is ArgumentException or InvalidOperationException or System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException or NotSupportedException or System.Printing.PrintSystemException or System.Windows.Xps.XpsException){shell.Outputs.ShowStatus(ex.Message);MessageBox.Show(this,ex.Message,"Çıktı",MessageBoxButton.OK,MessageBoxImage.Information);}}
    private void ReloadPayment_Click(object sender,RoutedEventArgs e)
    {
        if(MessageBox.Show(this,"Kaydedilmemiş tablo değişiklikleri yerine son kayıtlar yüklensin mi?","Kayıtları yenile",MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)==MessageBoxResult.Yes)
            RunPeriod(shell.Calculation,shell.Calculation.Reload);
    }
    private bool ChooseBilling(PayrollRow row)
    {
        var editor=new BillingChoiceViewModel(row.Teacher,shell.Calculation.Year,shell.Calculation.Month,row.Read(),shell.Outputs.DataRoot);
        var accepted=billingChoicePrompt?.Invoke(editor)??new BillingChoiceWindow(editor){Owner=this}.ShowDialog()==true;
        if(!accepted)return false;
        shell.SaveBillingChoice(row,editor.SelectedModule?.Id??throw new ArgumentException("Ödeme modülünü seçin."),editor.ReadInput());
        return true;
    }
    private void ChangeBillingChoice_Click(object sender,RoutedEventArgs e)
    {
        if(!CommitHybrid() || !shell.Calculation.CanChooseBillingModule)return;
        RunPeriod(shell.Calculation,()=>{
            if(shell.Calculation.Rows.Any(r=>r.Teacher!=r.SourceTeacher))throw new InvalidOperationException("Önce öğretmen bilgileri için Değişiklikleri kaydet düğmesini kullanın.");
            if(ChooseBilling(shell.Calculation.SelectedHybridRow!))ShowSaved();
        });
    }
    private void CreatePayment_Click(object sender,RoutedEventArgs e)
    {
        if(!HybridGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Cell,true) ||
           !HybridGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row,true) || HasValidationError(HybridGrid))
        {shell.Calculation.ShowError("Tablodaki hücre girişini tamamlayın.");return;}
        RunPeriod(shell.Calculation,()=>{
            if(shell.Calculation.Rows.Any(r=>r.Teacher!=r.SourceTeacher))throw new InvalidOperationException("Önce öğretmen bilgileri için Değişiklikleri kaydet düğmesini kullanın.");
            while(shell.Calculation.BillingChoicesNeeded.FirstOrDefault() is { } row)
            {
                if(!ChooseBilling(row)){shell.Calculation.ShowError("Ödeme tercihi yapılmadı; hakediş oluşturulmadı.");return;}
            }
            shell.Calculation.CreatePayment();
            const string message="İşlem başarılı. Hakediş oluşturuldu ve kaydedildi.";
            if(notify is not null)notify(message);else MessageBox.Show(this,message,"Hakediş",MessageBoxButton.OK,MessageBoxImage.Information);
            shell.Navigate("Outputs");
            shell.Outputs.Selected=shell.Outputs.Records.FirstOrDefault();
        });
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
