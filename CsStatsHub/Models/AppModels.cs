using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace CsStatsHub.Models;

public class PlayerStats
{
    public string PlayerName { get; set; } = string.Empty;
    public string Rank { get; set; } = string.Empty;
    public string HeadshotRate { get; set; } = string.Empty;
    public string MatchesPlayed { get; set; } = string.Empty;
    public double RankProgress { get; set; }
}

public class MatchData
{
    public string MatchTitle { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public class FavoritePlayer
{
    public string PlayerInfo { get; set; } = string.Empty;
    public string RankInfo { get; set; } = string.Empty;
}

/// <summary>SQLite cache row for Players.</summary>
public sealed record PlayerCacheRow(
    string SteamId,
    string PersonaName,
    string AvatarUrl,
    double Kills,
    double Deaths,
    double HeadshotKills,
    int Wins,
    int MatchesPlayed,
    DateTime UpdatedUtc);

/// <summary>SQLite cache row for Friends.</summary>
public sealed record FriendCacheRow(
    string SteamId,
    string PersonaName,
    string AvatarUrl,
    int PersonaState,
    DateTime UpdatedUtc);

/// <summary>SQLite cache row for Matches.</summary>
public sealed record MatchCacheRow(
    string MapName,
    string ScoreText,
    bool IsWin,
    int Kills,
    int Deaths,
    int Assists,
    int SortIndex);

/// <summary>List row for match history UI.</summary>
public sealed class MatchHistoryRow
{
    public string MapName { get; set; } = string.Empty;
    public string MapThumbUrl { get; set; } = string.Empty;
    public string ScoreText { get; set; } = string.Empty;
    public string KdaText { get; set; } = string.Empty;
    public string ResultText { get; set; } = string.Empty;
    public Color ResultColor { get; set; } = Colors.Gray;
    public bool IsWin { get; set; }
}

/// <summary>Friends list row for UI.</summary>
public sealed class FriendRow
{
    public string SteamId { get; set; } = string.Empty;
    public string PersonaName { get; set; } = string.Empty;
    public ImageSource? AvatarImage { get; set; }
    public string StatusText { get; set; } = string.Empty;
}
