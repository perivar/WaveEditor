namespace aybe.Graphics
{
    /// <summary>
    ///     Represents a renderer.
    /// </summary>
    public abstract class Renderer
    {
        /// <summary>
        ///     Gets the context of this renderer.
        /// </summary>
        /// <returns></returns>
        public abstract RendererContext GetContext();

        /// <summary>
        ///     Gets the bitmap (see remarks).
        /// </summary>
        /// <returns>A bitmap object, the type depends of the implementation of <see cref="Renderer" /> you are using.</returns>
        /// <remarks>
        ///     You should cast the object to the type representing a bitmap in your framework, e.g. WriteableBitmap in WPF.
        ///     <para>
        ///         See remarks of the implementation you are using for which type you need to cast to.
        ///     </para>
        /// </remarks>
        public abstract object GetBitmap();

        /// <summary>
        ///     Gets the bitmap size.
        /// </summary>
        /// <param name="bitmapWidth">Variable receiving the bitmap width.</param>
        /// <param name="bitmapHeight">Variable recieving the bitmap height.</param>
        public abstract void GetBitmapSize(out int bitmapWidth, out int bitmapHeight);

        /// <summary>
        ///     Sets the bitmap size.
        /// </summary>
        /// <param name="bitmapWidth">Desired bitmap width.</param>
        /// <param name="bitmapHeight">Desired bitmap height.</param>
        /// <remarks>
        ///     Implementation should re-create the underlying object only when size differs from current one.
        /// </remarks>
        public abstract void SetBitmapSize(int bitmapWidth, int bitmapHeight);
    }
}