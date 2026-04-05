using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using CsStatsHub.Data;
using CsStatsHub.Models;
using CsStatsHub.Services;

namespace CsStatsHub;

public partial class ProfilePage : ContentPage
{
    private readonly SteamService _steam;
    private readonly LocalDatabase _db;

    public ProfilePage()
    {
        InitializeComponent();
        _steam = App.Services.GetRequiredService<SteamService>();
        _db = App.Services.GetRequiredService<LocalDatabase>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var target = ProfileSession.ResolveViewingSteamId();
        SteamIdLabel.Text = string.IsNullOrWhiteSpace(target) ? "—" : $"SteamID: {target}";

        _ = LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        var target = ProfileSession.ResolveViewingSteamId();
        if (string.IsNullOrWhiteSpace(target))
        {
            ApplyEmptyState("Not signed in");
            return;
        }

        var cached = _db.GetPlayer(target);
        if (cached != null)
            ApplyFromCache(cached);

        var cachedMatches = _db.GetMatches(target);
        ApplyLastMatch(cachedMatches.FirstOrDefault());

        SyncHintLabel.Text = cached == null ? "Fetching data from Steam…" : "Syncing in the background…";

        try
        {
            await RefreshFromNetworkAsync(target).ConfigureAwait(true);
        }
        catch
        {
            if (cached == null)
                ApplyEmptyState("Unable to load data from Steam. Check your network or API key.");
            SyncHintLabel.Text = "Sync failed. Showing cached data.";
        }
    }

    private void ApplyEmptyState(string message)
    {
        PersonaNameLabel.Text = message;
        TierBadgeLabel.Text = "—";
        TierNameLabel.Text = string.Empty;
        KdValueLabel.Text = "—";
        HsValueLabel.Text = "—";
        WinValueLabel.Text = "—";
        LastMapLabel.Text = "—";
        LastScoreLabel.Text = string.Empty;
        LastKdaLabel.Text = string.Empty;
        AvatarImage.Source = null;
        LastMapImage.Source = null;
    }

    private void ApplyFromCache(PlayerCacheRow row)
    {
        PersonaNameLabel.Text = string.IsNullOrWhiteSpace(row.PersonaName) ? row.SteamId : row.PersonaName;

        var deaths = row.Deaths <= 0 ? 1 : row.Deaths;
        var kd = row.Kills / deaths;
        var hsRate = row.Kills <= 0 ? 0 : (row.HeadshotKills / row.Kills) * 100.0;
        var winRate = row.MatchesPlayed <= 0 ? 0 : (row.Wins / (double)row.MatchesPlayed) * 100.0;

        KdValueLabel.Text = kd.ToString("0.00", CultureInfo.InvariantCulture);
        HsValueLabel.Text = $"{hsRate:0.0}%";
        WinValueLabel.Text = $"{winRate:0.0}%";

        ApplyTier(winRate, row.MatchesPlayed > 0);

        if (!string.IsNullOrWhiteSpace(row.AvatarUrl))
            AvatarImage.Source = new UriImageSource { Uri = new Uri(row.AvatarUrl), CachingEnabled = true };
        else
            AvatarImage.Source = null;

        SyncHintLabel.Text = $"Cached · {row.UpdatedUtc.ToLocalTime():g}";
    }

    private void ApplyTier(double winRate, bool hasMatches)
    {
        if (!hasMatches)
        {
            TierBadgeLabel.Text = "⚔️";
            TierNameLabel.Text = "Unranked";
            return;
        }

        if (winRate >= 58)
        {
            TierBadgeLabel.Text = "👑";
            TierNameLabel.Text = "Master";
        }
        else if (winRate >= 50)
        {
            TierBadgeLabel.Text = "🎖️";
            TierNameLabel.Text = "Elite";
        }
        else if (winRate >= 42)
        {
            TierBadgeLabel.Text = "🥇";
            TierNameLabel.Text = "Gold";
        }
        else
        {
            TierBadgeLabel.Text = "⚔️";
            TierNameLabel.Text = "Prospect";
        }
    }

    private void ApplyLastMatch(MatchCacheRow? m)
    {
        if (m == null)
        {
            LastMapLabel.Text = "No matches yet";
            LastScoreLabel.Text = string.Empty;
            LastKdaLabel.Text = string.Empty;
            LastMapImage.Source = null;
            return;
        }

        LastMapLabel.Text = m.MapName;
        LastScoreLabel.Text = $"{(m.IsWin ? "Win" : "Loss")} · {m.ScoreText}";
        LastKdaLabel.Text = $"K/D/A {m.Kills}/{m.Deaths}/{m.Assists}";

        LastMapImage.Source = new UriImageSource
        {
            Uri = new Uri(MatchUi.MapThumbUrl(m.MapName)),
            CachingEnabled = true
        };
    }

    private async Task RefreshFromNetworkAsync(string steamId)
    {
        var profile = await _steam.GetPlayerProfileAsync(steamId).ConfigureAwait(false);
        var stats = await _steam.GetCSStatsAsync(steamId).ConfigureAwait(false);
        var sim = await _steam.GetRecentMatchesAsync(steamId).ConfigureAwait(false);

        var now = DateTime.UtcNow;
        var old = _db.GetPlayer(steamId);

        var persona = profile?.PersonaName ?? old?.PersonaName ?? steamId;
        var avatar = profile?.AvatarFullUrl ?? old?.AvatarUrl ?? string.Empty;

        double kills;
        double deaths;
        double hs;
        int wins;
        int matches;

        if (stats != null)
        {
            kills = stats.TotalKills;
            deaths = stats.TotalDeaths <= 0 ? 1 : stats.TotalDeaths;
            hs = stats.HeadshotKills;
            wins = (int)stats.TotalWins;
            matches = (int)stats.TotalMatches;
        }
        else if (old != null)
        {
            kills = old.Kills;
            deaths = old.Deaths <= 0 ? 1 : old.Deaths;
            hs = old.HeadshotKills;
            wins = old.Wins;
            matches = old.MatchesPlayed;
        }
        else
        {
            kills = 0;
            deaths = 1;
            hs = 0;
            wins = 0;
            matches = 0;
        }

        var row = new PlayerCacheRow(steamId, persona, avatar, kills, deaths, hs, wins, matches, now);
        _db.UpsertPlayer(row);

        var matchRows = sim.Select(m => new MatchCacheRow(m.MapName, m.ScoreText, m.IsWin, m.Kills, m.Deaths, m.Assists, m.SortIndex)).ToList();
        _db.ReplaceMatches(steamId, matchRows);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ApplyFromCache(row);
            ApplyLastMatch(matchRows.Count > 0 ? matchRows[0] : null);
            SyncHintLabel.Text = $"Synced · {DateTime.Now:g}";
        }).ConfigureAwait(false);
    }

    private async void OnMatchHistoryClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MatchHistoryPage());
    }

    private async void OnFriendsClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new FriendsPage());
    }
}
