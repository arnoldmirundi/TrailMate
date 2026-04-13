using TrailMate.ViewModels;

namespace TrailMate.Views;

public partial class GalleryPage : ContentPage
{
    private readonly GalleryViewModel _vm;

    public GalleryPage(GalleryViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadGalleryCommand.ExecuteAsync(null);
    }

    protected override bool OnBackButtonPressed()
    {
        if (_vm.IsDetailView)
        {
            _vm.BackToGridCommand.Execute(null);
            return true;
        }
        return base.OnBackButtonPressed();
    }
}