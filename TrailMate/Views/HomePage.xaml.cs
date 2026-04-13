using TrailMate.Models;
using TrailMate.ViewModels;

namespace TrailMate.Views;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _vm;

    public HomePage(HomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    // Reload every time the page is navigated back to
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadRecentTrailsCommand.ExecuteAsync(null);
    }

    //  Trail card tapped, Opens detail page

    private async void OnTrailSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not TrailEntry selected)
            return;

        // Clear selection highlight immediately so card doesn't stay blue
        TrailsCollection.SelectedItem = null;

        await _vm.OpenTrailDetailAsync(selected);
    }

    // Delete button tapped 

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        // The Button's BindingContext was set to the TrailEntry in XAML
        if (sender is Button btn && btn.BindingContext is TrailEntry trail)
        {
            await _vm.DeleteTrailAsync(trail);
        }
    }
}