using System.Threading;
using System.Windows;
using System.Windows.Threading;
using SecretFix.Services;

namespace SecretFix;

public partial class App : Application
{
    private static Mutex? _singleInstanceMutex;
    private static bool _ownsMutex;
    private readonly AppLogService _log = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        _singleInstanceMutex = new Mutex(true, "SecretFix.App.SingleInstance.v0.3", out var createdNew);
        if (!createdNew)
        {
            Shutdown();
            return;
        }
        _ownsMutex = true;

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        CrosshairOverlayService.Close();
        if (_ownsMutex)
            _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _log.Error("Unhandled UI exception", e.Exception);
        NotificationService.Show("Erro de interface registrado. O aplicativo continuará aberto.");
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            _log.Error("Unhandled domain exception", ex);
        else
            _log.Info($"Unhandled domain exception object: {e.ExceptionObject}");
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _log.Error("Unobserved task exception", e.Exception);
        e.SetObserved();
        NotificationService.Show("Erro assíncrono registrado.");
    }
}
