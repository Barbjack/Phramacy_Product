using System;
using System.IO;
using System.Reflection;
using PdfSharp.Fonts;

namespace Phramacy_Product
{
    public class MyFontResolver : IFontResolver
    {
        public string DefaultFontName => "cour";

        public byte[] GetFont(string faceName)
        {
            if (faceName.Equals("cour", StringComparison.OrdinalIgnoreCase))
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream("Phramacy_Product.Fonts.cour.ttf"))
                {
                    if (stream != null)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            return ms.ToArray();
                        }
                    }
                }
            }
            return null;
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            // Now that you're explicitly using "cour" in your code, 
            // the resolver needs to handle that request directly.
            if (familyName.Equals("cour", StringComparison.OrdinalIgnoreCase))
            {
                // This line is now crucial. It tells the library that "cour" is a valid font name.
                return new FontResolverInfo("cour");
            }

            // Keep the Arial mapping as a fallback for any other parts of the code.
            //if (familyName.Equals("Arial", StringComparison.OrdinalIgnoreCase))
            //{
            //    return new FontResolverInfo("cour");
            //}

            if (familyName.Equals("Courier New", StringComparison.OrdinalIgnoreCase))
            {
                return new FontResolverInfo("cour");
            }

            return null;
        }
    }
}