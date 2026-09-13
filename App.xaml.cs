namespace Swift;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // MainPage is a single page with its own pill-shaped tab bar
        // (see Views/MainPage.xaml) rather than a Shell — that let us match
        // the reference mockup's look exactly instead of fighting Shell's
        // native Android tab bar styling.
        MainPage = new NavigationPage(new Views.MainPage());
    }
}
