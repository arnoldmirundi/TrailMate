using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;

namespace TrailMate.ViewModels;

/// ViewModel for the Camera page.
/// Hardware used: Camera (back camera for trail photos).
/// Uses text-to-speech to announce when a photo is taken.
public partial class CameraViewModel : BaseViewModel
{
    [ObservableProperty] private ImageSource? _capturedImage;
    [ObservableProperty] private bool _hasPhoto;
    [ObservableProperty] private string _photoTimestamp = string.Empty;

    public CameraViewModel()
    {
        Title = "Trail Camera";
    }


    /// Opens the device camera to capture a trail photo.
    /// Announces capture via text-to-speech.
  
    [RelayCommand]
    private async Task TakePhotoAsync()
    {
        try
        {
            ClearError();

            // Check camera permission
            var status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                SetError("Camera permission is required to take photos.");
                return;
            }

            if (!MediaPicker.Default.IsCaptureSupported)
            {
                SetError("Camera capture is not supported on this device.");
                return;
            }

            IsBusy = true;

            var photo = await MediaPicker.Default.CapturePhotoAsync();

            if (photo is null)
            {
                // User cancelled — not an error
                StatusMessage = string.Empty;
                return;
            }

            // Save to app's local cache
            var localPath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
            using var sourceStream = await photo.OpenReadAsync();
            using var destStream = File.OpenWrite(localPath);
            await sourceStream.CopyToAsync(destStream);

            CapturedImage = ImageSource.FromFile(localPath);
            HasPhoto = true;
            PhotoTimestamp = $"📸 Taken at {DateTime.Now:HH:mm:ss  dd/MM/yyyy}";

            // Text-to-speech announcement
            await TextToSpeech.Default.SpeakAsync("Photo saved to your trail gallery.");

            // Haptic confirmation
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); }
            catch { }
        }
        catch (PermissionException)
        {
            SetError("Camera permission was denied. Enable it in Settings.");
        }
        catch (Exception ex)
        {
            SetError($"Could not take photo: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }


    /// Opens the device photo library to pick an existing image.

    [RelayCommand]
    private async Task PickPhotoAsync()
    {
        try
        {
            ClearError();
            IsBusy = true;

            var photo = await MediaPicker.Default.PickPhotoAsync();

            if (photo is null) return;

            using var stream = await photo.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var bytes = ms.ToArray();

            CapturedImage = ImageSource.FromStream(() => new MemoryStream(bytes));
            HasPhoto = true;
            PhotoTimestamp = $"🖼️ Selected at {DateTime.Now:HH:mm dd/MM/yyyy}";
        }
        catch (Exception ex)
        {
            SetError($"Could not select photo: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// Clears the currently displayed photo.
    [RelayCommand]
    private void ClearPhoto()
    {
        CapturedImage = null;
        HasPhoto = false;
        PhotoTimestamp = string.Empty;
        ClearError();
    }
}