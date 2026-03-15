using CsStatsHub.Models;
using System.Collections.ObjectModel;

namespace CsStatsHub
{
    public partial class FavoritesPage : ContentPage
    {
        public ObservableCollection<FavoritePlayer> FavoritesList { get; set; }

        public FavoritesPage()
        {
            InitializeComponent();

            // 生成虚拟收藏好友数据
            FavoritesList = new ObservableCollection<FavoritePlayer>
            {
                new FavoritePlayer { PlayerInfo = "player 1: PlayerID", RankInfo = "Rank: Gold" },
                new FavoritePlayer { PlayerInfo = "player 2:", RankInfo = "Rank: Silver" },
                new FavoritePlayer { PlayerInfo = "player 3:", RankInfo = "Rank: Master" }
            };

            FavoritesCollection.ItemsSource = FavoritesList;
        }
    }
}