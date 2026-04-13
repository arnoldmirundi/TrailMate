using TrailMate.Models;

namespace TrailMate.Services;
/// Wraps MAUI's Geolocation API to provide current location and
/// simple distance calculations between GPS coordinates.

public class LocationService
{
    /// Requests the current GPS location with balanced accuracy.
    /// Returns null if permission is denied or location unavailable.
    public async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                return null;

            var request = new GeolocationRequest(GeolocationAccuracy.Medium,
                                                 TimeSpan.FromSeconds(10));
            return await Geolocation.Default.GetLocationAsync(request);
        }
        catch (FeatureNotSupportedException)
        {
            // GPS not available on this device
            return null;
        }
        catch (PermissionException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
    /// Calculates the distance in kilometres between two GPS coordinates
    public double CalculateDistance(WaypointModel from, WaypointModel to)
    {
        const double earthRadiusKm = 6371.0;

        double dLat = ToRadians(to.Latitude - from.Latitude);
        double dLon = ToRadians(to.Longitude - from.Longitude);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                 + Math.Cos(ToRadians(from.Latitude))
                 * Math.Cos(ToRadians(to.Latitude))
                 * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}