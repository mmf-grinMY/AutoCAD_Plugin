using System;

namespace Plugins.Entities
{
    /// <summary>
    /// Мой стиль отрисовки примитивов
    /// </summary>
    class MyEntityStyle
    {
        /// <summary>
        /// Масштаб примитива
        /// </summary>
        public readonly double scale;
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="scale">масштаб отрисовки</param>
        /// <exception cref="ArgumentException"></exception>
        public MyEntityStyle(double scale)
        {
            if (scale == 0)
                throw new ArgumentException(nameof(scale), "Масштаб не может принимать значение 0!");

            this.scale = scale;
        }
    }
    /// <summary>
    /// Мой стиль отрисовки штриховки
    /// </summary>
    class MyHatchStyle : MyEntityStyle
    {
        /// <summary>
        /// Прозрачность
        /// </summary>
        public readonly byte transparency;
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="scale">Масштаб отрисовки</param>
        /// <param name="transparency">Прозрачность</param>
        /// <exception cref="ArgumentException"></exception>
        public MyHatchStyle(double scale, byte transparency) : base(scale)
        {
            if (transparency == 0)
                throw new ArgumentException(nameof(MyHatchStyle.transparency), "Прозрачность не может принимать значение 0!");

            this.transparency = transparency;
        }
    }
}
