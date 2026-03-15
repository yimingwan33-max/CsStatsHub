using CsStatsHub.Models;

namespace CsStatsHub
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnSearchClicked(object sender, EventArgs e)
        {
            string username = string.IsNullOrWhiteSpace(UsernameEntry.Text) ? "Unknown Player" : UsernameEntry.Text;

            // 1. 生成虚拟个人数据
            var mockPlayerStats = new PlayerStats
            {
                PlayerName = username,
                Rank = "Gold",
                HeadshotRate = "K/D: 1.4",
                MatchesPlayed = "182",
                RankProgress = 0.75
            };

            // 2. 导航到 ProfilePage 并传递数据
            await Navigation.PushAsync(new ProfilePage(mockPlayerStats));
        }
    }
}