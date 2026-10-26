namespace Hakedis.Contracts;

public static class AppIdentity
{
    public const string Name="Ek Ders Hakediş Sistemi";
    public const string ShortName="Ek Ders Hakediş Sistemi";
    public const string FolderName="Ek Ders Hakediş Sistemi";
}
public sealed record UiPreferences(int Width=1366,int Height=768);
public sealed record WindowSizeOption(int Width,int Height)
{
    public string Label=>$"{Width} × {Height}";
}
public sealed record WindowSize(double Width,double Height);
public static class WindowSizing
{
    public static WindowSize Fit(UiPreferences preference,double availableWidth,double availableHeight)
    {
        if(availableWidth<=0 || availableHeight<=0)throw new ArgumentException("Kullanılabilir ekran alanı geçersiz.");
        return new(Math.Min(preference.Width,availableWidth),Math.Min(preference.Height,availableHeight));
    }
}
