using System;
using System.Diagnostics;

namespace aybe.Graphics
{
    /// <summary>
    ///     Represents a <see cref="Renderer" /> context.
    /// </summary>
    /// <remarks>
    ///     Note: a color is represented by an integer in ARGB format.
    /// </remarks>
    public abstract class RendererContext : IDisposable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private readonly Renderer _renderer;

        /// <summary>
        ///     Create a new instance of <see cref="RendererContext" />.
        /// </summary>
        /// <param name="renderer">The renderer this context is for.</param>
        protected RendererContext(Renderer renderer)
        {
            _renderer = renderer;
        }

        /// <summary>
        ///     Gets the renderer of this context.
        /// </summary>
        protected Renderer Renderer
        {
            get { return _renderer; }
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public abstract void Dispose();

        /// <summary>
        ///     Clear bitmap with specified color.
        /// </summary>
        /// <param name="color">The color to use.</param>
        public abstract void Clear(int color);

        /// <summary>
        ///     Clear bitmap with transparent color.
        /// </summary>
        public abstract void Clear();

        /// <summary>
        ///     Draws a line.
        /// </summary>
        /// <param name="x1">The X-coordinate of the first point.</param>
        /// <param name="y1">The Y-coordinate of the first point.</param>
        /// <param name="x2">The X-coordinate of the second point.</param>
        /// <param name="y2">The Y-coordinate of the second point.</param>
        /// <param name="color">The color to use.</param>
        public abstract void DrawLine(int x1, int y1, int x2, int y2, int color);

        /// <summary>
        ///     Draws a dashed line.
        /// </summary>
        /// <param name="x1">The X-coordinate of the first point.</param>
        /// <param name="y1">The Y-coordinate of the first point.</param>
        /// <param name="x2">The X-coordinate of the second point.</param>
        /// <param name="y2">The Y-coordinate of the second point.</param>
        /// <param name="dashes">The array of line dashes to use.</param>
        public abstract void DrawLine(int x1, int y1, int x2, int y2, LineDash[] dashes);

        /// <summary>
        ///     Draws a filled rectangle.
        /// </summary>
        /// <param name="x1">The X-coordinate of the top/left point.</param>
        /// <param name="y1">The Y-coordinate of the top/left point.</param>
        /// <param name="x2">The X-coordinate of the bottom/right point.</param>
        /// <param name="y2">The Y-coordinate of the bottom/right point.</param>
        /// <param name="color">The color to use.</param>
        public abstract void FillRectangle(int x1, int y1, int x2, int y2, int color);
    }
}