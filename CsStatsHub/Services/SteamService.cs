using System.Net.Http;
using System.Text.Json;
using CsStatsHub.Models;

namespace CsStatsHub.Services;

/// <summary>
/// Steam Web API wrapper for CS2 (AppId 730).
/// </summary>
public sealed class SteamService
{
    /// <summary>Replace with your Steam Web API key from https://steamcommunity.com/dev/apikey</summary>
    public const string SteamWebApiKey = "C4CCEDD263FFF5B2D9A2714367AB378F";

    private readonly HttpClient _http;
    private readonly string _apiKey;

    public SteamService(HttpClient? httpClient = null, string? apiKey = null)
    {
        _http = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _apiKey = string.IsNullOrWhiteSpace(apiKey) ? SteamWebApiKey : apiKey;
    }

    public async Task<PlayerProfileApi?> GetPlayerProfileAsync(string steamId, CancellationToken ct = default)
    {
        var url = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={Uri.EscapeDataString(_apiKey)}&steamids={Uri.EscapeDataString(steamId)}";
        await using var stream = await _http.GetStreamAsync(url, ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        if (!doc.RootElement.TryGetProperty("response", out var response))
            return null;
        if (!response.TryGetProperty("players", out var players) || players.GetArrayLength() == 0)
            return null;

        var p = players[0];
        return new PlayerProfileApi(
            p.TryGetProperty("personaname", out var n) ? n.GetString() ?? steamId : steamId,
            p.TryGetProperty("avatarfull", out var a) ? a.GetString() ?? string.Empty : string.Empty);
    }

    public async Task<CsGameStatsApi?> GetCSStatsAsync(string steamId, CancellationToken ct = default)
    {
        var url = $"https://api.steampowered.com/ISteamUserStats/GetUserStatsForGame/v0002/?appid=730&key={Uri.EscapeDataString(_apiKey)}&steamid={Uri.EscapeDataString(steamId)}";
        using var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            return null;

        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        if (!doc.RootElement.TryGetProperty("playerstats", out var ps))
            return null;
        if (!ps.TryGetProperty("stats", out var statsEl))
            return null;

        double kills = 0, deaths = 1, hs = 0;
        long wins = 0, matches = 0;

        foreach (var item in statsEl.EnumerateArray())
        {
            if (!item.TryGetProperty("name", out var nameEl))
                continue;
            var name = nameEl.GetString();
            if (!item.TryGetProperty("value", out var val))
                continue;
            var v = val.GetDouble();

            switch (name)
            {
                case "total_kills": kills = v; break;
                case "total_deaths": deaths = v <= 0 ? 1 : v; break;
                case "total_kills_headshot": hs = v; break;
                case "total_matches_played": matches = (long)v; break;
                case "total_wins": wins = (long)v; break;
            }
        }

        var kd = deaths <= 0 ? kills : kills / deaths;
        var hsRate = kills <= 0 ? 0 : (hs / kills) * 100.0;
        var winRate = matches <= 0 ? 0 : (wins / (double)matches) * 100.0;

        return new CsGameStatsApi(kills, deaths, hs, wins, matches, kd, hsRate, winRate);
    }

    public async Task<IReadOnlyList<FriendProfileApi>> GetFriendListAsync(string steamId, CancellationToken ct = default)
    {
        var listUrl = $"https://api.steampowered.com/ISteamUser/GetFriendList/v0001/?key={Uri.EscapeDataString(_apiKey)}&steamid={Uri.EscapeDataString(steamId)}&relationship=friend";
        using var resp = await _http.GetAsync(listUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            return Array.Empty<FriendProfileApi>();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        if (!doc.RootElement.TryGetProperty("friendslist", out var fl))
            return Array.Empty<FriendProfileApi>();
        if (!fl.TryGetProperty("friends", out var friends))
            return Array.Empty<FriendProfileApi>();

        var ids = new List<string>();
        foreach (var f in friends.EnumerateArray())
        {
            if (f.TryGetProperty("steamid", out var sid))
            {
                var s = sid.GetString();
                if (!string.IsNullOrEmpty(s))
                    ids.Add(s);
            }
        }

        if (ids.Count == 0)
            return Array.Empty<FriendProfileApi>();

        var result = new List<FriendProfileApi>();
        for (var i = 0; i < ids.Count; i += 100)
        {
            var batch = ids.Skip(i).Take(100).ToArray();
            var joined = string.Join(',', batch);
            var sumUrl = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={Uri.EscapeDataString(_apiKey)}&steamids={Uri.EscapeDataString(joined)}";
            await using var s2 = await _http.GetStreamAsync(sumUrl, ct).ConfigureAwait(false);
            using var doc2 = await JsonDocument.ParseAsync(s2, cancellationToken: ct).ConfigureAwait(false);
            if (!doc2.RootElement.TryGetProperty("response", out var r2))
                continue;
            if (!r2.TryGetProperty("players", out var players))
                continue;

            foreach (var p in players.EnumerateArray())
            {
                var id = p.TryGetProperty("steamid", out var idEl) ? idEl.GetString() ?? string.Empty : string.Empty;
                var name = p.TryGetProperty("personaname", out var n) ? n.GetString() ?? id : id;
                var avatar = p.TryGetProperty("avatarfull", out var av) ? av.GetString() ?? string.Empty : string.Empty;
                var state = p.TryGetProperty("personastate", out var st) ? st.GetInt32() : 0;
                if (!string.IsNullOrEmpty(id))
                    result.Add(new FriendProfileApi(id, name, avatar, state));
            }
        }

        return result.OrderBy(f => f.PersonaName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Simulated recent match rows (Steam has no public match-history API for CS2 like this).</summary>
    public Task<IReadOnlyList<SimulatedMatchApi>> GetRecentMatchesAsync(string steamId, CancellationToken ct = default)
    {
        var maps = new[] { "de_inferno", "de_mirage", "de_nuke", "de_anubis", "de_vertigo", "de_ancient", "de_dust2" };
        var seed = StringComparer.Ordinal.GetHashCode(steamId);
        if (seed == int.MinValue)
            seed = 1;
        var rng = new Random(seed ^ 0x5F356495);

        var list = new List<SimulatedMatchApi>(10);
        for (var i = 0; i < 10; i++)
        {
            var map = maps[rng.Next(maps.Length)];
            var my = rng.Next(8, 17);
            var opp = rng.Next(3, 16);
            var win = rng.NextDouble() > 0.45;
            if (!win && my > opp)
                (my, opp) = (opp, my);
            if (win && my < opp)
                (my, opp) = (opp, my);

            var k = rng.Next(12, 35);
            var d = rng.Next(10, 30);
            var a = rng.Next(2, 12);
            list.Add(new SimulatedMatchApi(map, $"{my}:{opp}", win, k, d, a, i));
        }

        return Task.FromResult<IReadOnlyList<SimulatedMatchApi>>(list);
    }
}

public sealed record PlayerProfileApi(string PersonaName, string AvatarFullUrl);

public sealed record CsGameStatsApi(
    double TotalKills,
    double TotalDeaths,
    double HeadshotKills,
    long TotalWins,
    long TotalMatches,
    double KillDeathRatio,
    double HeadshotPercent,
    double WinPercent);

public sealed record FriendProfileApi(string SteamId, string PersonaName, string AvatarUrl, int PersonaState);

public sealed record SimulatedMatchApi(string MapName, string ScoreText, bool IsWin, int Kills, int Deaths, int Assists, int SortIndex);
