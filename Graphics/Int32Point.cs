using System.Runtime.InteropServices;

namespace aybe.Graphics
{
    /// <summary>
    ///     Represents a point with integer coordinates.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Int32Point
    {
        public readonly int X;
        public readonly int Y;

        public Int32Point(int x, int y)
            : this()
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return string.Format("X: {0}, Y: {1}", X, Y);
        }
    }
}