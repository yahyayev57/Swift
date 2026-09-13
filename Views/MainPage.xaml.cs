namespace Swift.Views;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        SelectTab(0);
    }

    private void OnSpeedometerTabTapped(object? sender, EventArgs e) => SelectTab(0);
    private void OnStatsTabTapped(object? sender, EventArgs e) => SelectTab(1);
    private void OnHistoryTabTapped(object? sender, EventArgs e) => SelectTab(2);

    private void SelectTab(int index)
    {
        speedometerView.IsVisible = index == 0;
        statsView.IsVisible = index == 1;
        historyView.IsVisible = index == 2;

        if (index == 2)
            historyView.Refresh();

        Highlight(tabSpeedometer, tabSpeedometerLabel, index == 0);
        Highlight(tabStats, tabStatsLabel, index == 1);
        Highlight(tabHistory, tabHistoryLabel, index == 2);
    }

    private static void Highlight(Border tab, Label label, bool selected)
    {
        var accent = (Color)Application.Current!.Resources["AccentBlue"];
        var muted = (Color)Application.Current!.Resources["TextSecondary"];

        if (selected)
        {
            tab.Stroke = accent;
            tab.StrokeThickness = 1.5;
            label.TextColor = accent;
        }
        else
        {
            tab.Stroke = Colors.Transparent;
            tab.StrokeThickness = 0;
            label.TextColor = muted;
        }
    }
}
