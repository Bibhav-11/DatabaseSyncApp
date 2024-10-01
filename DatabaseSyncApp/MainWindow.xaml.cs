using System.Windows;
using DatabaseSyncApp.Services;
using System.Timers;
using DatabaseSyncApp.Context;
using Microsoft.EntityFrameworkCore;
using Timer = System.Timers.Timer;

namespace DatabaseSyncApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Timer syncTimer;
    private double syncInterval;
    
    public MainWindow()
    {
        InitializeComponent();
        
        syncTimer = new Timer();
        syncTimer.Elapsed += OnTimedEvent;
    }

    private async void SyncDatabase(object sender, EventArgs args)
    {
        await FetchAndSync();
    }
    
    private void SetInterval_Click(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(IntervalTextBox.Text, out double seconds))
        {
            syncInterval = seconds * 1000; // Convert to milliseconds
            syncTimer.Interval = syncInterval;
            syncTimer.Start();
        }
        else
        {
            MessageBox.Show("Please enter a valid number.");
        }
    }
    
    private async void OnTimedEvent(object sender, EventArgs e)
    {
        Console.WriteLine("Syncing interval triggered");
        await FetchAndSync();
    }

    public async Task FetchAndSync()
    {
        Dispatcher.Invoke(() =>StatusLabel.Content = "Status: Fetching");
        using var sourceContext = new SourceDbContext();
        var customers = await sourceContext.Customers.Include(x => x.Locations).AsNoTracking().ToListAsync();
        Dispatcher.Invoke(() => CustomerDataGrid.ItemsSource = customers);
        Dispatcher.Invoke(() => StatusLabel.Content = "Status: Syncing");
        await SyncDatabase();
        Dispatcher.Invoke(() => StatusLabel.Content = "Status: Sync Successful");
    }

    private async Task SyncDatabase()
    {
        var syncService = new SyncService();
        await syncService.SyncCustomerAndLocationDatabases();
    }
}