using Swift.Services;

namespace Swift.Views;

public partial class SpeedometerView : ContentView
{
    private readonly TripTrackingService _tracking = TripTrackingService.Instance;
    private readonly AlertSoundService _alertSound = new();
    private readonly IDispatcherTimer _refreshTimer;
    private bool _wasOverLimit;

    public SpeedometerView()
    {
        InitializeComponent();

        _tracking.Updated += (_, _) => RefreshUi();

        // A timer keeps elapsed time and colors ticking even between GPS
        // fixes, which otherwise only arrive roughly once a second.
        _refreshTimer = Dispatcher.CreateTimer();
        _refreshTimer.Interval = TimeSpan.FromMilliseconds(500);
        _refreshTimer.Tick += (_, _) => RefreshUi();
        _refreshTimer.Start();

        RefreshUi();
    }

    private async void OnStartStopClicked(object? sender, EventArgs e)
    {
        if (!_tracking.IsTracking)
        {
            var started = await _tracking.StartAsync();
            if (!started)
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Location needed",
                    "Swift needs location permission to track your speed.",
                    "OK");
                return;
            }
            startStopButton.Text = "Stop";
        }
        else
        {
            var trip = _tracking.Stop();
            startStopButton.Text = "Start";

            // Skip saving accidental taps with no real movement.
            if (trip.DistanceMeters > 20)
                await TripHistoryService.Instance.SaveTripAsync(trip);
        }

        RefreshUi();
    }

    private void OnResetClicked(object? sender, EventArgs e)
    {
        _tracking.Reset();
        RefreshUi();
    }

    private async void OnSetLimitClicked(object? sender, EventArgs e)
    {
        var currentInDisplayUnit = AppSettings.UseImperialUnits
            ? SpeedUnits.KphToMph(AppSettings.SpeedLimitKph)
            : AppSettings.SpeedLimitKph;

        var result = await Application.Current!.MainPage!.DisplayPromptAsync(
            "Speed limit",
            $"Enter your speed limit in {SpeedUnits.UnitLabel}",
            initialValue: Math.Round(currentInDisplayUnit).ToString(),
            keyboard: Keyboard.Numeric);

        if (double.TryParse(result, out var value) && value > 0)
        {
            AppSettings.SpeedLimitKph = AppSettings.UseImperialUnits
                ? SpeedUnits.MphToKph(value)
                : value;
            RefreshUi();
        }
    }

    private void OnUnitToggleTapped(object? sender, EventArgs e)
    {
        AppSettings.UseImperialUnits = !AppSettings.UseImperialUnits;
        RefreshUi();
    }

    private void RefreshUi()
    {
        var unit = SpeedUnits.UnitLabel;
        unitToggleLabel.Text = unit;
        speedUnitLabel.Text = unit;

        var currentDisplay = SpeedUnits.DisplaySpeed(_tracking.CurrentSpeedMps);
        var avgDisplay = SpeedUnits.DisplaySpeed(_tracking.AverageSpeedMps);
        var limitDisplay = AppSettings.UseImperialUnits
            ? SpeedUnits.KphToMph(AppSettings.SpeedLimitKph)
            : AppSettings.SpeedLimitKph;

        speedLabel.Text = Math.Round(currentDisplay).ToString();
        avgSpeedLabel.Text = Math.Round(avgDisplay).ToString();
        speedLimitLabel.Text = Math.Round(limitDisplay).ToString();

        headingLabel.Text = _tracking.HeadingDegrees is double h
            ? TripTrackingService.HeadingToCardinal(h)
            : "—";

        var overLimit = limitDisplay > 0 && currentDisplay > limitDisplay;
        speedLabel.TextColor = overLimit
            ? (Color)Application.Current!.Resources["StatusWarning"]
            : (Color)Application.Current!.Resources["TextPrimary"];

        // Fire the alert once on the transition into "over limit" rather
        // than every tick, so it doesn't retrigger every 500ms while speeding.
        if (overLimit && !_wasOverLimit)
            _alertSound.PlayAlert();

        _wasOverLimit = overLimit;
    }
}
