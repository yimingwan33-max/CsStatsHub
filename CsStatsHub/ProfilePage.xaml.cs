using CsStatsHub.Models;
using System.Xml;

namespace CsStatsHub
{
    public partial class ProfilePage : ContentPage
    {
        public ProfilePage(PlayerStats stats)
        {
            InitializeComponent();

            
            NameLabel.Text = stats.PlayerName;
            RankLabel.Text = stats.Rank;
            RankProgressBar.Progress = stats.RankProgress;
            KDLabel.Text = stats.HeadshotRate;
            MatchesLabel.Text = stats.MatchesPlayed;
        }

        private async void OnViewMatchHistoryClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MatchHistoryPage());
        }

        private async void OnShowFavoritesClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new FavoritesPage());
        }
    }
}