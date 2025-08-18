using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PdfSharp.Fonts;
using System.IO;
using System.Reflection;

namespace Phramacy_Product
{


    public class MyFontResolver : IFontResolver
    {
        public static readonly MyFontResolver Instance = new MyFontResolver();

        public string DefaultFontName => "CourierNew";

        public byte[] GetFont(string faceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream("Phramacy_Product.Fonts.cour.ttf"))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            if (familyName.Equals("Courier New", StringComparison.OrdinalIgnoreCase))
                return new FontResolverInfo("CourierNew");

            return null;
        }
    }

}
