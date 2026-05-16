using System.Drawing;
using System.Windows;
using Forms = System.Windows.Forms;

namespace DeskCat.Services;

public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Func<bool> _isVisible;
    private readonly Action _toggleVisible;
    private readonly Func<bool> _isTopmost;
    private readonly Action _toggleTopmost;
    private readonly Func<bool> _allowWalk;
    private readonly Action _toggleAllowWalk;
    private readonly Func<bool> _startupEnabled;
    private readonly Action _toggleStartup;

    public TrayService(
        Action feed,
        Action play,
        Func<bool> isVisible,
        Action toggleVisible,
        Func<bool> isTopmost,
        Action toggleTopmost,
        Func<bool> allowWalk,
        Action toggleAllowWalk,
        Func<bool> startupEnabled,
        Action toggleStartup,
        Action showAbout,
        Action exit)
    {
        _isVisible = isVisible;
        _toggleVisible = toggleVisible;
        _isTopmost = isTopmost;
        _toggleTopmost = toggleTopmost;
        _allowWalk = allowWalk;
        _toggleAllowWalk = toggleAllowWalk;
        _startupEnabled = startupEnabled;
        _toggleStartup = toggleStartup;

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = LoadCatIcon(),
            Text = "DeskCat",
            Visible = true,
            ContextMenuStrip = BuildMenu(feed, play, showAbout, exit)
        };
        _notifyIcon.DoubleClick += (_, _) => _toggleVisible();
    }

    public void ShowBalloon(string title, string text)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = text;
        _notifyIcon.ShowBalloonTip(2500);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Icon?.Dispose();
        _notifyIcon.Dispose();
    }

    private Forms.ContextMenuStrip BuildMenu(Action feed, Action play, Action showAbout, Action exit)
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Opening += (_, _) =>
        {
            foreach (Forms.ToolStripItem item in menu.Items)
            {
                switch (item.Tag)
                {
                    case "visible":
                        item.Text = _isVisible() ? "隐藏宠物" : "显示宠物";
                        break;
                    case "topmost" when item is Forms.ToolStripMenuItem topmost:
                        topmost.Checked = _isTopmost();
                        break;
                    case "walk" when item is Forms.ToolStripMenuItem walk:
                        walk.Checked = _allowWalk();
                        break;
                    case "startup" when item is Forms.ToolStripMenuItem startup:
                        startup.Checked = _startupEnabled();
                        break;
                }
            }
        };

        menu.Items.Add(new Forms.ToolStripMenuItem("🐱 DeskCat") { Enabled = false });
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(new Forms.ToolStripMenuItem("喂食", null, (_, _) => feed()));
        menu.Items.Add(new Forms.ToolStripMenuItem("逗玩", null, (_, _) => play()));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(new Forms.ToolStripMenuItem("隐藏宠物", null, (_, _) => _toggleVisible()) { Tag = "visible" });
        menu.Items.Add(new Forms.ToolStripMenuItem("置顶显示", null, (_, _) => _toggleTopmost()) { Tag = "topmost", Checked = _isTopmost() });
        menu.Items.Add(new Forms.ToolStripMenuItem("允许行走", null, (_, _) => _toggleAllowWalk()) { Tag = "walk", Checked = _allowWalk() });
        menu.Items.Add(new Forms.ToolStripMenuItem("开机自启", null, (_, _) => _toggleStartup()) { Tag = "startup", Checked = _startupEnabled() });
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(new Forms.ToolStripMenuItem("关于", null, (_, _) => showAbout()));
        menu.Items.Add(new Forms.ToolStripMenuItem("退出", null, (_, _) => exit()));

        return menu;
    }

    private static Icon LoadCatIcon()
    {
        try
        {
            var info = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/cat_icon.ico"));
            if (info?.Stream is not null)
            {
                using var stream = info.Stream;
                using var icon = new Icon(stream);
                return (Icon)icon.Clone();
            }
        }
        catch
        {
            // Fall back to a system icon if the packaged icon cannot be read.
        }

        return (Icon)SystemIcons.Application.Clone();
    }
}
