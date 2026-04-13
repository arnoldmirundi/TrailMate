using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TrailMate.Models;
using TrailMate.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace TrailMate.ViewModels;


/// ViewModel for the Gallery page.
/// Loads all TrailEntry records that have an associated photo,
/// and supports deleting individual entries.
/// Implements IQueryAttributable so Shell can pass a trail ID
/// for a detail view via ?trailId=X query parameter.

public partial class GalleryViewModel : BaseViewModel, IQueryAttributable
{
    private readonly DatabaseService _db;

    /// al trail entries that have a saved photo, shown in the grid.
    public ObservableCollection<TrailEntry> PhotoTrails { get; } = new();

    ///The trail entry currently selected for detail view. Null = list view.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDetailView))]
    private TrailEntry? _selectedTrail;

    ///True when a trail is selected and the detail panel is shown
    public bool IsDetailView => SelectedTrail is not null;

    ///Total count label displayed in the header
    [ObservableProperty] private string _photoCount = "0 photos";

    public GalleryViewModel(DatabaseService db)
    {
        _db = db;
        Title = "Trail Gallery";
    }

    // ── IQueryAttributable ────────────────────────────────────────────────────

    /// Called by Shell when navigating with ?trailId=X.
    /// Loads the matching trail and opens it in detail view automatically.
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("trailId", out var idObj)
            && int.TryParse(idObj?.ToString(), out var id))
        {
            await LoadGalleryAsync();
            SelectedTrail = PhotoTrails.FirstOrDefault(t => t.Id == id);
        }
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    /// Loads all TrailEntry records that have a non-null PhotoPath.
    /// Called on page appearing and after a delete.
    [RelayCommand]
    public async Task LoadGalleryAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ClearError();

            var all = await _db.GetAllTrailsAsync();

            // Only include trails that have at least one existing photo file on disk.
            var withPhotos = all.Where(t =>
                             t.PhotoPathList.Any(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p)))
                                .ToList();

            PhotoTrails.Clear();
            foreach (var trail in withPhotos)
                PhotoTrails.Add(trail);

            PhotoCount = $"{PhotoTrails.Count} photo{(PhotoTrails.Count == 1 ? "" : "s")}";
        }
        catch (Exception ex)
        {
            SetError($"Could not load gallery: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// Selects a trail entry and switches to detail view.
    [RelayCommand]
    private void SelectTrail(TrailEntry trail)
    {
        SelectedTrail = trail;
        // Haptic feedback on selection
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); }
        catch { }
    }

    /// Returns from detail view back to the photo grid.
    [RelayCommand]
    private void BackToGrid()
    {
        SelectedTrail = null;
    }

    /// Deletes the currently selected trail entry from the database
    /// and removes its cached photo file from disk.
    /// Shows a confirmation alert before proceeding.
    [RelayCommand]
    private async Task DeleteTrailAsync(TrailEntry? trail)
    {
        trail ??= SelectedTrail;
        if (trail is null) return;

        // Confirm with the user before destructive action
        bool confirmed = await Shell.Current.DisplayAlert(
            "Delete Trail",
            $"Are you sure you want to delete '{trail.Name}'? This cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        try
        {
            IsBusy = true;

            // Delete all photo files from cache if they exist
            foreach (var path in trail.PhotoPathList)
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    try { File.Delete(path); } catch { /* Ignore individual file delete errors */ }
                }
            }

            await _db.DeleteTrailAsync(trail);

            // Haptic — confirm deletion
            try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); }
            catch { }

            SelectedTrail = null;
            await LoadGalleryAsync();

            StatusMessage = $"🗑️ '{trail.Name}' deleted.";
        }
        catch (Exception ex)
        {
            SetError($"Could not delete trail: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// Speaks the selected trail's summary aloud using text-to-speech.
    /// Useful accessibility feature for reviewing trail stats hands-free.
    [RelayCommand]
    private async Task SpeakTrailSummaryAsync()
    {
        if (SelectedTrail is null) return;

        try
        {
            var t = SelectedTrail;
            var speech = $"Trail: {t.Name}. "
                       + $"Distance: {t.DistanceKm:F1} kilometres. "
                       + $"Duration: {t.DurationMinutes} minutes. "
                       + $"Steps: {t.StepCount}. "
                       + $"Recorded on {t.StartedAt:dd MMMM yyyy}.";

            await TextToSpeech.Default.SpeakAsync(speech);
        }
        catch (Exception ex)
        {
            SetError($"Text-to-speech error: {ex.Message}");
        }
    }
}