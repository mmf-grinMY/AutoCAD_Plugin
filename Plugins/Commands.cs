using Plugins.Logging;
using Plugins.View;

using System.Collections.Generic;
using System;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

using static Plugins.Constants;
using System.Linq;
using Oracle.ManagedDataAccess.Client;

namespace Plugins
{
    public class Commands : IExtensionApplication
    {
        #region Private Methods

        /// <summary>
        /// Взять граничные точки области
        /// </summary>
        /// <param name="doc">Текущий документ</param>
        /// <returns>Граничные точки, в случае успеха и UndefinedType в противном случае</returns>
        IEnumerable<Point3d> GetPoints(Editor editor)
        {
            var pointOptions = new PromptPointOptions("\n\tЛевый нижний угол: ");
            var left = editor.GetPoint(pointOptions);
            var cornerOptions = new PromptCornerOptions("\n\tПравый верхний угол: ", left.Value);
            var right = editor.GetCorner(cornerOptions);

            return right.Status == PromptStatus.OK && left.Status == PromptStatus.OK
                ? new Point3d[] { left.Value, right.Value }
                : null;
        }
        /// <summary>
        /// Вывести сообщение об ошибке
        /// </summary>
        /// <param name="message">Содержимое сообщения</param>
        /// <exception cref="ArgumentException"></exception>
        void ShowError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException(nameof(message));

            System.Windows.MessageBox.Show(message, "Ошибка",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Инициализировать плагин
        /// </summary>
        public void Initialize()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;

            if (doc is null)
            {
                System.Windows.MessageBox.Show("Невозможен доступ к активному документу!", "Ошибка",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            Editor editor = null;
            Database db;
            ILogger logger = null;

            try
            {
                Constants.Initialize();

                logger = new FileLogger(nameof(Initialize));
                db = doc.Database ?? throw new ArgumentNullException(nameof(db), "Не удалось получить ссылку на БД документа!");
                editor = doc.Editor;

                editor.WriteMessage("Загрузка плагина прошла успешно!\n");
            }
            catch (System.Exception e)
            {
                var errorMessage = "При загрузке плагина произошла ошибка!" + Environment.NewLine + e.Message;
                logger.LogError(e);

                if (editor is null)
                {
                    ShowError(errorMessage);
                }
                else
                {
                    editor.WriteMessage(errorMessage);
                }
            }
        }
        /// <summary>
        /// Завершить работу плагина
        /// </summary>
        public void Terminate()
        {
            try
            {
                System.IO.File.Delete(DbConfigPath);
                UnloadFriendFolders();
            }
            catch (System.Exception e)
            {
                var logger = new FileLogger(nameof(Terminate));
                logger.LogError(e);
            }
        }

        #endregion

        #region Command Methods

        public const string DRAW_COMMAND = "MMP_DRAW";
        /// <summary>
        /// Отрисовать геометрию
        /// </summary>
        [CommandMethod(DRAW_COMMAND)]
        public void DrawCommand()
        {
            Session session = null;
            var logger = new FileLogger("main.log");

            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;

                try
                {
                    session = new Session(logger)
                    {
                        Bottom = long.MinValue,
                        Left = long.MinValue,
                        Right = long.MaxValue,
                        Top = long.MaxValue
                    };
                }
                catch (InvalidOperationException) 
                {
                    logger.LogInformation("Выполнение команды \"" + DRAW_COMMAND + "\" была остановлена пользователем!");
                    return; 
                }

                if (session.IsBoundingBoxChecked)
                {
                    var points = GetPoints(doc.Editor);

                    if (points != null && points.Count() == 2)
                    {
                        var point = points.ElementAt(0);
                        var corner = points.ElementAt(1);

                        const double precession = 1000;
                        session.Left = (long)(Math.Min(point.X, corner.X) * precession);
                        session.Right = (long)(Math.Max(point.X, corner.X) * precession);
                        session.Bottom = (long)(Math.Min(point.Y, corner.Y) * precession);
                        session.Top = (long)(Math.Max(point.Y, corner.Y) * precession);
                    }
                }

                session.Run();
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
            }
            finally
            {
                session?.Dispose();
            }
        }
        /// <summary>
        /// Команда инспектирования отрисованных примитивов
        /// </summary>
        // [CommandMethod("MMP_INSPECT_EXT_DB")]
        public void InspectExtDB()
        {
            OracleDbDispatcher connection;

            try
            {
                connection = new OracleDbDispatcher(null, "");
            }
            catch (InvalidOperationException) { return; }

            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            var editor = doc.Editor;
            var options = new PromptEntityOptions("\nВыберите объект: ");
            using (var transaction = doc.TransactionManager.StartTransaction())
            {
                PromptEntityResult result = null;
                while ((result = editor.GetEntity(options)).Status == PromptStatus.OK)
                {
                    var buffer = transaction.GetObject(result.ObjectId, OpenMode.ForRead).XData;
                    if (buffer == null)
                    {
                        editor.WriteMessage("\nУ объекта отсутствуют параметры XData");
                        continue;
                    }

                    string xData;
                    const int ERROR_SYSTEM_ID = -1;

                    int systemId;
                    string[] row;
                    string linkField;

                    if ((linkField = buffer.GetXData(LINK_FIELD)) == string.Empty
                        || (systemId = (xData = buffer.GetXData(SYSTEM_ID)) == string.Empty 
                            ? ERROR_SYSTEM_ID 
                            : Convert.ToInt32(xData)) == ERROR_SYSTEM_ID
                        || (xData = buffer.GetXData(BASE_NAME)) == string.Empty
                        || (row = xData.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)).Length <= 1)
                    {
                        editor.WriteMessage("Атрибутивная таблица к объекту отсутствует!");
                        continue;
                    }

                    string baseName = row[1];
                    string baseCapture = baseName;
                    var link = connection.GetExternalDbLink(baseName);

                    var fields = link.Split('\n');
                    var fieldNames = new Dictionary<string, string>();

                    if (fields.Length > 5) fieldNames = DbHelper.ParseFieldNames(fields);

                    if (fields.Length > 2) baseCapture = fields[1];

                    new ExternalDbWindow(connection.GetDataTable(DbHelper.CreateCommand(baseName, linkField, systemId, fieldNames)).DefaultView)
                    {
                        Title = baseCapture
                    }.ShowDialog();
                }
            }
        }
        #endregion
        [CommandMethod("VRM_INSPECT_EXT_DB")]
        static public void InspectExtDB0()
        {
            VarOpenTrans vot = new VarOpenTrans();
            if (!vot.tryConnectAndSave())
                return;

            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            PromptEntityOptions opt =
              new PromptEntityOptions(
                "\nSelect entity: "
              );

            Transaction tr =
              doc.TransactionManager.StartTransaction();
            using (tr)
            {
                while (true)
                {

                    PromptEntityResult res =
                      ed.GetEntity(opt);
                    if (res.Status == PromptStatus.OK)
                    {
                        DBObject obj =
                          tr.GetObject(
                            res.ObjectId,
                            OpenMode.ForRead
                          );

                        ResultBuffer rb = obj.XData;
                        if (rb == null)
                        {
                            ed.WriteMessage(
                              "\nEntity does not have XData attached."
                            );
                        }
                        else
                        {
                            string cross_guid2;
                            getXDataMok(obj.XData, "varMM_SystemID", out cross_guid2);
                            if (cross_guid2 != "")
                            {
                                int systemID = Convert.ToInt32(cross_guid2);
                                getXDataMok(obj.XData, "varMM_BaseName", out cross_guid2);
                                if (cross_guid2 != "")
                                {
                                    string baseName = "";
                                    var row = cross_guid2.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (row.Count() > 1)
                                        baseName = row[1];
                                    getXDataMok(obj.XData, "varMM_LinkField", out cross_guid2);
                                    if (cross_guid2 != "")
                                    {
                                        string linkField = cross_guid2;
                                        string baseCapture = baseName;
                                        string outData;
                                        vot.parseExternalDBLINK(baseName, out outData, vot.dbcon);
                                        string[] f = outData.Split('\n');
                                        if (f.Count() > 2)
                                            baseCapture = f[1];
                                        vot.getExternalDB(baseName, baseCapture, linkField, systemID, vot.dbcon);
                                    }
                                }

                            }
                        }
                    }
                    else
                        break;
                }
            }
            vot.dbcon.Close();
        }
        public static void getXDataMok(ResultBuffer rb, string RegAppName, out string RegValue)
        {
            bool proc_fl_1 = false;
            RegValue = "";
            foreach (TypedValue tv in rb)
            {
                if (proc_fl_1)
                {
                    RegValue = tv.Value.ToString();
                    proc_fl_1 = false;
                }
                if ((tv.TypeCode == (int)DxfCode.ExtendedDataRegAppName) && (tv.Value.ToString() == RegAppName))
                {
                    proc_fl_1 = true;
                }
            }
        }
    }
    sealed class NotDrawingLineException : System.Exception { }
    public class SubLayerGuid
    {
        public string SubGuid;
        public string SubName;
        public string LayerGuid;
        public string LayerName;
        public string SubLayerBaseName;
        public string SubLayerParentFiedls;
        public string SubLayerChildFields;

        public SubLayerGuid(string s1, string s2, string s3, string s4, string s5, string s6, string s7)
        {
            SubGuid = s1;
            SubName = s2;
            LayerGuid = s3;
            LayerName = s4;
            SubLayerBaseName = s5;
            SubLayerParentFiedls = s6;
            SubLayerChildFields = s7;
        }

    }
    class VarOpenTrans
    {
        // Tables sufixes
        public string sufTransOpen = "_TRANS_OPEN";
        public string sufTransClone = "_TRANS_CLONE";
        public string sufTransControl = "_TRANS_CONTROL";
        public string sufTransSubLayers = "_TRANS_OPEN_SUBLAYERS";

        public string host;
        public int port = 1521;
        public string sid;
        public string network_user;
        public string network_pass;

        public List<string> WktStringList;
        public List<string> DrawStringList;
        public List<string> ParamStringList;
        public List<SubLayerGuid> NameGuidList;
        public List<string> SubLayerGuidList;
        public List<string> SubLayerNameList;
        public List<string> LayerGuidList;
        public List<string> LayerNameList;
        public List<int> SystemIdList;
        public Dictionary<string, string> field_names = new Dictionary<string, string>();
        public int WktCount = 0;
        public OracleConnection dbcon = null;
        public bool tryConnectAndSave()
        {
            dbcon = new OracleConnection(DbHelper.SimpleConnectionStr);
            try
            {
                dbcon.Open();
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                return false;
            }
            return true;
        }
        public bool parseExternalDBLINK(string BaseName, out string table_data_fin, OracleConnection dbcon)
        {
            table_data_fin = "Empty";
            field_names.Clear();
            try
            {
                if (true)
                {
                    string command_str = "SELECT * FROM LINKS WHERE NAME = '" + BaseName + "'";
                    OracleCommand command = new OracleCommand(command_str, dbcon);
                    try
                    {
                        OracleDataReader dr = command.ExecuteReader();
                        while (dr.Read())
                        {
                            table_data_fin = dr["DATA"].ToString();
                        }
                        dr.Close();
                    }
                    catch (System.Exception ex)
                    {
                        System.Windows.MessageBox.Show(ex.Message);
                        return false;
                    }
                }
                string[] f = table_data_fin.Split('\n');
                if (f.Count() > 5)
                {
                    bool fields_fl = true;
                    for (int i = 0; i < f.Count(); i++)
                    {
                        if (fields_fl)
                        {
                            if (f[i] == "FIELDS")
                            {
                                fields_fl = false;
                            }
                            continue;
                        }
                        if (f[i] == "ENDFIELDS")
                        {
                            break;
                        }
                        if (f[i].Contains("+"))
                            continue;
                        var row_0 = f[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (row_0.Count() > 1)
                        {
                            string field_value = "";
                            for (int j = 1; j < row_0.Count(); j++)
                            {
                                field_value += row_0[j] + "_";
                            }

                            if (!field_names.ContainsKey(row_0[0]))
                            {
                                field_names.Add(row_0[0], field_value);
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
            return true;
        }
        public bool getExternalDB(string BaseName, string BaseCaption, string ChildField, int SystemID, OracleConnection dbcon)
        {
            try
            {
                if (true)
                {
                    string command_str = "SELECT ";
                    int field_count = 0;
                    foreach (var item in field_names)
                    {
                        if (field_count > 0)
                            command_str += " , ";
                        command_str += item.Key + " as \"" + item.Value + "\"";
                        field_count++;
                    }
                    command_str += " FROM " + BaseName + " WHERE " + ChildField + " = " + SystemID.ToString();
                    OracleCommand command = new OracleCommand(command_str, dbcon);
                    try
                    {
                        OracleDataReader dr = command.ExecuteReader();
                        System.Data.DataTable dataTable = new System.Data.DataTable();
                        dataTable.Load(dr);
                        ExternalDB form_ex = new ExternalDB();
                        form_ex.Text = BaseCaption;
                        form_ex.dataGridView1.DataSource = dataTable;
                        form_ex.ShowDialog();
                        form_ex.Dispose();
                        dr.Close();
                    }
                    catch (System.Exception ex)
                    {
                        System.Windows.MessageBox.Show(ex.Message);
                        return false;
                    }
                }
                //dbcon.Close();
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
            return true;
        }
    }
}