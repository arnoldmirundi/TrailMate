namespace TrailMate;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        
        Routing.RegisterRoute("GalleryPage", typeof(TrailMate.Views.GalleryPage));
        Routing.RegisterRoute("TrailDetailPage", typeof(TrailMate.Views.TrailDetailPage));
    }
}