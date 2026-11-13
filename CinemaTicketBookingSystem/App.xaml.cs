using System.Configuration;
using System.Data;
using System.Windows;

namespace CinemaTicketBookingSystem
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                Console.WriteLine("Unhandled Exception: " + args.ExceptionObject);
                System.IO.File.WriteAllText("crash.log", args.ExceptionObject.ToString());
            };

            DispatcherUnhandledException += (s, args) =>
            {
                Console.WriteLine("Dispatcher Exception: " + args.Exception);
                System.IO.File.WriteAllText("crash.log", args.Exception.ToString());
            };

            base.OnStartup(e);

            Console.WriteLine("App.OnStartup: base.OnStartup completed.");
        }
    }
}
