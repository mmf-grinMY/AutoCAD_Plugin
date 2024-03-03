#define MY_BOUNDING_BOX

using Plugins.Entities;

using System.Collections.Generic;
using System.Windows;
using System;

using AApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace Plugins
{
    /// <summary>
    /// Диспетчер управления отрисовкой примитивов
    /// </summary>
    internal class ObjectDispatcher
    {
        #region Private Fields

        private readonly EntitiesFactory factory;
        /// <summary>
        /// Рисуемый горизонт
        /// </summary>
        private readonly string gorizont;
        /// <summary>
        /// Менеджер поключения к Oracle БД
        /// </summary>
        private readonly OracleDbDispatcher connection;
        /// <summary>
        /// Внутренняя БД AutoCAD
        /// </summary>
        private static readonly Database db;
#if MY_BOUNDING_BOX
        readonly Box box;
#endif

        #endregion
        
        #region Private Methods

        /// <summary>
        /// Создать новый слой
        /// </summary>
        /// <param name="layerName">Имя слоя</param>
        private void CreateLayer(string layerName)
        {
            using (var transaction = db.TransactionManager.StartTransaction())
            {
                using (var table = transaction.GetObject(db.LayerTableId, OpenMode.ForWrite) as LayerTable)
                {
                    using (var record = new LayerTableRecord { Name = layerName })
                    {
                        table.Add(record);
                        transaction.AddNewlyCreatedDBObject(record, true);
                    }
                }

                transaction.Commit();
            }
        }

        #endregion

        #region Ctors

        /// <summary>
        /// Статическое создание
        /// </summary>
        static ObjectDispatcher()
        {
            db = AApplication.DocumentManager.MdiActiveDocument.Database;
        }
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="connection">Менеджер подключения</param>
        /// <param name="selectedGorizont">Выбранный горизонт</param>
        public ObjectDispatcher(OracleDbDispatcher connection, string selectedGorizont)
        {
            this.connection = connection;
            gorizont = selectedGorizont;
            box = new Box() { Bottom = long.MaxValue, Left = long.MaxValue, Right = long.MinValue, Top = long.MinValue };
            factory = new EntitiesFactory(AApplication.DocumentManager.MdiActiveDocument.Database, box);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Начать отрисовку объектов
        /// </summary>
        /// <param name="window">Окно отображения пргресса отрисовки</param>
        public void Draw()
        {
            var layersCache = new HashSet<string>();

            using (var reader = connection.GetDrawParams(gorizont))
            {
                // TODO: Добавить индикатор прогресса
                while(reader.Read())
                {
                    try
                    {
                        var draw = new Primitive(reader["geowkt"].ToString(),
                                                  reader["drawjson"].ToString(),
                                                  reader["paramjson"].ToString(),
                                                  reader["layername"] + " | " + reader["sublayername"],
                                                  reader["systemid"].ToString(),
                                                  reader["basename"].ToString(),
                                                  reader["childfields"].ToString());

                        var layer = draw.LayerName;

                        if (!layersCache.Contains(layer))
                        {
                            layersCache.Add(layer);
                            CreateLayer(layer);
                        }

                        using (var entity = factory.Create(draw))
                        {
                            entity?.Draw();
                        }
                    }
                    catch (NoDrawingLineException) { }
                    catch (FormatException)
                    {
                        var rows = new string[]
                        {
                            "geowkt",
                            "drawjson",
                            "paramjson",
                            "layername",
                            "sublayername",
                            "systemid",
                            "basename",
                            "childfields"
                        };

                        foreach (var row in rows)
                        {
                            if (reader[row] == null)
                                MessageBox.Show("Столбец " + row + " принимает значение NULL!" + '\n' + reader["geowkt"]);
                        }
                    }
                    catch (Exception ex)
                    {
#if !RELEASE
                        MessageBox.Show(ex.GetType() + "\n" + ex.Message + "\n" + ex.StackTrace + "\n" + ex.Source);
#endif
                    }
                }
            }
            MessageBox.Show("Закончена отрисовка геометрии!");
            AApplication.DocumentManager.MdiActiveDocument.Editor
                .Zoom(new Extents3d(new Point3d(box.Left, box.Bottom, 0), new Point3d(box.Right, box.Top, 0)));
        }

        #endregion
    }
    public sealed class NoDrawingLineException : Exception { }
}