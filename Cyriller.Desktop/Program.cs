using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using Cyriller.Desktop.ViewModels;
using Cyriller.Desktop.Views;
using Cyriller.Desktop.Models;
using Avalonia.Markup.Xaml;

namespace Cyriller.Desktop
{
    public class Program
    {
        public static ServiceProvider ServiceProvider { get; set; }

        public static void Main(string[] args)
        {
            SetupCultureInfo();
            SetupExceptionHandling();

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by the visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();

        public static ServiceProvider BuildServiceProvider(Application app)
        {
            ServiceProvider = new ServiceCollection()
                .AddSingleton<Application>(app)
                .AddSingleton<CyrCollectionContainer>()
                .AddTransient<CyrName>()
                .AddTransient<CyrNumber>()

                .AddSingleton<MainWindowViewModel>()
                .AddSingleton<MainWindow>()

                .AddTransient<NounViewModel>()
                .AddTransient<AdjectiveViewModel>()
                .AddTransient<NameViewModel>()
                .AddTransient<NumberViewModel>()
                .AddTransient<PhraseViewModel>()

                .BuildServiceProvider();

            return ServiceProvider;
        }

        private static void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
            };
        }

        private static void LogUnhandledException(Exception exception, string source)
        {
            Logger logger = new Logger();
            logger.LogException(exception, source);
            Environment.Exit(1);
        }

        private static void SetupCultureInfo()
        {
            CultureInfo ci = CultureInfo.GetCultureInfo("ru-RU");

            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;

            CultureInfo.DefaultThreadCurrentCulture = ci;
            CultureInfo.DefaultThreadCurrentUICulture = ci;
        }
    }
}
