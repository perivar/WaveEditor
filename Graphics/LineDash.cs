using System;
using System.Runtime.InteropServices;

namespace aybe.Graphics
{
    /// <summary>
    ///     Represents a line dash.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LineDash
    {
        /// <summary>
        ///     Dash color.
        /// </summary>
        /// <remarks>Format is ARGB.</remarks>
        public readonly int Color;

        /// <summary>
        ///     Dash size in pixels.
        /// </summary>
        public readonly int Size;

        /// <summary>
        ///     Create a new instance of <see cref="LineDash" />.
        /// </summary>
        /// <param name="size">Dash size.</param>
        /// <param name="color">Dash color.</param>
        public LineDash(int size, int color)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException("size");
            Size = size;
            Color = color;
        }

        public override string ToString()
        {
            return string.Format("Size: {0}, Color: {1}", Size, Color);
        }
    }
}