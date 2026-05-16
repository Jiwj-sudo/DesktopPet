using System.Threading;
using System.Windows;

namespace DeskCat;

public partial class App : System.Windows.Application
{
    private MainWindow? _mainWindow;
    private static Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        // 单实例检测
        const string mutexName = "DeskCat_SingleInstance_Mutex";
        _mutex = new Mutex(true, mutexName, out bool createdNew);

        if (!createdNew)
        {
            // 已有实例在运行，静默退出
            // System.Windows.MessageBox.Show("DeskCat 已经在运行中！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        try
        {
            _mainWindow = new MainWindow();
            MainWindow = _mainWindow;
            _mainWindow.Show();
            System.Diagnostics.Debug.WriteLine($"MainWindow shown at {_mainWindow.Left}, {_mainWindow.Top}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Startup error: {ex}");
            throw;
        }
    }

    internal void ExitApplication()
    {
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mainWindow?.Dispose();
        try
        {
            _mutex?.ReleaseMutex();
        }
        catch { }
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
