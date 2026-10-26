using System.Windows;
using System.Windows.Controls;
using Hakedis.Storage;

namespace Hakedis.App;

public partial class SgkXmlWindow : Window
{
    private readonly SgkXmlEditor editor;
    private readonly SgkXmlStore store;
    private readonly string outputRoot;
    private readonly Action<string>? notify;
    public string? OutputPath {get;private set;}
    public SgkXmlWindow(ArchivedPayment record,SgkXmlStore store,string? outputRoot=null,Action<string>? notification=null)
    {
        InitializeComponent();notify=notification;this.store=store;this.outputRoot=outputRoot??PaymentReportService.DefaultOutputRoot;
        editor=new(record,store.Read(record.Document.ModuleId));DataContext=editor;
        Width=Math.Min(1200,SystemParameters.WorkArea.Width);Height=Math.Min(700,SystemParameters.WorkArea.Height);
        MinWidth=Math.Min(MinWidth,Width);MinHeight=Math.Min(MinHeight,Height);
    }
    private void Export_Click(object sender,RoutedEventArgs e)
    {
        try
        {
            if(!PeopleGrid.CommitEdit(DataGridEditingUnit.Cell,true) || !PeopleGrid.CommitEdit(DataGridEditingUnit.Row,true))throw new ArgumentException("Önce tablo hücresindeki girişi tamamlayın.");
            store.EnsureCurrent(editor.Record);
            _=editor.Build();
            // Kullanıcının onayladığı işyeri ve ad ayrımları saklanır; hakediş kaydı değiştirilmez.
            store.Save(editor.Record.Document.ModuleId,editor.Settings());
            OutputPath=SgkXmlStore.Export(outputRoot,editor.Record.Document.ModuleId,editor.Record.Document.Data,editor.Workplace(),editor.ReadPeople());
            DialogResult=true;
        }
        catch(Exception ex) when(ex is ArgumentException or InvalidOperationException or System.IO.IOException or UnauthorizedAccessException or System.Xml.XmlException or System.Xml.Schema.XmlSchemaException)
        {if(notify is not null)notify(ex.Message);else MessageBox.Show(this,ex.Message,"XML kaydedilemedi",MessageBoxButton.OK,MessageBoxImage.Warning);ErrorText.Text=ex.Message;if(string.IsNullOrWhiteSpace(editor.RegistrationNumber))RegistryField.Focus();else if(string.IsNullOrWhiteSpace(editor.WorkplaceTitle))TitleField.Focus();else if(string.IsNullOrWhiteSpace(editor.Address))AddressField.Focus();}
    }
}
