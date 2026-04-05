using Microsoft.Maui.Storage;

namespace CsStatsHub.Services;

/// <summary>
/// In-memory target for which Steam profile is shown (self or a selected friend).
/// </summary>
public static class ProfileSession
{
    public const string SteamIdPreferenceKey = "steam_id";

    public static string? ViewingSteamId { get; set; }

    public static string ResolveViewingSteamId()
    {
        if (!string.IsNullOrWhiteSpace(ViewingSteamId))
            return ViewingSteamId!;
        return Preferences.Get(SteamIdPreferenceKey, string.Empty);
    }

    public static string ResolveLoggedInSteamId() => Preferences.Get(SteamIdPreferenceKey, string.Empty);
}
