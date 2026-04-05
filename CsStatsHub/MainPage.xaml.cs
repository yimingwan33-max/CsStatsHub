using System.Web;

namespace CsStatsHub;

public partial class MainPage : ContentPage
{
    
    private const string RedirectUrl = "https://localhost/steamlogin";

    public MainPage()
    {
        InitializeComponent();
    }

    private void OnSteamLoginClicked(object sender, EventArgs e)
    {
        
        LoginUI.IsVisible = false;
        SteamWebView.IsVisible = true;

        
        string openIdUrl = $"https://steamcommunity.com/openid/login?" +
                           $"openid.ns=http://specs.openid.net/auth/2.0&" +
                           $"openid.mode=checkid_setup&" +
                           $"openid.return_to={RedirectUrl}&" +
                           $"openid.realm={RedirectUrl}&" +
                           $"openid.identity=http://specs.openid.net/auth/2.0/identifier_select&" +
                           $"openid.claimed_id=http://specs.openid.net/auth/2.0/identifier_select";

        
        SteamWebView.Source = openIdUrl;
    }

    
    
    private async void OnWebViewNavigating(object sender, WebNavigatingEventArgs e)
    {
        if (e.Url.StartsWith(RedirectUrl))
        {
            e.Cancel = true;
            SteamWebView.IsVisible = false;
            LoginUI.IsVisible = true;

            try
            {
               
                string decodedUrl = System.Net.WebUtility.UrlDecode(e.Url);


                int idIndex = decodedUrl.IndexOf("openid/id/");

                if (idIndex != -1)
                {

                    string realSteamId = decodedUrl.Substring(idIndex + 10, 17);

                    await DisplayAlertAsync("真实登录成功", $"太棒了！获取到真实的 SteamID:\n{realSteamId}", "下一步");

                    await Navigation.PushAsync(new ProfilePage(realSteamId));
                }
                else
                {
                    
                    await DisplayAlertAsync("请截图这个报错", $"未找到ID，返回的链接是:\n{decodedUrl}", "确定");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("解析错误", ex.Message, "确定");
            }
        }
    }
}