using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using CsStatsHub.Data;
using CsStatsHub.Models;
using CsStatsHub.Services;

namespace CsStatsHub;

public partial class FriendsPage : ContentPage
{
    private readonly SteamService _steam;
    private readonly LocalDatabase _db;
    public ObservableCollection<FriendRow> Friends { get; } = new();

    public FriendsPage()
    {
        InitializeComponent();
        _steam = App.Services.GetRequiredService<SteamService>();
        _db = App.Services.GetRequiredService<LocalDatabase>();
        FriendsList.ItemsSource = Friends;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var owner = ProfileSession.ResolveLoggedInSteamId();
        if (string.IsNullOrWhiteSpace(owner))
            return;

        Friends.Clear();
        foreach (var f in _db.GetFriends(owner))
            Friends.Add(ToRow(f));

        try
        {
            var apiFriends = await _steam.GetFriendListAsync(owner).ConfigureAwait(true);
            var rows = apiFriends
                .Select(f => new FriendCacheRow(f.SteamId, f.PersonaName, f.AvatarUrl, f.PersonaState, DateTime.UtcNow))
                .ToList();

            _db.ReplaceFriends(owner, rows);

            Friends.Clear();
            foreach (var f in _db.GetFriends(owner))
                Friends.Add(ToRow(f));
        }
        catch
        {
            // keep cache
        }
    }

    private static FriendRow ToRow(FriendCacheRow f) => new()
    {
        SteamId = f.SteamId,
        PersonaName = f.PersonaName,
        AvatarImage = string.IsNullOrWhiteSpace(f.AvatarUrl)
            ? null
            : ImageSource.FromUri(new Uri(f.AvatarUrl)),
        StatusText = StatusForState(f.PersonaState)
    };

    private static string StatusForState(int state) => state switch
    {
        1 => "在线",
        2 => "忙碌",
        3 => "离开",
        4 => "离开",
        5 => "想交易",
        6 => "想开黑",
        _ => "离线"
    };

    private async void OnFriendSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not FriendRow row)
            return;

        FriendsList.SelectedItem = null;

        ProfileSession.ViewingSteamId = row.SteamId;
        await Navigation.PopAsync();
    }
}
