using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DeskCat.Services;
using DeskCat.ViewModels;
using System.Windows.Forms;

namespace DeskCat;

public partial class MainWindow : System.Windows.Window, IDisposable
{
    private readonly PetViewModel _viewModel = new();
    private readonly TrayService _trayService;
    private bool _isExiting;
    private bool _isDragging;
    private bool _suppressClick;
    private System.Windows.Point _dragStartMouse;
    private System.Windows.Point _dragStartWindow;
    private DispatcherTimer? _singleClickTimer;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        ContextMenu = BuildContextMenu();

        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        Loaded += OnWindowLoaded;

        _trayService = new TrayService(
            feed: () => Dispatcher.Invoke(_viewModel.Feed),
            play: () => Dispatcher.Invoke(_viewModel.Play),
            isVisible: () => Dispatcher.Invoke(() => IsVisible),
            toggleVisible: () => Dispatcher.Invoke(ToggleVisible),
            isTopmost: () => Dispatcher.Invoke(() => _viewModel.IsTopmost),
            toggleTopmost: () => Dispatcher.Invoke(_viewModel.ToggleTopmost),
            allowWalk: () => Dispatcher.Invoke(() => _viewModel.AllowWalk),
            toggleAllowWalk: () => Dispatcher.Invoke(_viewModel.ToggleAllowWalk),
            startupEnabled: StartupService.IsEnabled,
            toggleStartup: () => Dispatcher.Invoke(ToggleStartup),
            showAbout: () => Dispatcher.Invoke(ShowAbout),
            exit: () => Dispatcher.Invoke(Exit));
    }

    public void Dispose()
    {
        CancelPendingClick();
        _viewModel.Dispose();
        _trayService.Dispose();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExiting)
        {
            e.Cancel = true;
            Hide();
            _trayService.ShowBalloon("DeskCat 还在这里", "双击托盘图标可以重新显示小猫。");
            return;
        }

        _viewModel.Save();
        base.OnClosing(e);
    }

    private ContextMenu BuildContextMenu()
    {
        var menu = new ContextMenu();
        menu.Opened += (_, _) => RefreshContextMenu(menu);

        menu.Items.Add(CreateHeader());
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateItem("喂食", (_, _) => _viewModel.Feed()));
        menu.Items.Add(CreateItem("逗玩", (_, _) => _viewModel.Play()));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateCheckItem("置顶显示", (_, _) => _viewModel.ToggleTopmost(), () => _viewModel.IsTopmost, "topmost"));
        menu.Items.Add(CreateCheckItem("允许行走", (_, _) => _viewModel.ToggleAllowWalk(), () => _viewModel.AllowWalk, "walk"));
        menu.Items.Add(CreateCheckItem("开机自启", (_, _) => ToggleStartup(), StartupService.IsEnabled, "startup"));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateItem("关于", (_, _) => ShowAbout()));
        menu.Items.Add(CreateItem("退出", (_, _) => Exit()));
        return menu;
    }

    private static MenuItem CreateHeader()
    {
        return new MenuItem
        {
            Header = "🐱 DeskCat",
            IsEnabled = false
        };
    }

    private static MenuItem CreateItem(string header, RoutedEventHandler handler)
    {
        var item = new MenuItem { Header = header };
        item.Click += handler;
        return item;
    }

    private static MenuItem CreateCheckItem(string header, RoutedEventHandler handler, Func<bool> isChecked, string tag)
    {
        var item = new MenuItem
        {
            Header = header,
            IsCheckable = true,
            IsChecked = isChecked(),
            Tag = tag
        };
        item.Click += handler;
        item.Loaded += (_, _) => item.IsChecked = isChecked();
        return item;
    }

    private void RefreshContextMenu(ItemsControl menu)
    {
        foreach (var raw in menu.Items)
        {
            if (raw is not MenuItem item)
            {
                continue;
            }

            item.IsChecked = item.Tag switch
            {
                "topmost" => _viewModel.IsTopmost,
                "walk" => _viewModel.AllowWalk,
                "startup" => StartupService.IsEnabled(),
                _ => item.IsChecked
            };
        }
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount > 1)
        {
            CancelPendingClick();
            _viewModel.DoubleClick();
            _suppressClick = true;
            e.Handled = true;
            return;
        }

        _isDragging = false;
        _suppressClick = false;
        _dragStartMouse = GetScreenPoint(e.GetPosition(this));
        _dragStartWindow = new System.Windows.Point(Left, Top);
        CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var current = GetScreenPoint(e.GetPosition(this));
        var delta = current - _dragStartMouse;

        if (!_isDragging && (Math.Abs(delta.X) > 4 || Math.Abs(delta.Y) > 4))
        {
            _isDragging = _viewModel.BeginDrag();
            _suppressClick = !_isDragging;
        }

        if (_isDragging)
        {
            _viewModel.MoveDragged(_dragStartWindow.X + delta.X, _dragStartWindow.Y + delta.Y);
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }

        if (_isDragging)
        {
            _viewModel.EndDrag();
        }
        else if (!_suppressClick)
        {
            ScheduleSingleClick();
        }

        _isDragging = false;
        _suppressClick = false;
        e.Handled = true;
    }

    private void ScheduleSingleClick()
    {
        CancelPendingClick();

        _singleClickTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(System.Windows.Forms.SystemInformation.DoubleClickTime)
        };
        _singleClickTimer.Tick += (_, _) =>
        {
            CancelPendingClick();
            _viewModel.Click();
        };
        _singleClickTimer.Start();
    }

    private void CancelPendingClick()
    {
        if (_singleClickTimer is null)
        {
            return;
        }

        _singleClickTimer.Stop();
        _singleClickTimer = null;
    }

    private System.Windows.Point GetScreenPoint(System.Windows.Point point)
    {
        var screen = PointToScreen(point);
        var source = PresentationSource.FromVisual(this);
        return source?.CompositionTarget is null
            ? screen
            : source.CompositionTarget.TransformFromDevice.Transform(screen);
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel.InitializePosition();
        Left = _viewModel.Left;
        Top = _viewModel.Top;
    }

    private double PetSize => _viewModel.PetSize;

    private void ToggleVisible()
    {
        if (IsVisible)
        {
            Hide();
        }
        else
        {
            Show();
            Activate();
        }
    }

    private void ToggleStartup()
    {
        try
        {
            StartupService.SetEnabled(!StartupService.IsEnabled());
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, $"开机自启设置失败：{ex.Message}", "DeskCat", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ShowAbout()
    {
        System.Windows.MessageBox.Show(
            this,
            "DeskCat 桌面小猫\n\n左键抚摸，拖动抱起，双击观察，右键可以喂食和设置。",
            "关于 DeskCat",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Exit()
    {
        _isExiting = true;
        Close();
        if (System.Windows.Application.Current is App app)
        {
            app.ExitApplication();
        }
    }
}
