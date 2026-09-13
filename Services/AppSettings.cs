namespace Swift.Services;

/// <summary>
/// Small wrapper around Microsoft.Maui.Storage.Preferences so the unit
/// choice and speed limit survive an app restart without needing a
/// database table for two values.
/// </summary>
public static class AppSettings
{
    private const string ImperialKey = "use_imperial_units";
    private const string SpeedLimitKey = "speed_limit_kph";

    /// <summary>True = MPH/miles, false = KPH/kilometers.</summary>
    public static bool UseImperialUnits
    {
        get => Preferences.Default.Get(ImperialKey, false);
        set => Preferences.Default.Set(ImperialKey, value);
    }

    /// <summary>Always stored in km/h regardless of display unit.</summary>
    public static double SpeedLimitKph
    {
        get => Preferences.Default.Get(SpeedLimitKey, 100.0); // ~62 mph default
        set => Preferences.Default.Set(SpeedLimitKey, value);
    }
}
