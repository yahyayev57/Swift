using Swift.Services;

namespace Swift.Views;

public partial class StatsView : ContentView
{
    private readonly TripTrackingService _tracking = TripTrackingService.Instance;
    private readonly IDispatcherTimer _refreshTimer;

    public StatsView()
    {
        InitializeComponent();

        _tracking.Updated += (_, _) => RefreshUi();

        _refreshTimer = Dispatcher.CreateTimer();
        _refreshTimer.Interval = TimeSpan.FromMilliseconds(500);
        _refreshTimer.Tick += (_, _) => RefreshUi();
        _refreshTimer.Start();

        RefreshUi();
    }

    private void RefreshUi()
    {
        var unit = SpeedUnits.UnitLabel;
        var distanceUnit = AppSettings.UseImperialUnits ? "mi" : "km";

        speedLabel.Text = Math.Round(SpeedUnits.DisplaySpeed(_tracking.CurrentSpeedMps)).ToString();
        avgSpeedLabel.Text = $"{Math.Round(SpeedUnits.DisplaySpeed(_tracking.AverageSpeedMps))} {unit}";

        var limitDisplay = AppSettings.UseImperialUnits
            ? SpeedUnits.KphToMph(AppSettings.SpeedLimitKph)
            : AppSettings.SpeedLimitKph;
        speedLimitLabel.Text = $"{Math.Round(limitDisplay)} {unit}";

        maxSpeedLabel.Text = $"{Math.Round(SpeedUnits.DisplaySpeed(_tracking.MaxSpeedMps))} {unit}";

        var distance = AppSettings.UseImperialUnits
            ? SpeedUnits.MetersToMiles(_tracking.DistanceMeters)
            : SpeedUnits.MetersToKm(_tracking.DistanceMeters);
        distanceLabel.Text = $"{distance:0.00} {distanceUnit}";

        elapsedLabel.Text = _tracking.Elapsed.ToString(@"hh\:mm\:ss");

        headingLabel.Text = _tracking.HeadingDegrees is double h
            ? $"{TripTrackingService.HeadingToCardinal(h)} ({Math.Round(h)}°)"
            : "—";
    }

    private async void OnSpeedLimitTapped(object? sender, EventArgs e)
    {
        var unit = SpeedUnits.UnitLabel;
        var currentInDisplayUnit = AppSettings.UseImperialUnits
            ? SpeedUnits.KphToMph(AppSettings.SpeedLimitKph)
            : AppSettings.SpeedLimitKph;

        var result = await Application.Current!.MainPage!.DisplayPromptAsync(
            "Speed limit",
            $"Enter your speed limit in {unit}",
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
}
