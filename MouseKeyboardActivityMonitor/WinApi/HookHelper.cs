﻿// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or http://opensource.org/licenses/mit-license.php

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MouseKeyboardActivityMonitor.WinApi
{
    internal static class HookHelper
    {
        public static HookResult HookAppMouse(Callback callback)
        {
            return HookApp(HookIds.WH_MOUSE, callback);
        }

        public static HookResult HookAppKeyboard(Callback callback)
        {
            return HookApp(HookIds.WH_KEYBOARD, callback);
        }

        public static HookResult HookGlobalMouse(Callback callback)
        {
            return HookGlobal(HookIds.WH_MOUSE_LL, callback);
        }

        public static HookResult HookGlobalKeyboard(Callback callback)
        {
            return HookGlobal(HookIds.WH_KEYBOARD_LL, callback);
        }

        private static HookResult HookApp(int hookId, Callback callback)
        {
            HookProcedure hookProcedure = (code, param, lParam) => HookProcedure(code, param, lParam, callback);

            var hookHandle = HookNativeMethods.SetWindowsHookEx(
                hookId,
                hookProcedure,
                IntPtr.Zero,
                ThreadNativeMethods.GetCurrentThreadId());

            if (hookHandle.IsInvalid)
            {
                ThrowLastUnmanagedErrorAsException();
            }

            return new HookResult(hookHandle, hookProcedure);
        }

        private static HookResult HookGlobal(int hookId, Callback callback)
        {
            HookProcedure hookProcedure = (code, param, lParam) => HookProcedure(code, param, lParam, callback);

            var hookHandle = HookNativeMethods.SetWindowsHookEx(
                hookId,
                hookProcedure,
                Process.GetCurrentProcess().MainModule.BaseAddress,
                0);

            if (hookHandle.IsInvalid)
            {
                ThrowLastUnmanagedErrorAsException();
            }

            return new HookResult(hookHandle, hookProcedure);
        }

        private static int HookProcedure(int nCode, int wParam, IntPtr lParam, Callback callback)
        {
            var passThrough = nCode != 0;
            if (passThrough)
            {
                return CallNextHookEx(nCode, wParam, lParam);
            }

            var callbackData = new CallbackData(wParam, lParam);
            var continueProcessing = callback(callbackData);

            if (!continueProcessing)
            {
                return -1;
            }

            return CallNextHookEx(nCode, wParam, lParam);
        }

        private static int CallNextHookEx(int nCode, int wParam, IntPtr lParam)
        {
            return HookNativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        private static void ThrowLastUnmanagedErrorAsException()
        {
            var errorCode = Marshal.GetLastWin32Error();
            throw new Win32Exception(errorCode);
        }
    }
}