namespace CsStatsHub
{
    public partial class App : Application
    {
        internal static IServiceProvider Services { get; set; } = default!;

        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new MainPage());
        }
    }
}
