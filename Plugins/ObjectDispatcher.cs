using System;

using AApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins
{
    /// <summary>
    /// Диспетчер управления отрисовкой примитивов
    /// </summary>
    class ObjectDispatcher
    {
        #region Private Fields

        readonly Entities.EntitiesFactory factory;
        /// <summary>
        /// Рисуемый горизонт
        /// </summary>
        readonly string gorizont;
        /// <summary>
        /// Менеджер поключения к Oracle БД
        /// </summary>
        readonly OracleDbDispatcher connection;
        /// <summary>
        /// Внутренняя БД AutoCAD
        /// </summary>
        readonly Database db;

        #endregion
        
        #region Private Methods

        /// <summary>
        /// Создать новый слой
        /// </summary>
        /// <param name="layerName">Имя слоя</param>
        public void CreateLayer(string layerName)
        {
            using (var transaction = db.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(db.LayerTableId, OpenMode.ForWrite) as LayerTable;
                var record = new LayerTableRecord { Name = layerName };
                table.Add(record);
                transaction.AddNewlyCreatedDBObject(record, true);

                transaction.Commit();
            }
        }
        public Oracle.ManagedDataAccess.Client.OracleDataReader DrawParams => connection.GetDrawParams(gorizont);
        public void ConnectionDispose() => connection.Dispose();
        /// <summary>
        /// Количество объектов, доступных для отрисовки на выбранном горизонте
        /// </summary>
        public int Count => connection.Count(gorizont);
        public void Create(Entities.Primitive draw)
        {
            var layer = draw.LayerName;
            if (!Session.Contains(layer))
            {
                Session.Add(layer);
                CreateLayer(layer);
            }

            using (var entity = factory.Create(draw))
            {
                entity?.Draw();
            }
        }

        #endregion

        #region Ctors

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="conn">Менеджер подключения</param>
        /// <param name="selectedGorizont">Выбранный горизонт</param>
        public ObjectDispatcher(OracleDbDispatcher conn, string selectedGorizont)
        {
            db = AApplication.DocumentManager.MdiActiveDocument.Database;
            connection = conn;
            gorizont = selectedGorizont;
            factory = new Entities.EntitiesFactory(db);
        }

        #endregion
    }
    public sealed class NotDrawingLineException : Exception { }
}