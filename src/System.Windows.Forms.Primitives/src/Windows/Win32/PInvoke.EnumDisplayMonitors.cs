﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Windows.Win32;

internal static unsafe partial class PInvoke
{
    public delegate bool EnumDisplayMonitorsCallback(HMONITOR monitor, HDC hdc);

    public static BOOL EnumDisplayMonitors(EnumDisplayMonitorsCallback callBack)
    {
        GCHandle gcHandle = GCHandle.Alloc(callBack);
        try
        {
            return EnumDisplayMonitors(default, (RECT*)null, &EnumDisplayMonitorsNativeCallback, (LPARAM)(nint)gcHandle);
        }
        finally
        {
            gcHandle.Free();
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static BOOL EnumDisplayMonitorsNativeCallback(HMONITOR monitor, HDC hdc, RECT* lprcMonitor, LPARAM lParam)
    {
        return ((EnumDisplayMonitorsCallback)((GCHandle)(nint)lParam).Target!)(monitor, hdc);
    }
}
