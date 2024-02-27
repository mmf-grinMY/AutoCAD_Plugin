using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using static Plugins.Constants;

namespace Plugins.Entities
{
    /// <summary>
    /// Создатель блоков
    /// </summary>
    sealed class BlocksCreater
    {
        /// <summary>
        /// Внутренняя база данных AutoCAD
        /// </summary>
        private readonly Database db;
        /// <summary>
        /// Создание объекта
        /// </summary>
        public BlocksCreater() 
        {
            db = Application.DocumentManager.MdiActiveDocument.Database;
        }
        /// <summary>
        /// Создать блок
        /// </summary>
        /// <param name="blockName">Имя блока</param>
        /// <returns>true, если блок отрисован, false в противном случае</returns>
        public bool Create(string blockName)
        {
            using (var manager = new ResourceManager(db))
            {
                manager.Name = blockName;

                switch (blockName)
                {
                    // TODO: Сделать создание блоков по описанию в файле
                    case "pnt!.chr_48": CreatePntOld48(manager); break;
                    case "pnt!.chr_53": CreatePntOld53(manager); break;
                    case "pnt!.chr_100": CreatePntOld100(manager); break;
                    case "pnt!.chr_117": CreatePntOld117(manager); break;
                    case "pnt!.chr_123": CreatePntOld123(manager); break;
                    case "pnt!.chr_139": CreatePntOld139(manager); break;
                    default: manager.Abort(); return false;
                }
            }

            return true;
        }
        /// <summary>
        /// Создать блок, описывающий символ 49 из pnt!.chr
        /// </summary>
        /// <param name="manager">Менеджер ресурсов</param>
        private void CreatePntOld48(ResourceManager manager)
        {
            manager.AddCircle(2);
            manager.AddCircle(4);
        }
        /// <summary>
        /// Создать блок, описывающий символ 53 из pnt!.chr
        /// </summary>
        /// <param name="manager">Менеджер ресурсов</param>
        private void CreatePntOld53(ResourceManager manager) => manager.AddCircle(2, true);
        /// <summary>
        /// Создать блок, описывающий символ 100 из pnt!.chr
        /// </summary>
        /// <param name="manager">Менеджер ресурсов</param>
        private void CreatePntOld100(ResourceManager manager)
        {
            manager.AddCircle(3);
            manager.AddLine(new Point3d(-1 * SCALE, 0, 0), new Point3d(1 * SCALE, 0, 0));
            manager.AddLine(new Point3d(0, -1 * SCALE, 0), new Point3d(0, 1 * SCALE, 0));
        }
        /// <summary>
        /// Создать блок, описывающий символ 117 из pnt!.chr
        /// </summary>
        /// <param name="manager">Менеджер ресурсов</param>
        private void CreatePntOld117(ResourceManager manager) =>
            manager.AddPolygon(new Point2d[] { new Point2d(-3, -3), new Point2d(3, -3), new Point2d(0, 4) });
        /// <summary>
        /// Создать блок, описывающий символ 123 из pnt!.chr
        /// </summary>
        /// <param name="manager">Менеджер ресурсов</param>
        private void CreatePntOld123(ResourceManager manager) =>
            manager.AddPolygon(new Point2d[] { new Point2d(-3, 3), new Point2d(3, 3), new Point2d(0, -4) });
        /// <summary>
        /// Создать блок, описывающий символ 139 из pnt!.chr
        /// </summary>
        /// <param name="manager">Менеджер ресурсов</param>
        private void CreatePntOld139(ResourceManager manager) => manager.AddCircle(3);
    }
}