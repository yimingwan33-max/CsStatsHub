using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using CsStatsHub.Data;
using CsStatsHub.Services;
using SQLitePCL;

namespace CsStatsHub
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            Batteries_V2.Init();

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton(_ =>
            {
                var db = new LocalDatabase();
                db.Initialize();
                return db;
            });
            builder.Services.AddSingleton<SteamService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            App.Services = app.Services;
            return app;
        }
    }
}
