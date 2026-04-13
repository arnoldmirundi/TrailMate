using CommunityToolkit.Mvvm.ComponentModel;

namespace TrailMate.ViewModels;


/// Base class for all ViewModels.
/// All ViewModels inherit this to ensure consistent MVVM structure.

public partial class BaseViewModel : ObservableObject
{
    /// title displayed in the navigation bar.
    [ObservableProperty]
    private string _title = string.Empty;

    /// True while an async operation is running.
   
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    public bool IsNotBusy => !IsBusy;

    /// Status message shown to the user in the UI.
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// True when an error message should be displayed.
    [ObservableProperty]
    private bool _hasError;

    ///Error message text shown in error label.
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// Sets an error state and message in a single call.
    protected void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    ///Clears any active error state.
    protected void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }
}