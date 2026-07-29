using PdfSharp.Fonts;
using System.Reflection;

namespace Arauco.Otimizador.Common.Pdf
{
    public class CustomFontResolver : IFontResolver
    {
        public byte[]? GetFont(string faceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var path = @$"Arauco.Otimizador.Common.Pdf.Fonts.{faceName}";

            using (Stream stream = assembly.GetManifestResourceStream(path))
            {
                if (stream == null)
                    throw new ArgumentException("No resource with name " + path);

                int count = (int)stream.Length;
                byte[] data = new byte[count];
                stream.Read(data, 0, count);
                return data;
            }
        }

        public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            if (isBold && isItalic)
            {
                return new FontResolverInfo($"{familyName}-BoldItalic.ttf");
            }
            else if (isBold)
            {
                return new FontResolverInfo($"{familyName}-Bold.ttf");
            }
            else if (isItalic)
            {
                return new FontResolverInfo($"{familyName}-Italic.ttf");
            }
            else
            {
                return new FontResolverInfo($"{familyName}.ttf");
            }
        }
    }
}