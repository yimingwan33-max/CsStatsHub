namespace CsStatsHub
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // 这里是核心修改：把默认的 AppShell 替换成 NavigationPage 包裹的 MainPage
            // 这样才能支持页面互相跳转（PushAsync）和返回
            MainPage = new NavigationPage(new MainPage());
        }
    }
}