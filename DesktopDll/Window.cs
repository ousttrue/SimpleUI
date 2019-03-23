﻿using System;
using System.Reflection;
using System.Runtime.InteropServices;


namespace DesktopDll
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/windows/desktop/inputdev/mouse-input-notifications
    /// </summary>
    public enum WM : uint
    {
        DESTROY = 0x0002,
        MOVE = 0x0003,
        RESIZE = 0x0005,
        PAINT = 0x000F,
        MOUSEMOVE = 0x0200,
        LBUTTONDOWN = 0x0201,
        LBUTTONUP = 0x0202,
        RBUTTONDOWN = 0x0204,
        RBUTTONUP = 0x0205,
        MBUTTONDOWN = 0x0207,
        MBUTTONUP = 0x0208,
        MOUSEWHEEL = 0x020A,
    }

    /// <summary>
    /// https://docs.microsoft.com/en-us/windows/desktop/learnwin32/creating-a-window
    /// </summary>
    public class Window
    {
        const string CLASS_NAME = "class";
        const string WINDOW_NAME = "window";

        HWND _hwnd;
        public IntPtr WindowHandle
        {
            get { return _hwnd.Value; }
        }

        public RECT Rect
        {
            get
            {
                RECT rect;
                User32.GetClientRect(_hwnd, out rect);
                return rect;
            }
        }

        public int Width
        {
            get
            {
                var rect = Rect;
                return rect.right.Value - rect.left.Value;
            }
        }
        public int Height
        {
            get
            {
                var rect = Rect;
                return rect.bottom.Value - rect.top.Value;
            }
        }

        WNDPROC _delegate;
        IntPtr Callback
        {
            get
            {
                return Marshal.GetFunctionPointerForDelegate(_delegate);
            }
        }

        Window()
        {
            _delegate = new WNDPROC(WndProc);
        }

        public static Window Create()
        {
            var ms = Assembly.GetEntryAssembly().GetModules();
            var hInstance = Marshal.GetHINSTANCE(ms[0]);

            var window = new Window();

            var wc = new WNDCLASSEXW
            {
                cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEXW)),
                style = CS.VREDRAW | CS.HREDRAW,
                lpszClassName = CLASS_NAME,
                lpfnWndProc = window.Callback,
                hInstance = hInstance,
                hCursor = User32.LoadCursorW(default(HINSTANCE), IDC.ARROW),
            };
            var register = User32.RegisterClassExW(ref wc);
            if (register == 0)
            {
                return null;
            }

            var hwnd = User32.CreateWindowExW(0, CLASS_NAME, WINDOW_NAME, WS.OVERLAPPEDWINDOW,
                User32.CW_USEDEFAULT,
                User32.CW_USEDEFAULT,
                User32.CW_USEDEFAULT,
                User32.CW_USEDEFAULT,
                IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);
            if (hwnd == IntPtr.Zero)
            {
                return null;
            }

            window._hwnd = hwnd;
            return window;
        }

        LRESULT WndProc(HWND hwnd, WM msg, WPARAM wParam, LPARAM lParam)
        {
            switch (msg)
            {
                case WM.DESTROY:
                    User32.PostQuitMessage(0);
                    return 0;

                case WM.MOUSEMOVE:
                    OnMouseMove?.Invoke(lParam.LowWord, lParam.HiWord);
                    return 0;

                case WM.LBUTTONDOWN:
                    OnMouseLeftDown?.Invoke(lParam.LowWord, lParam.HiWord);
                    return 0;
                case WM.LBUTTONUP:
                    OnMouseLeftUp?.Invoke(lParam.LowWord, lParam.HiWord);
                    return 0;
                case WM.RBUTTONDOWN:
                    OnMouseRightDown?.Invoke(lParam.LowWord, lParam.HiWord);
                    return 0;
                case WM.RBUTTONUP:
                    OnMouseRightUp?.Invoke(lParam.LowWord, lParam.HiWord);
                    return 0;
                case WM.MBUTTONDOWN:
                    OnMouseMiddleDown?.Invoke(lParam.LowWord, lParam.HiWord);
                    return 0;
                case WM.MBUTTONUP:
                    OnMouseMiddleUp?.Invoke(lParam.LowWord, lParam.HiWord);
                    return 0;
                case WM.MOUSEWHEEL:
                    OnMouseWheel?.Invoke(wParam.HiWord);
                    return 0;

                case WM.RESIZE:
                    OnResize?.Invoke(lParam.LowWord, lParam.HiWord);
                    return 0;

                case WM.PAINT:
                    {
                        var ps = default(PAINTSTRUCT);
                        User32.BeginPaint(hwnd, ref ps);
                        OnPaint?.Invoke();
                        User32.EndPaint(hwnd, ref ps);
                    }
                    return 0;
            }
            return User32.DefWindowProcW(hwnd, msg, wParam, lParam);
        }

        public event Action<int, int> OnMouseLeftDown;
        public event Action<int, int> OnMouseLeftUp;
        public event Action<int, int> OnMouseRightDown;
        public event Action<int, int> OnMouseRightUp;
        public event Action<int, int> OnMouseMiddleDown;
        public event Action<int, int> OnMouseMiddleUp;
        public event Action<int, int> OnMouseMove;
        public event Action<int> OnMouseWheel;
        public event Action<int, int> OnResize;
        public event Action OnPaint;

        public void Show()
        {
            User32.ShowWindow(_hwnd, SW.SHOW);
        }

        public void Invalidate()
        {
            User32.InvalidateRect(_hwnd, IntPtr.Zero, true);
        }

        MSG _msg;
        public void MessageLoop()
        {
            while (true)
            {
                if (!User32.GetMessageW(ref _msg, 0, 0, 0))
                {
                    break;
                };
                User32.TranslateMessage(ref _msg);
                User32.DispatchMessage(ref _msg);
            }
        }
    }
}
