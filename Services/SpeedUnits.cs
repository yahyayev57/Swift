namespace Swift.Services;

/// <summary>
/// Centralizes every mph/kph/meter/mile conversion so each view doesn't
/// repeat (and risk drifting from) its own copy of the math.
/// </summary>
public static class SpeedUnits
{
    public static double MpsToKph(double mps) => mps * 3.6;
    public static double MpsToMph(double mps) => mps * 2.23694;
    public static double KphToMph(double kph) => kph * 0.621371;
    public static double MphToKph(double mph) => mph / 0.621371;
    public static double MetersToKm(double meters) => meters / 1000.0;
    public static double MetersToMiles(double meters) => meters / 1609.344;

    /// <summary>Current speed (m/s) converted into whichever unit the user has picked.</summary>
    public static double DisplaySpeed(double mps) =>
        AppSettings.UseImperialUnits ? MpsToMph(mps) : MpsToKph(mps);

    public static string UnitLabel => AppSettings.UseImperialUnits ? "MPH" : "KPH";
}
