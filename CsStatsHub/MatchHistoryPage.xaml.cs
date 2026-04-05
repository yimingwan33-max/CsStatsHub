using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Graphics;
using CsStatsHub.Data;
using CsStatsHub.Models;
using CsStatsHub.Services;

namespace CsStatsHub;

public partial class MatchHistoryPage : ContentPage
{
    private readonly SteamService _steam;
    private readonly LocalDatabase _db;
    public ObservableCollection<MatchHistoryRow> Rows { get; } = new();

    public MatchHistoryPage()
    {
        InitializeComponent();
        _steam = App.Services.GetRequiredService<SteamService>();
        _db = App.Services.GetRequiredService<LocalDatabase>();
        MatchesList.ItemsSource = Rows;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var steamId = ProfileSession.ResolveViewingSteamId();
        if (string.IsNullOrWhiteSpace(steamId))
            return;

        Rows.Clear();
        foreach (var m in _db.GetMatches(steamId))
            Rows.Add(ToRow(m));

        try
        {
            var sim = await _steam.GetRecentMatchesAsync(steamId).ConfigureAwait(true);
            var rows = sim.Select(x => new MatchCacheRow(x.MapName, x.ScoreText, x.IsWin, x.Kills, x.Deaths, x.Assists, x.SortIndex)).ToList();
            _db.ReplaceMatches(steamId, rows);

            Rows.Clear();
            foreach (var m in _db.GetMatches(steamId))
                Rows.Add(ToRow(m));
        }
        catch
        {
            // keep cached rows
        }
    }

    private static MatchHistoryRow ToRow(MatchCacheRow m)
    {
        var winColor = Color.FromRgb(61, 214, 140);
        var lossColor = Color.FromRgb(255, 107, 107);

        return new MatchHistoryRow
        {
            MapName = m.MapName,
            MapThumbUrl = MatchUi.MapThumbUrl(m.MapName),
            ScoreText = m.ScoreText,
            KdaText = $"K/D/A {m.Kills}/{m.Deaths}/{m.Assists}",
            ResultText = m.IsWin ? "胜" : "负",
            ResultColor = m.IsWin ? winColor : lossColor,
            IsWin = m.IsWin
        };
    }
}
