using System;
using System.Drawing;
using System.Windows.Forms;

namespace TrayDisplay;

class TrayApp : ApplicationContext
{
    readonly NotifyIcon trayIcon;

    public TrayApp()
    {
        var menu = new ContextMenuStrip();

        trayIcon = new NotifyIcon
        {
            Icon = File.Exists("Assets/icon.ico") ? new Icon("Assets/icon.ico") : SystemIcons.Application,
            Text = "TrayDisplay",
            ContextMenuStrip = menu,
            Visible = true
        };

        trayIcon.MouseClick += (s, e) =>
        {
            RebuildMenu(menu);
            typeof(NotifyIcon)
                .GetMethod("ShowContextMenu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(trayIcon, null);
        };
    }

    void RebuildMenu(ContextMenuStrip menu)
    {
        menu.Items.Clear();
        try
        {
            menu.Items.Add(BuildScalingMenu());
            menu.Items.Add(BuildRefreshRateMenu());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) => Application.Exit());
        }
        catch (Exception ex)
        {
            menu.Items.Clear();
            menu.Items.Add($"Error: {ex.Message}").Enabled = false;
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) => Application.Exit());
        }
    }

    static ToolStripMenuItem BuildScalingMenu()
    {
        var scalingMenu = new ToolStripMenuItem("Scaling");
        var info = DisplayScaling.GetInfo();

        if (info == null)
        {
            scalingMenu.DropDownItems.Add("Failed to read scaling info").Enabled = false;
            return scalingMenu;
        }

        foreach (var option in info.Options)
        {
            string label = $"{option.Percent}%";
            if (option.IsRecommended) label += "  (Recommended)";

            var item = new ToolStripMenuItem(label) { Checked = option.IsCurrent };
            item.Click += (s, e) => DisplayScaling.Apply(info, option.ScaleRel);
            scalingMenu.DropDownItems.Add(item);
        }

        return scalingMenu;
    }

    static ToolStripMenuItem BuildRefreshRateMenu()
    {
        var refreshMenu = new ToolStripMenuItem("Refresh Rate");
        var info = DisplayRefreshRate.GetInfo();

        if (info == null)
        {
            refreshMenu.DropDownItems.Add("Failed to read refresh rates").Enabled = false;
            return refreshMenu;
        }

        foreach (int hz in info.AvailableHz)
        {
            int rate = hz;
            var item = new ToolStripMenuItem($"{hz} Hz") { Checked = hz == info.CurrentHz };
            item.Click += (s, e) => DisplayRefreshRate.Apply(info.DeviceName, rate);
            refreshMenu.DropDownItems.Add(item);
        }

        return refreshMenu;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) trayIcon.Visible = false;
        base.Dispose(disposing);
    }
}
