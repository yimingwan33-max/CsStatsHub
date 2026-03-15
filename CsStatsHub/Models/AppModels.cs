namespace CsStatsHub.Models
{
    public class PlayerStats
    {
        public string PlayerName { get; set; }
        public string Rank { get; set; }
        public string HeadshotRate { get; set; }
        public string MatchesPlayed { get; set; }
        public double RankProgress { get; set; }
    }

    public class MatchData
    {
        public string MatchTitle { get; set; }
        public string Details { get; set; }
    }

    public class FavoritePlayer
    {
        public string PlayerInfo { get; set; } // e.g., "player 1:PlayerID"
        public string RankInfo { get; set; }   // e.g., "Rank: Gold"
    }
}