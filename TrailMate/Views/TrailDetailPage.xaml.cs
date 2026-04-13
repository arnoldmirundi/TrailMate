using TrailMate.ViewModels;

namespace TrailMate.Views;

public partial class TrailDetailPage : ContentPage
{
    public TrailDetailPage(TrailDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}