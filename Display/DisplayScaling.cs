using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TrayDisplay;

static class DisplayScaling
{
    static readonly int[] ScaleValues = { 100, 125, 150, 175, 200, 225, 250, 300, 350 };

    public record ScaleOption(int Percent, int ScaleRel, bool IsCurrent, bool IsRecommended);

    public class ScalingInfo
    {
        public required DISPLAYCONFIG_PATH_INFO Path { get; init; }
        public required DISPLAYCONFIG_SOURCE_DPI_SCALE_GET Packet { get; init; }
        public required List<ScaleOption> Options { get; init; }
    }

    public static ScalingInfo? GetInfo()
    {
        const uint QDC_ONLY_ACTIVE_PATHS = 2;
        uint pathCount = 0, modeCount = 0;
        if (NativeMethods.GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, ref pathCount, ref modeCount) != 0)
            return null;

        var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
        var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
        if (NativeMethods.QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero) != 0)
            return null;

        var path = paths[0];
        var packet = new DISPLAYCONFIG_SOURCE_DPI_SCALE_GET
        {
            header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
            {
                type = -3,
                size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DPI_SCALE_GET>(),
                adapterId = path.sourceInfo.adapterId,
                id = path.sourceInfo.id
            }
        };
        if (NativeMethods.DisplayConfigGetDeviceInfo(ref packet) != 0)
            return null;

        IntPtr monitor = NativeMethods.MonitorFromPoint(new POINT(0, 0), 1);
        NativeMethods.GetDpiForMonitor(monitor, 0, out uint liveDpi, out _);
        int currentPct = (int)(liveDpi * 100 / 96);
        int currentIdx = FindClosest(ScaleValues, currentPct);
        int recommendedIdx = currentIdx - packet.curScaleRel;
        int minIdx = Math.Max(0, recommendedIdx + packet.minScaleRel);
        int maxIdx = Math.Min(ScaleValues.Length - 1, recommendedIdx + packet.maxScaleRel);

        var options = new List<ScaleOption>();
        for (int i = minIdx; i <= maxIdx; i++)
        {
            options.Add(new ScaleOption(
                Percent: ScaleValues[i],
                ScaleRel: i - recommendedIdx,
                IsCurrent: i == currentIdx,
                IsRecommended: i == recommendedIdx));
        }

        return new ScalingInfo { Path = path, Packet = packet, Options = options };
    }

    public static void Apply(ScalingInfo info, int scaleRel)
    {
        var setPacket = new DISPLAYCONFIG_SOURCE_DPI_SCALE_SET
        {
            header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
            {
                type = -4,
                size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DPI_SCALE_SET>(),
                adapterId = info.Path.sourceInfo.adapterId,
                id = info.Path.sourceInfo.id
            },
            scaleRel = scaleRel
        };
        NativeMethods.DisplayConfigSetDeviceInfo(ref setPacket);
    }

    static int FindClosest(int[] arr, int value)
    {
        int best = 0;
        for (int i = 1; i < arr.Length; i++)
            if (Math.Abs(arr[i] - value) < Math.Abs(arr[best] - value)) best = i;
        return best;
    }
}
