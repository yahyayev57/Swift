using System.Text.Json;
using Swift.Models;

namespace Swift.Services;

/// <summary>
/// Trip history storage backed by a single JSON file in the app's private
/// data directory — no database engine and no extra NuGet package, just
/// System.Text.Json, which ships in the .NET runtime already. Every call
/// reads the whole file, applies the change, and rewrites it; that's
/// intentionally simple and is fine at the scale of a personal trip log
/// (dozens to low hundreds of trips). A phone logging thousands of trips
/// a day would want an indexed database instead.
/// </summary>
public class TripHistoryService
{
    public static TripHistoryService Instance { get; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    private TripHistoryService()
    {
        _filePath = Path.Combine(FileSystem.AppDataDirectory, "trips.json");
    }

    public async Task SaveTripAsync(TripRecord trip)
    {
        await _fileLock.WaitAsync();
        try
        {
            var trips = await ReadAllAsync();
            trip.Id = trips.Count == 0 ? 1 : trips.Max(t => t.Id) + 1;
            trips.Insert(0, trip); // newest first, so the History list needs no extra sort
            await WriteAllAsync(trips);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<List<TripRecord>> GetTripsAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            return await ReadAllAsync();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task DeleteTripAsync(TripRecord trip)
    {
        await _fileLock.WaitAsync();
        try
        {
            var trips = await ReadAllAsync();
            trips.RemoveAll(t => t.Id == trip.Id);
            await WriteAllAsync(trips);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<List<TripRecord>> ReadAllAsync()
    {
        if (!File.Exists(_filePath))
            return new List<TripRecord>();

        await using var stream = File.OpenRead(_filePath);
        var trips = await JsonSerializer.DeserializeAsync<List<TripRecord>>(stream);
        return trips ?? new List<TripRecord>();
    }

    private async Task WriteAllAsync(List<TripRecord> trips)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, trips, JsonOptions);
    }
}
