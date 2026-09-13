using System.Diagnostics;
using Microsoft.Maui.Devices.Sensors;
using Swift.Models;

namespace Swift.Services;

/// <summary>
/// Owns the live trip: GPS listening, distance accumulation, max/average
/// speed, elapsed time, and heading. It's a single shared instance
/// (Instance) rather than something registered in DI, because both the
/// Speedometer and Stats views need to read the exact same in-progress
/// trip at once, and a plain static singleton is the simplest way to
/// guarantee that without wiring up a DI container for two consumers.
/// </summary>
public class TripTrackingService
{
    public static TripTrackingService Instance { get; } = new();

    private TripTrackingService() { }

    private readonly Stopwatch _stopwatch = new();
    private Location? _lastLocation;

    public bool IsTracking { get; private set; }
    public DateTime? StartTime { get; private set; }
    public double DistanceMeters { get; private set; }
    public double MaxSpeedMps { get; private set; }
    public double CurrentSpeedMps { get; private set; }
    public double? HeadingDegrees { get; private set; }

    /// <summary>Distance / elapsed time — a true trip average, not a mean of instantaneous readings.</summary>
    public double AverageSpeedMps =>
        _stopwatch.Elapsed.TotalSeconds > 0
            ? DistanceMeters / _stopwatch.Elapsed.TotalSeconds
            : 0;

    public TimeSpan Elapsed => _stopwatch.Elapsed;

    /// <summary>Raised on every GPS or compass update so views can refresh immediately.</summary>
    public event EventHandler? Updated;

    public async Task<bool> StartAsync()
    {
        if (IsTracking) return true;

        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
            return false;

        Reset();
        IsTracking = true;
        StartTime = DateTime.Now;
        _stopwatch.Start();

        try
        {
            Geolocation.Default.LocationChanged += OnLocationChanged;
            // Reduced from 1 second to 500ms for more responsive speed updates
            var request = new GeolocationListeningRequest(GeolocationAccuracy.Best, TimeSpan.FromMilliseconds(500));
            var listening = await Geolocation.Default.StartListeningForegroundAsync(request);
            if (!listening)
            {
                IsTracking = false;
                _stopwatch.Stop();
                Geolocation.Default.LocationChanged -= OnLocationChanged;
                return false;
            }
        }
        catch
        {
            IsTracking = false;
            _stopwatch.Stop();
            return false;
        }

        try
        {
            // Compass is a nice-to-have while parked; OnLocationChanged prefers
            // GPS bearing once the trip is actually moving (see below).
            Compass.Default.ReadingChanged += OnCompassChanged;
            Compass.Default.Start(SensorSpeed.UI);
        }
        catch
        {
            // Not every device exposes a magnetometer — heading just falls
            // back to "—" until GPS course kicks in once moving.
        }

        return true;
    }

    /// <summary>Stops listening and returns a record of the completed trip.</summary>
    public TripRecord Stop()
    {
        IsTracking = false;
        _stopwatch.Stop();

        Geolocation.Default.LocationChanged -= OnLocationChanged;
        Geolocation.Default.StopListeningForeground();

        try
        {
            Compass.Default.Stop();
            Compass.Default.ReadingChanged -= OnCompassChanged;
        }
        catch
        {
            // Ignore — Stop() on an unstarted/unsupported compass is harmless either way.
        }

        return new TripRecord
        {
            StartTime = StartTime ?? DateTime.Now,
            EndTime = DateTime.Now,
            MaxSpeedKph = SpeedUnits.MpsToKph(MaxSpeedMps),
            AvgSpeedKph = SpeedUnits.MpsToKph(AverageSpeedMps),
            DistanceMeters = DistanceMeters,
            DurationSeconds = _stopwatch.Elapsed.TotalSeconds
        };
    }

    /// <summary>Clears the current trip's numbers without touching saved history.</summary>
    public void Reset()
    {
        DistanceMeters = 0;
        MaxSpeedMps = 0;
        CurrentSpeedMps = 0;
        HeadingDegrees = null;
        _lastLocation = null;
        _stopwatch.Reset();
        Updated?.Invoke(this, EventArgs.Empty);
    }

    private void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        var location = e.Location;

        // A GPS fix with a large accuracy radius is noise, not signal — skip
        // it rather than let it corrupt distance or speed for this tick.
        // Reduced from 30m to 50m to allow more updates while still filtering poor signals
        if (location.Accuracy is > 50) return;

        double? computedSpeedMps = null;

        if (_lastLocation != null)
        {
            var segmentMeters = Location.CalculateDistance(_lastLocation, location, DistanceUnits.Kilometers) * 1000;

            // Guard against the occasional GPS jump reporting an impossible
            // hop, which would otherwise inflate total distance.
            // With 500ms updates, 100m is a reasonable max per tick (200 km/h).
            if (segmentMeters < 100)
            {
                DistanceMeters += segmentMeters;

                var dt = (location.Timestamp - _lastLocation.Timestamp).TotalSeconds;
                if (dt > 0)
                    computedSpeedMps = segmentMeters / dt;
            }
        }

        // Prefer the provider's own speed field — it's usually smoother than
        // a two-point derivative — but not every provider fills it in
        // reliably (some mock/emulator location providers, or the first fix
        // or two on real hardware, report 0 or null even while moving).
        // Fall back to distance/time between our last two fixes so the
        // readout never just flatlines when that happens.
        if (location.Speed is double reportedSpeedMps && reportedSpeedMps > 0)
            CurrentSpeedMps = reportedSpeedMps;
        else if (computedSpeedMps is double computed)
            CurrentSpeedMps = computed;

        if (CurrentSpeedMps > MaxSpeedMps) MaxSpeedMps = CurrentSpeedMps;

        // GPS course is far steadier than the magnetometer once the phone is
        // actually moving in a car (metal chassis + electronics throw the
        // compass off), so heading prefers course over compass above ~1 m/s.
        if (location.Course is double course && CurrentSpeedMps > 1.0)
            HeadingDegrees = course;

        _lastLocation = location;
        Updated?.Invoke(this, EventArgs.Empty);
    }

    private void OnCompassChanged(object? sender, CompassChangedEventArgs e)
    {
        // Only trust the magnetometer while effectively stationary — see the
        // comment in OnLocationChanged for why GPS course wins once moving.
        if (CurrentSpeedMps <= 1.0)
            HeadingDegrees = e.Reading.HeadingMagneticNorth;

        Updated?.Invoke(this, EventArgs.Empty);
    }

    public static string HeadingToCardinal(double degrees)
    {
        string[] directions = { "N", "NE", "E", "SE", "S", "SW", "W", "NW", "N" };
        var normalized = ((degrees % 360) + 360) % 360;
        return directions[(int)Math.Round(normalized / 45.0)];
    }
}
