using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace Hakedis.App;
// Applied to input styles, restricted to numeric binding paths; identifiers are untouched.
public static class ZeroInput
{
    public static readonly DependencyProperty EnabledProperty=DependencyProperty.RegisterAttached("Enabled",typeof(bool),typeof(ZeroInput),new PropertyMetadata(false,Changed));
    public static bool GetEnabled(DependencyObject o)=>(bool)o.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject o,bool v)=>o.SetValue(EnabledProperty,v);
    private static readonly System.Collections.Generic.HashSet<string> Numeric=new(){"DayHours","DykHours","Garnishment","PreviousSchoolDayHours","NewSchoolDayHours","EntryDayHours","EntryDykHours","EntryGarnishment","Coefficient","Indicator","IncomeExemption","StampExemption","EmployeeRate","EmployerRate","IncomeRate","StampRate","DykMultiplier"};
    private static readonly DependencyProperty ClearedProperty=DependencyProperty.RegisterAttached("Cleared",typeof(bool),typeof(ZeroInput));
    private static void Changed(DependencyObject o,DependencyPropertyChangedEventArgs e)
    {
        if(o is not TextBox t)return;
        if((bool)e.NewValue){t.GotKeyboardFocus+=Focused;t.PreviewMouseLeftButtonDown+=Clicked;t.LostKeyboardFocus+=Blurred;}
        else {t.GotKeyboardFocus-=Focused;t.PreviewMouseLeftButtonDown-=Clicked;t.LostKeyboardFocus-=Blurred;}
    }
    private static bool IsNumeric(TextBox t)=>Numeric.Contains(t.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path?.Path??"");
    private static void Clear(TextBox t)
    {
        if(t.IsReadOnly || !t.IsEnabled || !IsNumeric(t))return;
        if(decimal.TryParse(t.Text,System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.GetCultureInfo("tr-TR"),out var v) && v==0)
        {t.SetValue(ClearedProperty,true);t.SetCurrentValue(TextBox.TextProperty,"");}
    }
    private static void Focused(object sender,KeyboardFocusChangedEventArgs e)=>Clear((TextBox)sender);
    private static void Clicked(object sender,MouseButtonEventArgs e)=>Clear((TextBox)sender);
    private static void Blurred(object sender,KeyboardFocusChangedEventArgs e)
    {
        var t=(TextBox)sender;
        if((bool)t.GetValue(ClearedProperty) && string.IsNullOrWhiteSpace(t.Text))t.SetCurrentValue(TextBox.TextProperty,"0");
        t.SetValue(ClearedProperty,false);
    }
}
