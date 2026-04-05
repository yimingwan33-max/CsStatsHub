namespace CsStatsHub;

public static class MatchUi
{
    public static string MapThumbUrl(string mapName)
    {
        var seed = Math.Abs(mapName.GetHashCode(StringComparison.Ordinal));
        return $"https://picsum.photos/seed/{seed}/144/88";
    }
}
