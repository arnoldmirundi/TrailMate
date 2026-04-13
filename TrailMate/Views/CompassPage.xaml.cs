using TrailMate.ViewModels;

namespace TrailMate.Views;

public partial class CompassPage : ContentPage
{
    private readonly CompassViewModel _vm;

    public CompassPage(CompassViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.OnDisappearing();
    }
}