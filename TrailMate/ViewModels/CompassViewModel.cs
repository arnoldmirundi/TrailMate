using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TrailMate.ViewModels;


/// ViewModel for the Compass page.
/// Hardware used: Compass (Magnetometer) + Text-to-speech direction narration.

public partial class CompassViewModel : BaseViewModel
{
    [ObservableProperty] private double _headingDegrees;
    [ObservableProperty] private string _headingText = "N";
    [ObservableProperty] private string _headingDetail = "North";
    [ObservableProperty] private bool _isCompassActive;
    [ObservableProperty] private double _needleRotation; // For animated needle

    public CompassViewModel()
    {
        Title = "Compass";
    }

    /// Starts listening to compass (magnetometer) readings.
    [RelayCommand]
    private void StartCompass()
    {
        try
        {
            ClearError();

            if (!Compass.Default.IsSupported)
            {
                SetError("Compass is not supported on this device.");
                return;
            }

            if (IsCompassActive) return;

            Compass.Default.ReadingChanged += OnCompassReadingChanged;
            Compass.Default.Start(SensorSpeed.UI);
            IsCompassActive = true;
        }
        catch (Exception ex)
        {
            SetError($"Could not start compass: {ex.Message}");
        }
    }

    /// Stops the compass sensor to conserve battery.
    [RelayCommand]
    private void StopCompass()
    {
        try
        {
            if (!IsCompassActive) return;
            Compass.Default.ReadingChanged -= OnCompassReadingChanged;
            Compass.Default.Stop();
            IsCompassActive = false;
        }
        catch (Exception ex)
        {
            SetError($"Could not stop compass: {ex.Message}");
        }
    }

   
    /// Reads out the current heading using text-to-speech.
    /// Useful accessibility feature for eyes-free navigation.

    [RelayCommand]
    private async Task AnnounceHeadingAsync()
    {
        try
        {
            var speech = $"You are heading {HeadingDetail}, {HeadingDegrees:F0} degrees.";
            await TextToSpeech.Default.SpeakAsync(speech);
        }
        catch (Exception ex)
        {
            SetError($"Text-to-speech error: {ex.Message}");
        }
    }

    private void OnCompassReadingChanged(object? sender, CompassChangedEventArgs e)
    {
        HeadingDegrees = Math.Round(e.Reading.HeadingMagneticNorth, 1);
        NeedleRotation = HeadingDegrees;
        (HeadingText, HeadingDetail) = GetCardinalDirection(HeadingDegrees);
    }

    
    /// Converts a magnetic heading to a cardinal/intercardinal direction label.
    
    private static (string abbreviation, string full) GetCardinalDirection(double degrees)
    {
        return degrees switch
        {
            >= 337.5 or < 22.5 => ("N", "North"),
            >= 22.5 and < 67.5 => ("NE", "North-East"),
            >= 67.5 and < 112.5 => ("E", "East"),
            >= 112.5 and < 157.5 => ("SE", "South-East"),
            >= 157.5 and < 202.5 => ("S", "South"),
            >= 202.5 and < 247.5 => ("SW", "South-West"),
            >= 247.5 and < 292.5 => ("W", "West"),
            >= 292.5 and < 337.5 => ("NW", "North-West"),
            _ => ("N", "North")
        };
    }

    ///Ensure compass is stopped when leaving the page.
    public void OnDisappearing()
    {
        if (IsCompassActive)
            StopCompassCommand.Execute(null);
    }
}