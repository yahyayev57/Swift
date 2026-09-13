using Microsoft.Extensions.Logging;

namespace Swift;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Oswald-Regular.ttf", "OswaldRegular");
                fonts.AddFont("Oswald-Light.ttf", "OswaldLight");
                fonts.AddFont("Oswald-Medium.ttf", "OswaldMedium");
                fonts.AddFont("Oswald-SemiBold.ttf", "OswaldSemiBold");
                fonts.AddFont("Oswald-Bold.ttf", "OswaldBold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
