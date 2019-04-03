using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DrawingColor = System.Drawing.Color;
using MediaColor = System.Windows.Media.Color;

namespace BlackSpace
{
    public static class ColorExtensions
    {
        public static MediaColor ToMediaColor(this DrawingColor inColor)
        {
            return MediaColor.FromArgb(inColor.A, inColor.R, inColor.G, inColor.B);
        }

        public static DrawingColor ToDrawingColor(this MediaColor inColor)
        {
            return DrawingColor.FromArgb(inColor.A, inColor.R, inColor.G, inColor.B);
        }
    }
}
