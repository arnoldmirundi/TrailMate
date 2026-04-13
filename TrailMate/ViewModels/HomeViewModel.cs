using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TrailMate.Models;
using TrailMate.Services;

namespace TrailMate.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    private readonly DatabaseService _db;

    public ObservableCollection<TrailEntry> RecentTrails { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private int _trailCount;

    public bool IsEmpty => TrailCount == 0;

    public HomeViewModel(DatabaseService db)
    {
        _db = db;
        Title = "Home";
    }

    // ── Load ──────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadRecentTrailsAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            ClearError();
            var trails = await _db.GetAllTrailsAsync();
            RecentTrails.Clear();
            foreach (var t in trails.Take(5))
                RecentTrails.Add(t);
            TrailCount = RecentTrails.Count;
        }
        catch (Exception ex) { SetError($"Could not load trails: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    // ── Open Detail — called from code-behind SelectionChanged ───────────

    public async Task OpenTrailDetailAsync(TrailEntry trail)
    {
        await Shell.Current.GoToAsync($"TrailDetailPage?trailId={trail.Id}");
    }

    // ── Delete — called from code-behind swipe/button ────────────────────

    public async Task DeleteTrailAsync(TrailEntry trail)
    {
        bool ok = await Shell.Current.DisplayAlert(
            "Delete Trail",
            $"Delete '{trail.Name}'? This cannot be undone.",
            "Delete", "Cancel");
        if (!ok) return;

        try
        {
            // Remove photos from disk
            foreach (var p in trail.PhotoPathList)
                if (File.Exists(p)) File.Delete(p);

            await _db.DeleteTrailAsync(trail);

            try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }

            RecentTrails.Remove(trail);
            TrailCount = RecentTrails.Count;
        }
        catch (Exception ex) { SetError($"Could not delete: {ex.Message}"); }
    }

    // ── Quick nav ─────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task GoToTrackerAsync() =>
        await Shell.Current.GoToAsync("//TrailTrackerPage");

    [RelayCommand]
    public async Task GoToCameraAsync() =>
        await Shell.Current.GoToAsync("//CameraPage");
}