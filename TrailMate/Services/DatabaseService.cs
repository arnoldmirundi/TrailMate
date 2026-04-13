using SQLite;
using TrailMate.Models;

namespace TrailMate.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _database;

    private static readonly string DbPath =
        Path.Combine(FileSystem.AppDataDirectory, "trailmate.db3");

    private async Task InitAsync()
    {
        if (_database is not null) return;
        _database = new SQLiteAsyncConnection(DbPath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        await _database.CreateTableAsync<TrailEntry>();
    }

    public async Task<List<TrailEntry>> GetAllTrailsAsync()
    {
        await InitAsync();
        return await _database!.Table<TrailEntry>()
                               .OrderByDescending(t => t.StartedAt)
                               .ToListAsync();
    }

    public async Task<int> SaveTrailAsync(TrailEntry trail)
    {
        await InitAsync();
        if (string.IsNullOrWhiteSpace(trail.Name))
            throw new ArgumentException("Trail name cannot be empty.");
        return trail.Id == 0
            ? await _database!.InsertAsync(trail)
            : await _database!.UpdateAsync(trail);
    }

    public async Task<int> DeleteTrailAsync(TrailEntry trail)
    {
        await InitAsync();
        return await _database!.DeleteAsync(trail);
    }

    public async Task<TrailEntry?> GetTrailByIdAsync(int id)
    {
        await InitAsync();
        return await _database!.Table<TrailEntry>()
                               .FirstOrDefaultAsync(t => t.Id == id);
    }
}