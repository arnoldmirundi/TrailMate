using TrailMate.ViewModels;

namespace TrailMate.Views;

public partial class CameraPage : ContentPage
{
    public CameraPage(CameraViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}