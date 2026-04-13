using SQLite;

namespace TrailMate.Models;

[Table("TrailEntries")]
public class TrailEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(100), NotNull]
    public string Name { get; set; } = string.Empty;

    public double DistanceKm { get; set; }
    public int DurationMinutes { get; set; }
    public int StepCount { get; set; }
    public double StartLatitude { get; set; }
    public double StartLongitude { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;


    public string PhotoPaths { get; set; } = string.Empty;

    [Ignore]
    public List<string> PhotoPathList
    {
        get => string.IsNullOrWhiteSpace(PhotoPaths)
               ? new List<string>()
               : PhotoPaths.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        set => PhotoPaths = string.Join(",", value);
    }

    [Ignore]
    public string? ThumbnailPath => PhotoPathList.FirstOrDefault();

    [Ignore]
    public bool HasPhotos => PhotoPathList.Count > 0;
}