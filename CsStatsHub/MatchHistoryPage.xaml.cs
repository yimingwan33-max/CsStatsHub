using CsStatsHub.Models;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace CsStatsHub
{
    public partial class MatchHistoryPage : ContentPage
    {
        public ObservableCollection<MatchData> Matches { get; set; }

        public MatchHistoryPage()
        {
            InitializeComponent();

            
            Matches = new ObservableCollection<MatchData>
            {
                new MatchData { MatchTitle = "Match 1: 13:5", Details = "Data: 17/3/9  80%" },
                new MatchData { MatchTitle = "Match 2: 12:13", Details = "Data: 23/18/3  78%" }
            };

            MatchesCollection.ItemsSource = Matches;
        }
    }
}