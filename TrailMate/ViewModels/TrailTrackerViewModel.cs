using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TrailMate.Models;
using TrailMate.Services;

namespace TrailMate.ViewModels;

public partial class TrailTrackerViewModel : BaseViewModel
{
    private readonly DatabaseService _db;
    private readonly LocationService _locationSvc;

    private CancellationTokenSource? _trackingCts;
    private DateTime _startTime;
    private WaypointModel? _lastWaypoint;

    public ObservableCollection<WaypointModel> Waypoints { get; } = new();
    public ObservableCollection<string> TrailPhotos { get; } = new();

    [ObservableProperty] private bool _isTracking;
    [ObservableProperty] private string _trailName = string.Empty;
    [ObservableProperty] private string _trailNameError = string.Empty;
    [ObservableProperty] private double _distanceKm;
    [ObservableProperty] private int _stepCount;
    [ObservableProperty] private string _currentLocation = "Tap Start to begin tracking...";
    [ObservableProperty] private string _elapsedTime = "00:00";
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private bool _hasPhotos;
    [ObservableProperty] private int _photoCount;

    public TrailTrackerViewModel(DatabaseService db, LocationService locationSvc)
    {
        _db = db;
        _locationSvc = locationSvc;
        Title = "Trail Tracker";
    }

    // Start 

    [RelayCommand]
    private async Task StartTrackingAsync()
    {
        if (string.IsNullOrWhiteSpace(TrailName))
        {
            TrailNameError = "Please enter a trail name before starting.";
            return;
        }
        TrailNameError = string.Empty;
        ClearError();

        IsTracking = true;
        DistanceKm = 0;
        StepCount = 0;
        _startTime = DateTime.UtcNow;
        _lastWaypoint = null;
        Waypoints.Clear();
        TrailPhotos.Clear();
        HasPhotos = false;
        PhotoCount = 0;

        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        _trackingCts = new CancellationTokenSource();
        StartAccelerometer();
        _ = TrackLocationLoopAsync(_trackingCts.Token);
        _ = UpdateTimerLoopAsync(_trackingCts.Token);
    }

    //  Stop & Save

    [RelayCommand]
    private async Task StopTrackingAsync()
    {
        if (!IsTracking) return;
        _trackingCts?.Cancel();
        IsTracking = false;
        StopAccelerometer();

        try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }

        try
        {
            IsBusy = true;
            var entry = new TrailEntry
            {
                Name = TrailName.Trim(),
                DistanceKm = Math.Round(DistanceKm, 2),
                DurationMinutes = (int)(DateTime.UtcNow - _startTime).TotalMinutes,
                StepCount = StepCount,
                StartedAt = _startTime,
                Notes = Notes.Trim(),
                StartLatitude = Waypoints.FirstOrDefault()?.Latitude ?? 0,
                StartLongitude = Waypoints.FirstOrDefault()?.Longitude ?? 0,
            };
            entry.PhotoPathList = TrailPhotos.ToList();

            await _db.SaveTrailAsync(entry);
            StatusMessage = $"✅ '{entry.Name}' saved ({entry.DistanceKm:F1} km, {entry.StepCount} steps)!";

            TrailName = string.Empty;
            Notes = string.Empty;
            TrailPhotos.Clear();
            HasPhotos = false;
            PhotoCount = 0;
        }
        catch (Exception ex) { SetError($"Could not save: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    // Take Photo mid-trail 

    [RelayCommand]
    private async Task TakeTrailPhotoAsync()
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                SetError("Camera permission required.");
                return;
            }
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                SetError("Camera not supported on this device.");
                return;
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo is null) return;

            var localPath = Path.Combine(FileSystem.AppDataDirectory,
                                         $"trail_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
            using var src = await photo.OpenReadAsync();
            using var dst = File.OpenWrite(localPath);
            await src.CopyToAsync(dst);

            TrailPhotos.Add(localPath);
            HasPhotos = true;
            PhotoCount = TrailPhotos.Count;

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            await TextToSpeech.Default.SpeakAsync("Photo captured.");
        }
        catch (Exception ex) { SetError($"Camera error: {ex.Message}"); }
    }

    // GPS Loop

    private async Task TrackLocationLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var location = await _locationSvc.GetCurrentLocationAsync();
                if (location is not null)
                {
                    var wp = new WaypointModel
                    { Latitude = location.Latitude, Longitude = location.Longitude };

                    if (_lastWaypoint is not null)
                    {
                        var delta = _locationSvc.CalculateDistance(_lastWaypoint, wp);
                        if (delta > 0.005) DistanceKm += delta;
                    }
                    _lastWaypoint = wp;

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Waypoints.Add(wp);
                        CurrentLocation =
                            $"📍 {location.Latitude:F4}°, {location.Longitude:F4}°";
                    });
                }
                await Task.Delay(5000, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(
                    () => CurrentLocation = $"GPS unavailable: {ex.Message}");
            }
        }
    }

    //  Timer

    private async Task UpdateTimerLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var e = DateTime.UtcNow - _startTime;
                ElapsedTime = $"{(int)e.TotalMinutes:D2}:{e.Seconds:D2}";
                await Task.Delay(1000, ct);
            }
            catch (OperationCanceledException) { break; }
        }
    }

    //  Accelerometer

    private bool _stepPeak;

    private void StartAccelerometer()
    {
        try
        {
            if (!Accelerometer.Default.IsSupported) return;
            Accelerometer.Default.ReadingChanged += OnAccelReading;
            Accelerometer.Default.Start(SensorSpeed.UI);
        }
        catch { }
    }

    private void StopAccelerometer()
    {
        try
        {
            if (!Accelerometer.Default.IsSupported) return;
            Accelerometer.Default.ReadingChanged -= OnAccelReading;
            Accelerometer.Default.Stop();
        }
        catch { }
    }

    private void OnAccelReading(object? sender, AccelerometerChangedEventArgs e)
    {
        var a = e.Reading.Acceleration;
        double mag = Math.Sqrt(a.X * a.X + a.Y * a.Y + a.Z * a.Z);
        if (mag > 1.2 && !_stepPeak)
        {
            _stepPeak = true;
            MainThread.BeginInvokeOnMainThread(() => StepCount++);
        }
        else if (mag <= 1.2) _stepPeak = false;
    }
}