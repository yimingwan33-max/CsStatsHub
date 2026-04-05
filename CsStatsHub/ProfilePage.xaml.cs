using CsStatsHub.Models;
using System.Text.Json; // 用于解析返回的 JSON 数据
using System.Net.Http;

namespace CsStatsHub
{
    public partial class ProfilePage : ContentPage
    {
        private string _steamId;
        // 请在此处填入你申请的 API Key
        private const string ApiKey = "C4CCEDD263FFF5B2D9A2714367AB378F";

        public ProfilePage(string steamId)
        {
            InitializeComponent();
            _steamId = steamId;

            // 初始化界面显示
            NameLabel.Text = "正在连接 Steam...";
            RankLabel.Text = "读取中...";
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // 页面一显示，立刻开始抓取数据
            await FetchRealSteamDataAsync();
        }

        private async Task FetchRealSteamDataAsync()
        {
            // 1. 构造 Steam API 的 URL
            // 获取个人资料（名字、头像）
            string userSummaryUrl = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={ApiKey}&steamids={_steamId}";
            // 获取 CS:GO/CS2 战绩（AppID 730）
            string userStatsUrl = $"https://api.steampowered.com/ISteamUserStats/GetUserStatsForGame/v0002/?appid=730&key={ApiKey}&steamid={_steamId}";

            using HttpClient client = new HttpClient();

            try
            {
                // --- 第一步：抓取玩家基本信息 ---
                var summaryResponse = await client.GetStringAsync(userSummaryUrl);
                using var summaryDoc = JsonDocument.Parse(summaryResponse);
                var player = summaryDoc.RootElement.GetProperty("response").GetProperty("players")[0];

                string playerName = player.GetProperty("personaname").GetString();
                // 如果你有 Image 控件，可以用这个 URL
                string avatarUrl = player.GetProperty("avatarfull").GetString();

                // --- 第二步：抓取战绩数据 ---
                var statsResponse = await client.GetAsync(userStatsUrl);

                if (statsResponse.IsSuccessStatusCode)
                {
                    var statsContent = await statsResponse.Content.ReadAsStringAsync();
                    using var statsDoc = JsonDocument.Parse(statsContent);
                    var allStats = statsDoc.RootElement.GetProperty("playerstats").GetProperty("stats");

                    string kills = "0", deaths = "1", hsKills = "0"; // deaths 默认为 1 防止除以 0

                    foreach (var item in allStats.EnumerateArray())
                    {
                        string name = item.GetProperty("name").GetString();
                        if (name == "total_kills") kills = item.GetProperty("value").ToString();
                        if (name == "total_deaths") deaths = item.GetProperty("value").ToString();
                        if (name == "total_kills_headshot") hsKills = item.GetProperty("value").ToString();
                    }

                    // 计算 K/D
                    double kd = Math.Round(double.Parse(kills) / double.Parse(deaths), 2);

                    // --- 第三步：更新 UI ---
                    MainThread.BeginInvokeOnMainThread(() => {
                        NameLabel.Text = playerName;
                        KDLabel.Text = $"K/D: {kd}";
                        MatchesLabel.Text = $"总击杀: {kills}";
                        RankLabel.Text = "已同步真实数据";
                    });
                }
                else
                {
                    // 状态码不是 200，通常是因为隐私设置
                    MainThread.BeginInvokeOnMainThread(() => {
                        NameLabel.Text = playerName;
                        RankLabel.Text = "隐私设置未公开";
                        DisplayAlertAsync("无法读取战绩", "请在 Steam 隐私设置中将“游戏详情”设为公开。", "明白");
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("网络错误", "无法连接到 Steam 服务器: " + ex.Message, "确定");
            }
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