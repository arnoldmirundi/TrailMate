using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TrailMate.Services;

namespace TrailMate.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly DatabaseService _db;

    private const string KeyDarkMode = "dark_mode";
    private const string KeyFontScale = "font_scale";
    private const string KeyTtsSpeed = "tts_speed";
    private const string KeyTtsEnabled = "tts_enabled";
    private const string KeyUnits = "units_metric";

    [ObservableProperty] private bool _isDarkMode;
    [ObservableProperty] private double _fontScale = 1.0;
    [ObservableProperty] private float _ttsSpeed = 1.0f;
    [ObservableProperty] private bool _ttsEnabled = true;
    [ObservableProperty] private bool _useMetric = true;
    [ObservableProperty] private string _storageInfo = string.Empty;
    [ObservableProperty] private int _totalTrails;

    public SettingsViewModel(DatabaseService db)
    {
        _db = db;
        Title = "Settings";
        LoadPreferences();
    }

    private void LoadPreferences()
    {
        IsDarkMode = Preferences.Default.Get(KeyDarkMode, false);
        FontScale = Preferences.Default.Get(KeyFontScale, 1.0);
        TtsSpeed = Preferences.Default.Get(KeyTtsSpeed, 1.0f);
        TtsEnabled = Preferences.Default.Get(KeyTtsEnabled, true);
        UseMetric = Preferences.Default.Get(KeyUnits, true);
    }

    // ── Dark Mode — applied immediately on toggle ──────────────────────────

    partial void OnIsDarkModeChanged(bool value)
    {
        Application.Current!.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
        Preferences.Default.Set(KeyDarkMode, value);
    }

    // Units toggle 

    partial void OnUseMetricChanged(bool value) =>
        Preferences.Default.Set(KeyUnits, value);

    // TTS Enabled toggle 

    partial void OnTtsEnabledChanged(bool value) =>
        Preferences.Default.Set(KeyTtsEnabled, value);

    //  Save all (font scale + TTS speed need explicit save)

    [RelayCommand]
    private void SaveSettings()
    {
        try
        {
            Preferences.Default.Set(KeyFontScale, FontScale);
            Preferences.Default.Set(KeyTtsSpeed, TtsSpeed);
            StatusMessage = "✅ Settings saved.";
        }
        catch (Exception ex) { SetError(ex.Message); }
    }

    //  TTS Test 

    [RelayCommand]
    private async Task TestTtsAsync()
    {
        if (!TtsEnabled)
        {
            StatusMessage = "Enable text-to-speech first.";
            return;
        }
        try
        {
            var opts = new SpeechOptions { Volume = 1.0f, Pitch = TtsSpeed };
            await TextToSpeech.Default.SpeakAsync(
                "Text to speech is working. TrailMate is ready for your next adventure.", opts);
        }
        catch (Exception ex) { SetError($"TTS error: {ex.Message}"); }
    }

    // Storage Info 

    [RelayCommand]
    private async Task LoadStorageInfoAsync()
    {
        try
        {
            IsBusy = true;
            var trails = await _db.GetAllTrailsAsync();
            TotalTrails = trails.Count;

            long totalBytes = 0;
            foreach (var t in trails)
                foreach (var p in t.PhotoPathList)
                    if (File.Exists(p))
                        totalBytes += new FileInfo(p).Length;

            var mb = totalBytes / 1_048_576.0;
            StorageInfo = $"{TotalTrails} trails · {mb:F1} MB photos stored";
        }
        catch (Exception ex) { SetError(ex.Message); }
        finally { IsBusy = false; }
    }

    // Clear all photos from disk

    [RelayCommand]
    private async Task ClearAllPhotosAsync()
    {
        bool ok = await Shell.Current.DisplayAlert(
            "Clear Photos",
            "Delete all trail photos from this device? Trail records will be kept.",
            "Delete Photos", "Cancel");
        if (!ok) return;

        try
        {
            IsBusy = true;
            var trails = await _db.GetAllTrailsAsync();
            int deleted = 0;
            foreach (var t in trails)
            {
                foreach (var p in t.PhotoPathList)
                    if (File.Exists(p)) { File.Delete(p); deleted++; }
                t.PhotoPathList = new List<string>();
                await _db.SaveTrailAsync(t);
            }
            StatusMessage = $"✅ {deleted} photo(s) deleted.";
            await LoadStorageInfoAsync();
        }
        catch (Exception ex) { SetError(ex.Message); }
        finally { IsBusy = false; }
    }

    // Reset preferences

    [RelayCommand]
    private void ResetDefaults()
    {
        IsDarkMode = false;
        FontScale = 1.0;
        TtsSpeed = 1.0f;
        TtsEnabled = true;
        UseMetric = true;
        SaveSettings();
        StatusMessage = "✅ Settings reset to defaults.";
    }
}