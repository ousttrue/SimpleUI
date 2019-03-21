﻿using DesktopDll;
using RectUI;
using System;


namespace RectUISample
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine($"IntPtr.Size = {IntPtr.Size} bytes");

            // create window
            var window = Window.Create();
            window.Show();

            // build UI
            var root = new HorizontalSplitter
            {
                Rect = new Rect(window.Width, window.Height)
            };
            root.Add(new ListRegion(new DirSource())
            {
                Rect = new Rect(window.Width, window.Height),
            });
            root.Add(new RectRegion
            {
                Drawer = new RectDrawer(),
            });

            // bind window with UI
            using (var app = new App(window, root))
            {
                window.MessageLoop();
            }
        }
    }
}
