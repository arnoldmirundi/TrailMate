namespace TrailMate;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        var isDark = Preferences.Default.Get("dark_mode", false);
        UserAppTheme = isDark ? AppTheme.Dark : AppTheme.Light;

        MainPage = new AppShell();
    }
}