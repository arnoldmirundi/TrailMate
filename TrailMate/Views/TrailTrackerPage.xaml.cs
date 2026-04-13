using TrailMate.ViewModels;

namespace TrailMate.Views;

public partial class TrailTrackerPage : ContentPage
{
    public TrailTrackerPage(TrailTrackerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}