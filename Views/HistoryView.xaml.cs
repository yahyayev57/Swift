using Swift.Models;
using Swift.Services;

namespace Swift.Views;

public partial class HistoryView : ContentView
{
    public HistoryView()
    {
        InitializeComponent();
    }

    /// <summary>Called by MainPage whenever the History tab is opened.</summary>
    public async void Refresh()
    {
        var trips = await TripHistoryService.Instance.GetTripsAsync();
        tripsCollectionView.ItemsSource = trips;
    }

    private async void OnDeleteSwipeInvoked(object? sender, EventArgs e)
    {
        if (sender is SwipeItem { BindingContext: TripRecord trip })
        {
            await TripHistoryService.Instance.DeleteTripAsync(trip);
            Refresh();
        }
    }
}
