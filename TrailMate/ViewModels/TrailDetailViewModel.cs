using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TrailMate.Models;
using TrailMate.Services;
using System.Collections.ObjectModel;

namespace TrailMate.ViewModels;

[QueryProperty(nameof(TrailId), "trailId")]
public partial class TrailDetailViewModel : BaseViewModel
{
    private readonly DatabaseService _db;

    [ObservableProperty] private int _trailId;
    [ObservableProperty] private TrailEntry? _trail;
    [ObservableProperty] private bool _hasPhotos;

    public ObservableCollection<string> Photos { get; } = new();

    public TrailDetailViewModel(DatabaseService db)
    {
        _db = db;
        Title = "Trail Details";
    }

    partial void OnTrailIdChanged(int value) =>
        LoadTrailCommand.ExecuteAsync(null);

    [RelayCommand]
    private async Task LoadTrailAsync()
    {
        if (TrailId <= 0) return;
        try
        {
            IsBusy = true;
            Trail = await _db.GetTrailByIdAsync(TrailId);
            if (Trail is null) return;

            Photos.Clear();
            foreach (var p in Trail.PhotoPathList)
                if (File.Exists(p)) Photos.Add(p);

            HasPhotos = Photos.Count > 0;
            Title = Trail.Name;
        }
        catch (Exception ex) { SetError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SpeakSummaryAsync()
    {
        if (Trail is null) return;
        var text = $"Trail: {Trail.Name}. "
                 + $"Distance: {Trail.DistanceKm:F1} kilometres. "
                 + $"Duration: {Trail.DurationMinutes} minutes. "
                 + $"Steps: {Trail.StepCount}. "
                 + $"Recorded on {Trail.StartedAt:dd MMMM yyyy}.";
        await TextToSpeech.Default.SpeakAsync(text);
    }

    [RelayCommand]
    private async Task DeleteTrailAsync()
    {
        if (Trail is null) return;

        bool ok = await Shell.Current.DisplayAlert(
            "Delete Trail",
            $"Delete '{Trail.Name}'? This cannot be undone.",
            "Delete", "Cancel");
        if (!ok) return;

        try
        {
            foreach (var p in Trail.PhotoPathList)
                if (File.Exists(p)) File.Delete(p);

            await _db.DeleteTrailAsync(Trail);
            try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex) { SetError(ex.Message); }
    }
}