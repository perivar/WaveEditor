using System.Runtime.InteropServices;

namespace aybe.Graphics
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Int32Size
    {
        public readonly int Width;
        public readonly int Height;

        public Int32Size(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public override string ToString()
        {
            return string.Format("Width: {0}, Height: {1}", Width, Height);
        }
        
    }
}