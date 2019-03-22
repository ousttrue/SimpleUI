﻿using DesktopDll;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RectUI.Graphics
{
    class MemoryBitmap : IDisposable
    {
        HDC m_hDC;
        HBITMAP m_bmp;
        public HDC DC
        {
            get;
            private set;
        }
        IntPtr m_hOrgBMP;

        public void Dispose()
        {
            Gdi32.SelectObject(DC, m_hOrgBMP);
            Gdi32.DeleteDC(DC);
            Gdi32.DeleteObject(m_bmp.Value);
            User32.ReleaseDC(default(HWND), m_hDC);
        }

        public MemoryBitmap(int w, int h)
        {
            m_hDC = User32.GetDC(default(HWND));
            m_bmp = Gdi32.CreateCompatibleBitmap(m_hDC, w, h);
            DC = Gdi32.CreateCompatibleDC(m_hDC);
            m_hOrgBMP = Gdi32.SelectObject(DC, m_bmp.Value);
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/desktop/gdi/capturing-an-image
        /// </summary>
        public void GetBitmap()
        {
            var bmpScreen = default(BITMAP);
            Gdi32.GetObject(DC.Value, Marshal.SizeOf<BITMAP>(), ref bmpScreen);

            var bmfHeader = default(BITMAPFILEHEADER);
            var bi = default(BITMAPINFOHEADER);

            bi.biSize = Marshal.SizeOf<BITMAPINFOHEADER>();
            bi.biWidth = bmpScreen.bmWidth;
            bi.biHeight = bmpScreen.bmHeight;
            bi.biPlanes = 1;
            bi.biBitCount = 32;
            //bi.biCompression = BI_RGB;
            bi.biSizeImage = 0;
            bi.biXPelsPerMeter = 0;
            bi.biYPelsPerMeter = 0;
            bi.biClrUsed = 0;
            bi.biClrImportant = 0;

            DWORD dwBmpSize = ((bmpScreen.bmWidth.Value * bi.biBitCount.Value + 31) / 32) * 4 * bmpScreen.bmHeight.Value;

            // Starting with 32-bit Windows, GlobalAlloc and LocalAlloc are implemented as wrapper functions that 
            // call HeapAlloc using a handle to the process's default heap. Therefore, GlobalAlloc and LocalAlloc 
            // have greater overhead than HeapAlloc.
            //HANDLE hDIB = GlobalAlloc(GHND, dwBmpSize);
            //char* lpbitmap = (char*)GlobalLock(hDIB);

            /*
            // Gets the "bits" from the bitmap and copies them into a buffer 
            // which is pointed to by lpbitmap.
            Gdi32.GetDIBits(default(HWND), hbmScreen, 0,
                (UINT)bmpScreen.bmHeight,
                lpbitmap,
                (BITMAPINFO*)&bi, DIB_RGB_COLORS);
            */
        }
    }

    public class D2D1Bitmap : IDisposable
    {
        Bitmap1 _bitmap;
        Dictionary<Color4, SolidColorBrush> _brushMap = new Dictionary<Color4, SolidColorBrush>();
        TextFormat _textFormat;
        Dictionary<IntPtr, Bitmap> _bitmapMap = new Dictionary<IntPtr, Bitmap>();
        Dictionary<int, Bitmap> _imageListMap = new Dictionary<int, Bitmap>();

        public void Dispose()
        {
            if (_textFormat != null)
            {
                _textFormat.Dispose();
                _textFormat = null;
            }

            foreach (var kv in _brushMap)
            {
                kv.Value.Dispose();
            }
            _brushMap.Clear();

            if (_bitmap != null)
            {
                _bitmap.Dispose();
                _bitmap = null;
            }
        }

        Func<SharpDX.DXGI.Surface> _getSurface;
        public D2D1Bitmap(Func<SharpDX.DXGI.Surface> getSurface)
        {
            _getSurface = getSurface;
        }

        void CreateBitmap(D3D11Device device)
        {
            Dispose();

            var pf = new PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Ignore);
            var bp = new BitmapProperties1(pf, device.Dpi.Height, device.Dpi.Width,
                BitmapOptions.CannotDraw | BitmapOptions.Target)
                ;

            using (var surface = _getSurface())
            {
                _bitmap = new Bitmap1(device.D2DDeviceContext, surface);
            }
        }

        public void Begin(D3D11Device device, Color4 clear)
        {
            if (_bitmap == null)
            {
                CreateBitmap(device);
            }

            device.D2DDeviceContext.Target = _bitmap;
            device.D2DDeviceContext.BeginDraw();
            device.D2DDeviceContext.Clear(clear);
            device.D2DDeviceContext.Transform = Matrix3x2.Identity;
        }

        public void End(D3D11Device device)
        {
            device.D2DDeviceContext.Target = null;
            device.D2DDeviceContext.EndDraw();
        }

        public void Draw(D3D11Device device, DrawCommand command)
        {
            switch(command.DrawType)
            {
                case DrawType.Rectangle:
                    DrawRect(device, command.Rectangle, 
                        command.FillColor, command.BorderColor);
                    break;

                case DrawType.Text:
                    DrawText(device, command.Rectangle, 
                        command.Font, command.FontSize,
                        command.TextColor, command.Text);
                    break;

                case DrawType.Icon:
                    DrawIcon(device, command.Rectangle,
                        command.Icon);
                    break;

                case DrawType.ImageList:
                    DrawImageList(device, command.Rectangle,
                        command.Icon, command.ImageListIndex);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        void DrawRect(D3D11Device device,
            RectangleF rect,
            Color4 fill,
            Color4 border)
        {
            SolidColorBrush fillBrush;
            if (!_brushMap.TryGetValue(fill, out fillBrush))
            {
                fillBrush = new SolidColorBrush(device.D2DDeviceContext, fill);
                _brushMap.Add(fill, fillBrush);
            }

            SolidColorBrush borderBrush;
            if (!_brushMap.TryGetValue(border, out borderBrush))
            {
                borderBrush = new SolidColorBrush(device.D2DDeviceContext, border);
                _brushMap.Add(border, borderBrush);
            }

            device.D2DDeviceContext.FillRectangle(rect, fillBrush);
            device.D2DDeviceContext.DrawRectangle(rect, borderBrush, 2.0f);
        }

        void DrawIcon(D3D11Device device, 
            RectangleF rect,
            IntPtr icon)
        {
            if (icon == IntPtr.Zero)
            {
                return;
            }
            Bitmap bitmap;
            if(!_bitmapMap.TryGetValue(icon, out bitmap))
            {
                // todo
            }
            // todo
        }

        void DrawImageList(D3D11Device device,
            RectangleF rect,
            IntPtr imageList, int imageListIndex)
        {
            if (imageList == IntPtr.Zero)
            {
                return;
            }
            Bitmap bitmap;
            if (!_imageListMap.TryGetValue(imageListIndex, out bitmap))
            {
                // todo
                int w = 0;
                int h = 0;
                if(!Comctl32.ImageList_GetIconSize(imageList, ref w, ref h))
                {
                    return;
                }
                using (var memoryBitmap = new MemoryBitmap(w, h))
                {
                    Comctl32.ImageList_Draw(imageList, imageListIndex, memoryBitmap.DC, 0, 0, ILD.NORMAL);

                    memoryBitmap.GetBitmap();
                }
            }
            // todo
        }

        void DrawText(D3D11Device device,
            RectangleF rect,
            string font,
            float fontSize,
            Color4 textColor,
            string text)
        {
            SolidColorBrush brush;
            if (!_brushMap.TryGetValue(textColor, out brush))
            {
                brush = new SolidColorBrush(device.D2DDeviceContext, textColor);
                _brushMap.Add(textColor, brush);
            }

            if (_textFormat == null)
            {
                using (var f = new SharpDX.DirectWrite.Factory())
                {
                    _textFormat = new TextFormat(f, font, fontSize);
                }
            }

            device.D2DDeviceContext.DrawText(text, _textFormat, rect, brush);
        }
    }
}
