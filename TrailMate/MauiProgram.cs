using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using TrailMate.Services;
using TrailMate.ViewModels;
using TrailMate.Views;

namespace TrailMate;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<LocationService>();

        // ViewModels
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<TrailTrackerViewModel>();
        builder.Services.AddTransient<CameraViewModel>();
        builder.Services.AddTransient<CompassViewModel>();
        builder.Services.AddTransient<GalleryViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<TrailDetailViewModel>();

        // Pages
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<TrailTrackerPage>();
        builder.Services.AddTransient<CameraPage>();
        builder.Services.AddTransient<CompassPage>();
        builder.Services.AddTransient<GalleryPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<TrailDetailPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}