using PdfSharp.Fonts;
using Phramacy_Product.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace Phramacy_Product
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // --- Temporary debug code ---
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            System.Diagnostics.Debug.WriteLine("Embedded Resources:");
            foreach (var name in resourceNames)
            {
                System.Diagnostics.Debug.WriteLine(name);
            }
            GlobalFontSettings.FontResolver = new MyFontResolver();
        }
    }
}
