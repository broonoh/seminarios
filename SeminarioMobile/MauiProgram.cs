using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SeminarioMobile.Data;
using SeminarioMobile.Services;
using SeminarioMobile.ViewModels;
using SeminarioMobile.Views;

namespace SeminarioMobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddDbContextFactory<AppDbContext>();

        builder.Services.AddSingleton<LocalService>();
        builder.Services.AddSingleton<ClasseService>();
        builder.Services.AddSingleton<IgrejaService>();
        builder.Services.AddSingleton<ObservacaoService>();
        builder.Services.AddSingleton<SeminarioService>();
        builder.Services.AddSingleton<BannerService>();
        builder.Services.AddSingleton<PortalImportService>();

        builder.Services.AddTransient<MaanaimViewModel>();
        builder.Services.AddTransient<ClassesViewModel>();
        builder.Services.AddTransient<IgrejasViewModel>();
        builder.Services.AddTransient<ObservacoesViewModel>();
        builder.Services.AddTransient<SeminariosViewModel>();
        builder.Services.AddTransient<BannerViewModel>();
        builder.Services.AddTransient<ImportarPortalViewModel>();

        builder.Services.AddTransient<MaanaimPage>();
        builder.Services.AddTransient<ClassesPage>();
        builder.Services.AddTransient<IgrejasPage>();
        builder.Services.AddTransient<ObservacoesPage>();
        builder.Services.AddTransient<SeminariosPage>();
        builder.Services.AddTransient<BannerPage>();
        builder.Services.AddTransient<ImportarPortalPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Evita que uma falha assíncrona não observada (ex.: exceção dentro de um
        // [RelayCommand] Task que ninguém aguarda) derrube o app inteiro.
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            System.Diagnostics.Trace.TraceError($"Tarefa não observada: {args.Exception}");
            args.SetObserved();
        };

        var app = builder.Build();

        // Roda em uma thread de pool (Task.Run) para não deixar o await interno
        // tentar voltar para a thread principal, que está bloqueada aqui —
        // isso causaria deadlock (e ANR) na inicialização do app.
        Task.Run(async () =>
        {
            using var scope = app.Services.CreateScope();
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var db = dbFactory.CreateDbContext();
            await DatabaseSeeder.PopularSeNecessarioAsync(db);
        }).GetAwaiter().GetResult();

        return app;
    }
}
