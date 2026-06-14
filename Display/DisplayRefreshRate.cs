using System.Collections.Generic;
using System.Linq;

namespace TrayDisplay;

static class DisplayRefreshRate
{
    const uint ENUM_CURRENT_SETTINGS = unchecked((uint)-1);
    const uint DM_BITSPERPEL        = 0x00040000;
    const uint DM_PELSWIDTH         = 0x00080000;
    const uint DM_PELSHEIGHT        = 0x00100000;
    const uint DM_DISPLAYFREQUENCY  = 0x00400000;
    const uint CDS_UPDATEREGISTRY   = 0x00000001;

    public class RefreshRateInfo
    {
        public required string DeviceName { get; init; }
        public required int CurrentHz { get; init; }
        public required List<int> AvailableHz { get; init; }
    }

    public static RefreshRateInfo? GetInfo()
    {
        var device = new DISPLAY_DEVICE { cb = System.Runtime.InteropServices.Marshal.SizeOf<DISPLAY_DEVICE>() };
        if (!NativeMethods.EnumDisplayDevices(null, 0, ref device, 0))
            return null;
        string deviceName = device.DeviceName;

        var currentMode = new DEVMODE { dmSize = (ushort)System.Runtime.InteropServices.Marshal.SizeOf<DEVMODE>() };
        if (!NativeMethods.EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref currentMode))
            return null;
        int currentHz = (int)currentMode.dmDisplayFrequency;

        var rates = new SortedSet<int>();
        var mode = new DEVMODE { dmSize = (ushort)System.Runtime.InteropServices.Marshal.SizeOf<DEVMODE>() };
        for (uint i = 0; NativeMethods.EnumDisplaySettings(deviceName, i, ref mode); i++)
        {
            if (mode.dmPelsWidth == currentMode.dmPelsWidth &&
                mode.dmPelsHeight == currentMode.dmPelsHeight &&
                mode.dmBitsPerPel == currentMode.dmBitsPerPel &&
                mode.dmDisplayFrequency > 1)
            {
                rates.Add((int)mode.dmDisplayFrequency);
            }
        }

        if (rates.Count == 0) return null;

        return new RefreshRateInfo
        {
            DeviceName = deviceName,
            CurrentHz = currentHz,
            AvailableHz = rates.ToList()
        };
    }

    public static void Apply(string deviceName, int hz)
    {
        var mode = new DEVMODE { dmSize = (ushort)System.Runtime.InteropServices.Marshal.SizeOf<DEVMODE>() };
        NativeMethods.EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref mode);
        mode.dmDisplayFrequency = (uint)hz;
        mode.dmFields = DM_DISPLAYFREQUENCY | DM_PELSWIDTH | DM_PELSHEIGHT | DM_BITSPERPEL;
        NativeMethods.ChangeDisplaySettingsEx(deviceName, ref mode, System.IntPtr.Zero, CDS_UPDATEREGISTRY, System.IntPtr.Zero);
    }
}
