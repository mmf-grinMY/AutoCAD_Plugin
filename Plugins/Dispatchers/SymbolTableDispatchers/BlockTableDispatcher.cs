using Plugins.Entities;
using Plugins.Logging;

using System;

using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using static Plugins.Constants;

namespace Plugins.Dispatchers
{
    /// <summary>
    /// Диспетчер таблицы блоков
    /// </summary>
    class BlockTableDispatcher : SymbolTableDispatcher
    {
        #region Ctors

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="database">Внутренняя БД AutoCAD</param>
        /// <param name="log">Текущий логер событий</param>
        public BlockTableDispatcher(Database database, ILogger log) : base(database, log) { }

        #endregion

        #region Private Methods

        /// <summary>
        /// Добавить элемент в БД
        /// </summary>
        /// <param name="transaction">Текущая транзакция в БД AutoCAD</param>
        /// <param name="record">Текущая запись в таблицу блоков</param>
        /// <param name="entity">Записываемый объект</param>
        void Append(Transaction transaction, BlockTableRecord record, Autodesk.AutoCAD.DatabaseServices.Entity entity)
        {
            record.AppendEntity(entity);
            transaction.AddNewlyCreatedDBObject(entity, true);
        }
        /// <summary>
        /// Добавить круг
        /// </summary>
        /// <param name="transaction">Текущая транзакция в БД AutoCAD</param>
        /// <param name="record">Текущая запись в таблицу блоков</param>
        /// <param name="radius">Радиус круга</param>
        /// <param name="hasHatch">Наличие штриховки</param>
        void AddCircle(Transaction transaction, BlockTableRecord record, double radius, bool hasHatch = false)
        {
            var circle = new Circle()
            {
                Center = new Point3d(0, 0, 0),
                Radius = radius,
                Color = Color.FromColorIndex(ColorMethod.ByBlock, 0)
            };

            Append(transaction, record, circle);

            if (hasHatch) AddHatch(transaction, record, circle);
        }
        /// <summary>
        /// Добавить штриховку
        /// </summary>
        /// <param name="transaction">Текущая транзакция в БД AutoCAD</param>
        /// <param name="record">Текущая запись в таблицу блоков</param>
        /// <param name="owner">Владелец штриховки</param>
        void AddHatch(Transaction transaction, BlockTableRecord record, Autodesk.AutoCAD.DatabaseServices.Entity owner)
        {
            var hatch = new Hatch();

            Append(transaction, record, hatch);

            hatch.SetHatchPattern(HatchPatternType.UserDefined, "SOLID");
            hatch.Color = Color.FromColorIndex(ColorMethod.ByBlock, 0);
            hatch.Associative = true;
            hatch.AppendLoop(HatchLoopTypes.Outermost, new ObjectIdCollection { owner.ObjectId });
            hatch.EvaluateHatch(true);
        }
        /// <summary>
        /// Добавить линию
        /// </summary>
        /// <param name="transaction">Текущая транзакция в БД AutoCAD</param>
        /// <param name="record">Текущая запись в таблицу блоков</param>
        /// <param name="p1">Начальная точка</param>
        /// <param name="p2">Конечная точка</param>
        void AddLine(Transaction transaction, BlockTableRecord record, Point3d p1, Point3d p2) =>
            Append(transaction, record, new Line(p1, p2) { Color = Color.FromColorIndex(ColorMethod.ByBlock, 0) });
        /// <summary>
        /// Добавить полигон
        /// </summary>
        /// <param name="transaction">Текущая транзакция в БД AutoCAD</param>
        /// <param name="record">Текущая запись в таблицу блоков</param>
        /// <param name="points">Точки вершин</param>
        void AddPolygon(Transaction transaction, BlockTableRecord record, Point2d[] points)
        {
            var polyline = new Autodesk.AutoCAD.DatabaseServices.Polyline()
            {
                Color = Color.FromColorIndex(ColorMethod.ByBlock, 0)
            };

            for (int i = 0; i < points.Length; ++i)
            {
                polyline.AddVertexAt(i, new Point2d(points[i].X, points[i].Y), 0, 0, 0);
            }

            polyline.Closed = true;

            Append(transaction, record, polyline);
            AddHatch(transaction, record, polyline);
        }
        /// <summary>
        /// Создать новую запись блока
        /// </summary>
        /// <param name="transaction">Текущая транзакция</param>
        /// <param name="record">Запись таблциы блоков</param>
        /// <param name="name">Имя записи</param>
        /// <exception cref="NotImplementedException"></exception>
        void Create(Transaction transaction, BlockTableRecord record, string name)
        {
            switch (name)
            {
                // TODO: Сделать создание блоков по описанию в файле
                case "pnt!.chr_48":
                    AddCircle(transaction, record, 2);
                    AddCircle(transaction, record, 4);
                    break;
                case "pnt!.chr_53":
                    AddCircle(transaction, record, 2, true);
                    break;
                case "pnt!.chr_100":
                    AddCircle(transaction, record, 3);
                    AddLine(transaction, record, new Point3d(-1, 0, 0), new Point3d(1, 0, 0));
                    AddLine(transaction, record, new Point3d(0, -1, 0), new Point3d(0, 1, 1));
                    break;
                case "pnt!.chr_117":
                    AddPolygon(transaction, record, new Point2d[] { new Point2d(-3, -3), new Point2d(3, -3), new Point2d(0, 4) });
                    break;
                case "pnt!.chr_123":
                    AddPolygon(transaction, record, new Point2d[] { new Point2d(-3, 3), new Point2d(3, 3), new Point2d(0, -4) });
                    break;
                case "pnt!.chr_139":
                    AddCircle(transaction, record, 3);
                    break;
                default:
                    {
                        var e = new NotImplementedException("Не определена логика отрисовки блока " + name + "!");
                        var strings = name.Split('_');
                        e.Data.Add("FontName", strings[0]);
                        e.Data.Add("Symbol", strings[1]);
                        throw e;
                    }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Создать блок
        /// </summary>
        /// <param name="name">Имя добавляемого блока</param>
        /// <returns>true, если блок отрисован, false в противном случае</returns>
        public override bool TryAdd(string name) => TryAdd<BlockTable, BlockTableRecord>(name, db.BlockTableId, Create);

        #endregion
    }
}
