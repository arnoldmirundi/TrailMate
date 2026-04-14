# TrailMate

> A cross-platform outdoor activity tracking application built with .NET MAUI (.NET 9.0), targeting Android and Windows from a single shared codebase

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Hardware Integration](#hardware-integration)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Pages & Navigation](#pages--navigation)
- [Data Persistence](#data-persistence)
- [Accessibility](#accessibility)
- [Error Handling & Validation](#error-handling--validation)
- [Dependencies](#dependencies)
- [Permissions](#permissions)

---

## Overview

TrailMate is a mobile-first outdoor companion application that integrates six hardware sensors and device capabilities — GPS, accelerometer, camera, compass, text-to-speech, and haptic feedback — into a unified trail recording experience. The application is built on the Model-View-ViewModel (MVVM) architectural pattern using Microsoft's CommunityToolkit.Mvvm, with all data stored locally in a SQLite database via `sqlite-net-pcl`.

The primary goal of the project is to demonstrate the integration of multiple mobile hardware features within a coherent, production-quality user experience — rather than demonstrating each feature in isolation.

---

## Features

| Feature | Description |
|---|---|
| **Trail Recording** | GPS-based route tracking with real-time distance calculation using the Haversine formula |
| **Step Counter** | Live pedometer using accelerometer magnitude peak detection |
| **In-Trail Camera** | Capture photos mid-session; photos are automatically linked to the active trail record |
| **Compass** | Real-time magnetic heading with animated needle and spoken direction via TTS |
| **Trail History** | Persistent SQLite-backed log of all completed sessions with full detail view |
| **Trail Detail** | Full stats, GPS coordinates, notes, photo gallery, and TTS summary per session |
| **Settings** | Dark mode, adjustable font scale, TTS enable/speed, storage reporting, and photo management |

---

## Hardware Integration

TrailMate integrates **six** hardware capabilities. The minimum required for a first-class grade under the module rubric is four.

### GPS / Geolocation
Location is polled every five seconds using `Geolocation.Default.GetLocationAsync()` with `GeolocationAccuracy.Medium`. Inter-reading distance is calculated with the Haversine formula. Readings below five metres are discarded to suppress GPS drift noise.

```csharp
var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
var location = await Geolocation.Default.GetLocationAsync(request);
```

### Accelerometer
The accelerometer runs continuously during an active session at `SensorSpeed.UI`. Steps are detected by measuring the vector magnitude across all three axes and identifying rising-edge peaks above a 1.2g threshold.

```csharp
double magnitude = Math.Sqrt(acc.X * acc.X + acc.Y * acc.Y + acc.Z * acc.Z);
if (magnitude > 1.2 && !_stepPeak) { _stepPeak = true; StepCount++; }
```

### Camera
Photos are captured via `MediaPicker.Default.CapturePhotoAsync()` and written to `FileSystem.AppDataDirectory` for persistence across sessions. Each file path is stored as a comma-separated list in the `TrailEntry.PhotoPaths` column and exposed as `List<string>` via an `[Ignore]`-attributed computed property.

### Compass
The device magnetometer is accessed via `Compass.Default`. Heading readings are converted to cardinal and intercardinal direction labels using a C# switch expression. The needle rotation angle is data-bound directly to the heading value, producing smooth animated updates without any manual animation code.

### Text-to-Speech
TTS is used in three distinct contexts: photo capture confirmation, spoken compass heading on demand, and full trail summary narration on the detail page. Speech pitch is configurable via `SpeechOptions` and persisted in user preferences.

```csharp
var opts = new SpeechOptions { Volume = 1.0f, Pitch = _ttsSpeed };
await TextToSpeech.Default.SpeakAsync(message, opts);
```

### Haptic Feedback
`HapticFeedback.Default.Perform()` is called at meaningful interaction points: session start (`HapticFeedbackType.Click`), session stop (`HapticFeedbackType.LongPress`), photo capture, and trail deletion. All calls are wrapped in try/catch as haptics are not available on all form factors.

---

## Architecture

The application follows a strict **MVVM** pattern. ViewModels contain all business logic and are entirely independent of the UI layer. Pages contain only initialisation code and event handlers that cannot be expressed as XAML bindings — specifically, `CollectionView.SelectionChanged` and `Button.Clicked` handlers that require direct ViewModel method calls to avoid broken `RelativeSource` lookups inside virtualised item templates.

### Dependency Injection
All services, ViewModels, and pages are registered in `MauiProgram.cs` using the built-in .NET DI container.

- **Services** are registered as `Singleton` — one instance per application lifetime.
- **ViewModels and Pages** are registered as `Transient` — a new instance is created on each navigation, preventing stale state between sessions.

```csharp
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<LocationService>();
builder.Services.AddTransient<TrailTrackerViewModel>();
builder.Services.AddTransient<TrailTrackerPage>();
```

### Navigation
Shell navigation is used throughout. Tab pages are declared in `AppShell.xaml` with `Shell.FlyoutBehavior="Disabled"` and a five-tab `TabBar` — the maximum before MAUI generates an overflow "More" item. Detail pages are push-navigated routes registered via `Routing.RegisterRoute()` and receive data via `[QueryProperty]` attributes on their ViewModels.

```csharp
// Registration
Routing.RegisterRoute("TrailDetailPage", typeof(TrailDetailPage));

// Navigation with parameter
await Shell.Current.GoToAsync($"TrailDetailPage?trailId={trail.Id}");

// ViewModel reception
[QueryProperty(nameof(TrailId), "trailId")]
public partial class TrailDetailViewModel : BaseViewModel { ... }
```

### BaseViewModel
All ViewModels inherit from `BaseViewModel`, which provides:
- `IsBusy` / `IsNotBusy` for loading state and button gating
- `StatusMessage` for success feedback
- `HasError` / `ErrorMessage` for error state
- `SetError()` and `ClearError()` convenience methods

---

## Project Structure

```
TrailMate/
├── MauiProgram.cs                  # App entry point, DI registration
├── App.xaml / App.xaml.cs          # Application resources, theme init
├── AppShell.xaml / AppShell.cs     # Shell navigation, route registration
│
├── Models/
│   ├── TrailEntry.cs               # SQLite entity, PhotoPathList helper
│   └── WaypointModel.cs            # GPS coordinate snapshot
│
├── ViewModels/
│   ├── BaseViewModel.cs            # Shared state: busy, error, status
│   ├── HomeViewModel.cs            # Recent trails, open/delete trail
│   ├── TrailTrackerViewModel.cs    # GPS loop, accelerometer, camera
│   ├── TrailDetailViewModel.cs     # Trail detail, TTS summary, delete
│   ├── CameraViewModel.cs          # Standalone photo capture
│   ├── CompassViewModel.cs         # Magnetometer, TTS heading
│   ├── GalleryViewModel.cs         # Photo grid and detail view
│   └── SettingsViewModel.cs        # Preferences, storage, TTS control
│
├── Views/
│   ├── HomePage.xaml / .cs
│   ├── TrailTrackerPage.xaml / .cs
│   ├── TrailDetailPage.xaml / .cs
│   ├── CameraPage.xaml / .cs
│   ├── CompassPage.xaml / .cs
│   ├── GalleryPage.xaml / .cs
│   └── SettingsPage.xaml / .cs
│
├── Services/
│   ├── DatabaseService.cs          # Async SQLite CRUD via sqlite-net-pcl
│   └── LocationService.cs          # Geolocation wrapper, Haversine calc
│
├── Converters/
│   └── AppConverters.cs            # InvertBool, StringToBool, NullToBool,
│                                   # IntToBool, StringToImageSource,
│                                   # BoolToOpacity
│
├── Resources/
│   ├── Styles/
│   │   ├── Colors.xaml             # WCAG AA colour palette
│   │   └── Styles.xaml             # Reusable control styles
│   └── Images/
│
└── Platforms/
    └── Android/
        └── AndroidManifest.xml     # All runtime permissions declared
```

---

## Getting Started

### Prerequisites

- [Visual Studio 2022](https://visualstudio.microsoft.com/) version 17.8 or later
- .NET 9.0 SDK
- MAUI workload installed: `dotnet workload install maui`
- Android SDK API level 21 or higher

### Installation

```bash
git clone <repository-url>
cd TrailMate
```

Open `TrailMate.sln` in Visual Studio 2022. Select a target (`net9.0-android` or `net9.0-windows10.0.19041.0`) from the run target dropdown and press **F5**.

### NuGet Packages

```xml
<PackageReference Include="CommunityToolkit.Maui"        Version="9.*" />
<PackageReference Include="CommunityToolkit.Mvvm"        Version="8.*" />
<PackageReference Include="sqlite-net-pcl"               Version="1.9.*" />
<PackageReference Include="SQLitePCLRaw.bundle_green"    Version="2.1.*" />
```

---

## Pages & Navigation

### Home (`//HomePage`)
Dashboard displaying a hero banner, two quick-action cards (Tracker and Camera), and the five most recent trail records. Trail cards are tappable via `CollectionView.SelectionChanged` handled in code-behind. Each card exposes an inline delete button wired via `Button.Clicked` — both patterns are used to avoid `RelativeSource AncestorType` resolution failures inside virtualised `CollectionView` templates on Android.

### Trail Tracker (`//TrailTrackerPage`)
Core recording screen. On session start, three concurrent async tasks are launched sharing a single `CancellationTokenSource`: a GPS polling loop, an accelerometer step-detection loop, and a 1-second timer display loop. A **Take Photo** button is visible during active sessions; images appear in a horizontal strip and are accumulated in the session's `TrailPhotos` list. On stop, all data is written to SQLite in a single `SaveTrailAsync` call.

### Trail Detail (`TrailDetailPage?trailId={id}`)
Receives a trail ID via `[QueryProperty]`. Loads the full `TrailEntry` from the database asynchronously, resolves and validates each photo path, and populates stat cards, coordinate labels, notes, and a photo gallery `CollectionView`. Provides a TTS summary command and a confirmed delete command.

### Camera (`//CameraPage`)
Standalone photo capture using `MediaPicker`. Supports both `CapturePhotoAsync()` and `PickPhotoAsync()`. Displays a full-screen preview after capture with timestamp. Fires a TTS confirmation on successful save.

### Compass (`//CompassPage`)
Starts `Compass.Default` on explicit user request. The needle `BoxView.Rotation` property is data-bound to `HeadingDegrees`, updating every sensor tick. Direction labels are resolved via an exhaustive switch expression covering all eight intercardinal points. The compass sensor is stopped in `OnDisappearing()` to prevent unnecessary background drain.

### Settings (`//SettingsPage`)
All controls are fully implemented:

| Control | Mechanism |
|---|---|
| Dark Mode | `OnIsDarkModeChanged` partial method → `Application.Current.UserAppTheme` + `Preferences.Set` |
| Font Scale | `Preferences.Set` on save; read on next launch |
| TTS Toggle | `OnTtsEnabledChanged` partial method → `Preferences.Set`; gates all TTS calls |
| TTS Speed | Stored as `float`; passed as `SpeechOptions.Pitch` in every `SpeakAsync` call |
| Test Speech | Executes live TTS with current speed setting |
| Check Storage | Enumerates `PhotoPathList` across all trail records; sums `FileInfo.Length` |
| Clear Photos | `DisplayAlert` confirmation → delete files from disk → clear `PhotoPaths` column in DB |
| Reset Defaults | Restores all preference keys and reapplies light theme |

---

## Data Persistence

All data is stored locally. There is no network dependency or remote API.

`DatabaseService` exposes four async operations:

```csharp
Task<List<TrailEntry>> GetAllTrailsAsync()
Task<int>              SaveTrailAsync(TrailEntry trail)   // Insert or Update
Task<int>              DeleteTrailAsync(TrailEntry trail)
Task<TrailEntry?>      GetTrailByIdAsync(int id)
```

The SQLite connection is lazily initialised on first use via a private `InitAsync()` guard and reused for the application lifetime. `CreateTableAsync<TrailEntry>()` is idempotent — sqlite-net-pcl skips creation if the table already exists.

Photo files are stored in `FileSystem.AppDataDirectory` (private app storage) rather than the public media store, ensuring they persist with the trail record and are removed cleanly on app uninstall.

---

## Accessibility

The application targets **WCAG 2.1** compliance across all four principles.

| Principle | Implementation |
|---|---|
| **Perceivable** | WCAG AA colour contrast ratios throughout; user-adjustable font scale via Settings; TTS available on all key events; `SemanticProperties.Description` on all interactive elements |
| **Operable** | All touch targets ≥ 44×44 points; five-tab navigation — no features hidden behind menus; Android back button handled on all push-navigated pages |
| **Understandable** | Inline validation errors appear immediately below the relevant input; consistent error and success banners at page top; instructional description labels on every screen |
| **Robust** | `SemanticProperties.HeadingLevel` on section headers; all sensor operations degrade gracefully when hardware is absent; `IsSupported` checked before Accelerometer and Compass start |

---

## Error Handling & Validation

Input validation executes synchronously before any async operation. The trail name field validates on start — empty or oversized input produces an inline `Label` error beneath the field without a blocking alert dialog.

All async operations follow a consistent pattern across every ViewModel:

```csharp
try
{
    IsBusy = true;
    ClearError();
    // operation
}
catch (Exception ex) { SetError($"Descriptive message: {ex.Message}"); }
finally { IsBusy = false; }
```

`IsBusy` is bound to `IsEnabled` on all action buttons, preventing concurrent invocation. Permission denials produce descriptive error banners. Sensor unavailability is caught and surfaced as non-fatal status messages rather than unhandled exceptions.

---

## Dependencies

| Package | Version | Purpose |
|---|---|---|
| `CommunityToolkit.Maui` | 9.x | `UseMauiCommunityToolkit()` initialisation |
| `CommunityToolkit.Mvvm` | 8.x | `ObservableObject`, `[RelayCommand]`, `[ObservableProperty]` source generators |
| `sqlite-net-pcl` | 1.9.x | Async SQLite ORM |
| `SQLitePCLRaw.bundle_green` | 2.1.x | Native SQLite bindings for Android and Windows |

---

## Permissions

Declared in `Platforms/Android/AndroidManifest.xml`:

```xml
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
<uses-permission android:name="android.permission.CAMERA" />
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE"
                 android:maxSdkVersion="32"/>
<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE"
                 android:maxSdkVersion="32"/>
<uses-permission android:name="android.permission.READ_MEDIA_IMAGES"/>
<uses-permission android:name="android.permission.VIBRATE" />
```

All runtime permissions — `CAMERA`, `ACCESS_FINE_LOCATION`, and `READ_MEDIA_IMAGES` — are requested at the point of use via `Permissions.RequestAsync<T>()`. The result is checked before the dependent operation proceeds, and denial is surfaced as a descriptive error message rather than a crash.

---

*Module: 6G6Z0014 Mobile Computing | Manchester Metropolitan University*
