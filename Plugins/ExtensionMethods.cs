using System.Collections.Generic;
using System;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using Oracle.ManagedDataAccess.Client;

using static Plugins.Constants;

namespace Plugins
{
    static class ExtensionMethods
    {
        private static Matrix3d EyeToWorld(this ViewTableRecord view)
        {
            return
                Matrix3d.Rotation(-view.ViewTwist, view.ViewDirection, view.Target) *
                Matrix3d.Displacement(view.Target - Point3d.Origin) *
                Matrix3d.PlaneToWorld(view.ViewDirection);
        }
        private static Matrix3d WorldToEye(this ViewTableRecord view) => view.EyeToWorld().Inverse();
        public static void Zoom(this Editor ed, Extents3d ext)
        {
            using (var view = ed.GetCurrentView())
            {
                ext.TransformBy(view.WorldToEye());
                view.Width = ext.MaxPoint.X - ext.MinPoint.X;
                view.Height = ext.MaxPoint.Y - ext.MinPoint.Y;
                view.CenterPoint = new Point2d(
                    (ext.MaxPoint.X + ext.MinPoint.X) / 2.0,
                    (ext.MaxPoint.Y + ext.MinPoint.Y) / 2.0);
                ed.SetCurrentView(view);
            }
        }
        /// <summary>
        /// Конвертация строки в вещественное число
        /// </summary>
        /// <param name="str">Строковое представление числа</param>
        /// <returns>Вещественное число</returns>
        public static double ToDouble(this string str)
        {
            if (str is null)
                return 0.0;

            if (str.Contains("_"))
                str = str.Replace('_', '.');
            else if (str.Contains(","))
                str = str.Replace(',', '.');

            return double.Parse(str, System.Globalization.CultureInfo.GetCultureInfo("en"));
        }
        /// <summary>
        /// Конвертация градусов в радианы
        /// </summary>
        /// <param name="degree">Угол в градусах</param>
        /// <returns>Угол в радианах</returns>
        public static double ToRad(this double degree) => degree / 180 * System.Math.PI;
        /// <summary>
        /// Добавление в XData необходимых для связывания таблиц параметров
        /// </summary>
        /// <param name="entity">Связываемый объект</param>
        /// <param name="primitive">Параметры отрисовки</param>
        public static Entity AddXData(this Entity entity, Entities.Primitive primitive)
        {
            var typedValues = new List<TypedValue>
            {
                new TypedValue(1001, SYSTEM_ID), 
                new TypedValue((int)DxfCode.ExtendedDataInteger32, primitive.SystemId),
                new TypedValue(1001, OBJ_ID),
                new TypedValue((int)DxfCode.ExtendedDataInteger32, primitive.Id),
            };

            if (primitive.BaseName != null && primitive.ChildField != null)
            {
                typedValues.Add(new TypedValue(1001, BASE_NAME));
                typedValues.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, primitive.BaseName));
                typedValues.Add(new TypedValue(1001, LINK_FIELD));
                typedValues.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, primitive.ChildField));
            }

            entity.XData = new ResultBuffer(typedValues.ToArray());

            return entity;
        }
        /// <summary>
        /// Получение XData
        /// </summary>
        /// <param name="buffer">Исходный буфер</param>
        /// <param name="RegAppName">Зарегестрированное имя</param>
        /// <returns>Хранимые данные</returns>
        public static string GetXData(this ResultBuffer buffer, string RegAppName)
        {
            for (var iter = buffer.GetEnumerator(); iter.MoveNext();)
            {
                var tv = iter.Current;

                if ((tv.TypeCode == (short)DxfCode.ExtendedDataRegAppName) && (tv.Value.ToString() == RegAppName))
                {
                    if (iter.MoveNext())
                    {
                        return iter.Current.Value.ToString();
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return string.Empty;
        }
        /// <summary>
        /// Добавить примитив в БД AutoCAD
        /// </summary>
        /// <param name="entity">Сохраняемый объект</param>
        /// <param name="transaction">Текщуая транзакция</param>
        /// <param name="record">Текщая запись таблицы блоков</param>
        /// <param name="primitive">Примитив объекта</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void AppendToDb(this Entity entity,
                                      Transaction transaction,
                                      BlockTableRecord record,
                                      Entities.Primitive primitive)
        {
            if (entity is null)
                throw new ArgumentNullException(nameof(entity));

            if (transaction is null)
                throw new ArgumentNullException(nameof(transaction));

            if (record is null)
                throw new ArgumentNullException(nameof(record));

            if (primitive is null)
                throw new ArgumentNullException(nameof(primitive));

            record.AppendEntity(entity.AddXData(primitive));
            transaction.AddNewlyCreatedDBObject(entity, true);
        }
        /// <summary>
        /// Получение текста ошибки по ее коду
        /// </summary>
        /// <param name="e">Ошибка подключения</param>
        /// <returns>Текстовое содержимое ошибки</returns>
        public static string GetCodeDescription(this OracleException e)
        {
            switch (e.Number)
            {
                case 12154: return "Неправильно указаны данные подключения к базе данных!";
                case 1017: return "Неправильный логин или пароль!";
                default: return "При попытки подключения произошла ошибка! Проверьте введенные данные и повторите попытку!";
            }
        }
    }
}