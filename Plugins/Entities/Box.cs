namespace Plugins
{
    /// <summary>
    /// Граничная рамка рисуемых примитивов
    /// </summary>
    sealed class Box
    {
        /// <summary>
        /// Координата левого края
        /// </summary>
        public long Left { get; set; }
        /// <summary>
        /// Координата правого края
        /// </summary>
        public long Right { get; set; }
        /// <summary>
        /// Координата верхнего края
        /// </summary>
        public long Top { get; set; }
        /// <summary>
        /// Координата нижнего края
        /// </summary>
        public long Bottom { get; set; }
    }
}