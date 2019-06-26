using System.Data;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel;
using Microsoft.AspNetCore.Http;

namespace Shove
{
    /// <summary>
    /// Excel 相关
    /// </summary>
    public class Excel
    {
        /// <summary>
        /// Excel 相关，通过 NPOI 实现，无需 Office COM 组件且不依赖 Office
        /// </summary>
        public class NPOIExcelHelper
        {
            /// <summary>
            /// 根据 Excel 列类型获取列的值
            /// </summary>
            /// <param name="cell">Excel 列</param>
            /// <returns></returns>
            private static string GetCellValue(ICell cell)
            {
                if (cell == null)
                {
                    return string.Empty;
                }

                switch (cell.CellType)
                {
                    case CellType.Blank:
                        return string.Empty;
                    case CellType.Boolean:
                        return cell.BooleanCellValue.ToString();
                    case CellType.Error:
                        return cell.ErrorCellValue.ToString();
                    case CellType.Numeric:
                        return cell.NumericCellValue.ToString();
                    case CellType.Unknown:
                    case CellType.String:
                        return cell.StringCellValue;
                    case CellType.Formula:
                        try
                        {
                            HSSFFormulaEvaluator e = new HSSFFormulaEvaluator(cell.Sheet.Workbook);
                            e.EvaluateInCell(cell);
                            return cell.ToString();
                        }
                        catch
                        {
                            return cell.NumericCellValue.ToString();
                        }
                    default:
                        return cell.ToString();//This is a trick to get the correct value of the cell. NumericCellValue will return a numeric value no matter the cell value is a date or a number
                }
            }

            /// <summary>
            /// 自动设置 Excel 列宽
            /// </summary>
            /// <param name="sheet">Excel工作簿</param>
            private static void AutoSizeColumns(ISheet sheet)
            {
                if (sheet.PhysicalNumberOfRows > 0)
                {
                    IRow headerRow = sheet.GetRow(0);

                    for (int i = 0, l = headerRow.LastCellNum; i < l; i++)
                    {
                        sheet.AutoSizeColumn(i);
                    }
                }
            }

            /// <summary>
            /// 保存 Excel 文档流到文件
            /// </summary>
            /// <param name="ms">Excel 文档流</param>
            /// <param name="fileName">文件名</param>
            private static void SaveToFile(MemoryStream ms, string fileName)
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    byte[] data = ms.ToArray();

                    fs.Write(data, 0, data.Length);
                    fs.Flush();

                    data = null;
                }
            }

            /// <summary>
            /// 输出文件到浏览器
            /// </summary>
            /// <param name="ms">Excel 文档流</param>
            /// <param name="context">HTTP上下文</param>
            /// <param name="fileName">文件名</param>
            private static void RenderToBrowser(MemoryStream ms, HttpContext context, string fileName)
            {
                //if (context.Request.Browser.Browser == "IE")
                //{
                //    fileName = HttpUtility.UrlEncode(fileName);
                //}

                context.Response.Headers.Add("Content-Disposition", "attachment;fileName=" + fileName);
                //context.Response.WriteAsync(ms); //[shove]
            }

            /// <summary>
            /// DataReader 转换成 Excel 文档流
            /// </summary>
            /// <param name="reader"></param>
            /// <returns></returns>
            public static MemoryStream RenderToExcel(IDataReader reader)
            {
                MemoryStream ms = new MemoryStream();

                using (reader)
                {
                    IWorkbook workbook = new HSSFWorkbook();
                    ISheet sheet = workbook.CreateSheet();
                    IRow headerRow = sheet.CreateRow(0);
                    int cellCount = reader.FieldCount;

                    // handling header.
                    for (int i = 0; i < cellCount; i++)
                    {
                        headerRow.CreateCell(i).SetCellValue(reader.GetName(i));
                    }

                    // handling value.
                    int rowIndex = 1;
                    while (reader.Read())
                    {
                        IRow dataRow = sheet.CreateRow(rowIndex);

                        for (int i = 0; i < cellCount; i++)
                        {
                            dataRow.CreateCell(i).SetCellValue(reader[i].ToString());
                        }

                        rowIndex++;
                    }

                    AutoSizeColumns(sheet);

                    workbook.Write(ms);
                    ms.Flush();
                    ms.Position = 0;
                }

                return ms;
            }

            /// <summary>
            /// DataReader 转换成 Excel 文档流，并保存到文件
            /// </summary>
            /// <param name="reader"></param>
            /// <param name="fileName">保存的路径</param>
            public static void RenderToExcel(IDataReader reader, string fileName)
            {
                using (MemoryStream ms = RenderToExcel(reader))
                {
                    SaveToFile(ms, fileName);
                }
            }

            /// <summary>
            /// DataReader 转换成 Excel 文档流，并输出到客户端
            /// </summary>
            /// <param name="reader"></param>
            /// <param name="context">HTTP上下文</param>
            /// <param name="fileName">输出的文件名</param>
            public static void RenderToExcel(IDataReader reader, HttpContext context, string fileName)
            {
                using (MemoryStream ms = RenderToExcel(reader))
                {
                    RenderToBrowser(ms, context, fileName);
                }
            }

            /// <summary>
            /// DataTable 转换成 Excel 文档流
            /// </summary>
            /// <param name="table"></param>
            /// <returns></returns>
            public static MemoryStream RenderToExcel(DataTable table)
            {
                MemoryStream ms = new MemoryStream();

                using (table)
                {
                    IWorkbook workbook = new HSSFWorkbook();
                    ISheet sheet = workbook.CreateSheet();
                    IRow headerRow = sheet.CreateRow(0);

                    // handling header.
                    foreach (DataColumn column in table.Columns)
                        headerRow.CreateCell(column.Ordinal).SetCellValue(column.Caption);//If Caption not set, returns the ColumnName value

                    // handling value.
                    int rowIndex = 1;

                    foreach (DataRow row in table.Rows)
                    {
                        IRow dataRow = sheet.CreateRow(rowIndex);

                        foreach (DataColumn column in table.Columns)
                        {
                            dataRow.CreateCell(column.Ordinal).SetCellValue(row[column].ToString());
                        }

                        rowIndex++;
                    }
                    AutoSizeColumns(sheet);

                    workbook.Write(ms);
                    ms.Flush();
                    ms.Position = 0;
                }

                return ms;
            }

            /// <summary>
            /// DataTable 转换成 Excel 文档流，并保存到文件
            /// </summary>
            /// <param name="table"></param>
            /// <param name="fileName">保存的路径</param>
            public static void RenderToExcel(DataTable table, string fileName)
            {
                using (MemoryStream ms = RenderToExcel(table))
                {
                    SaveToFile(ms, fileName);
                }
            }

            /// <summary>
            /// DataTable转换成Excel文档流，并输出到客户端
            /// </summary>
            /// <param name="table"></param>
            /// <param name="context"></param>
            /// <param name="fileName">输出的文件名</param>
            public static void RenderToExcel(DataTable table, HttpContext context, string fileName)
            {
                using (MemoryStream ms = RenderToExcel(table))
                {
                    RenderToBrowser(ms, context, fileName);
                }
            }

            /// <summary>
            /// Excel 文档流是否有数据
            /// </summary>
            /// <param name="excelFileStream">Excel文档流</param>
            /// <returns></returns>
            public static bool HasData(Stream excelFileStream)
            {
                return HasData(excelFileStream, 0);
            }

            /// <summary>
            /// Excel 文档流是否有数据
            /// </summary>
            /// <param name="excelFileStream">Excel 文档流</param>
            /// <param name="sheetIndex">表索引号，如第一个表为0</param>
            /// <returns></returns>
            public static bool HasData(Stream excelFileStream, int sheetIndex)
            {
                using (excelFileStream)
                {
                    IWorkbook workbook = new HSSFWorkbook(excelFileStream);

                    if (workbook.NumberOfSheets > 0)
                    {
                        if (sheetIndex < workbook.NumberOfSheets)
                        {
                            ISheet sheet = workbook.GetSheetAt(sheetIndex);
                            return sheet.PhysicalNumberOfRows > 0;
                        }
                    }
                }

                return false;
            }

            #region RenderFromExcel 从 Excel 文件流

            /// <summary>
            /// Excel 文档流转换成 DataTable
            /// 默认转换 Excel 的第一个表
            /// 第一行必须为标题行
            /// </summary>
            /// <param name="excelFileStream">Excel 文档流</param>
            /// <returns></returns>
            public static DataTable RenderFromExcel(Stream excelFileStream)
            {
                return RenderFromExcel(excelFileStream, 0, 0);
            }

            /// <summary>
            /// Excel 文档流转换成 DataTable
            /// 第一行必须为标题行
            /// </summary>
            /// <param name="excelFileStream">Excel文档流</param>
            /// <param name="sheetName">表名称</param>
            /// <returns></returns>
            public static DataTable RenderFromExcel(Stream excelFileStream, string sheetName)
            {
                return RenderFromExcel(excelFileStream, sheetName, 0);
            }

            /// <summary>
            /// Excel 文档流转换成 DataTable
            /// </summary>
            /// <param name="excelFileStream">Excel 文档流</param>
            /// <param name="sheetName">表名称</param>
            /// <param name="headerRowIndex">标题行索引号，如第一行为0</param>
            /// <returns></returns>
            public static DataTable RenderFromExcel(Stream excelFileStream, string sheetName, int headerRowIndex)
            {
                DataTable table = null;

                using (excelFileStream)
                {
                    IWorkbook workbook = new HSSFWorkbook(excelFileStream);
                    ISheet sheet = workbook.GetSheet(sheetName);

                    table = RenderFromExcel(sheet, headerRowIndex);
                }

                return table;
            }

            /// <summary>
            /// Excel 文档流转换成 DataTable
            /// 第一行必须为标题行
            /// </summary>
            /// <param name="excelFileStream">Excel 文档流</param>
            /// <param name="sheetIndex">表索引号，如第一个表为0</param>
            /// <returns></returns>
            public static DataTable RenderFromExcel(Stream excelFileStream, int sheetIndex)
            {
                return RenderFromExcel(excelFileStream, sheetIndex, 0);
            }

            /// <summary>
            /// Excel 文档流转换成 DataTable
            /// </summary>
            /// <param name="excelFileStream">Excel 文档流</param>
            /// <param name="sheetIndex">表索引号，如第一个表为0</param>
            /// <param name="headerRowIndex">标题行索引号，如第一行为0</param>
            /// <returns></returns>
            public static DataTable RenderFromExcel(Stream excelFileStream, int sheetIndex, int headerRowIndex)
            {
                DataTable table = null;

                using (excelFileStream)
                {
                    IWorkbook workbook = new HSSFWorkbook(excelFileStream);
                    ISheet sheet = workbook.GetSheetAt(sheetIndex);

                    table = RenderFromExcel(sheet, headerRowIndex);
                }

                return table;
            }
            
            #endregion

            #region RenderFromExcel 从 Excel 文件名

            /// <summary>
            /// Excel 表格转换成 DataTable
            /// </summary>
            /// <param name="fileName"></param>
            /// <returns></returns>
            public static DataTable RenderFromExcel(string fileName)
            {
                return RenderFromExcel(fileName, 0, 0);
            }

            /// <summary>
            /// Excel 表格转换成 DataTable
            /// </summary>
            /// <param name="fileName"></param>
            /// <param name="sheetName"></param>
            /// <returns></returns>
            public static DataTable RenderFromExcel(string fileName, string sheetName)
            {
                return RenderFromExcel(fileName, sheetName, 0);
            }

            /// <summary>
            /// Excel 表格转换成 DataTable
            /// </summary>
            /// <param name="fileName"></param>
            /// <param name="sheetName"></param>
            /// <param name="headerRowIndex">标题行索引号，如第一行为0</param>
            /// <returns></returns>
            public static DataTable RenderFromExcel(string fileName, string sheetName, int headerRowIndex)
            {
                DataTable table = null;
                byte[] fileBytes = System.IO.File.ReadAllBytes(fileName);
                Stream excelFileStream = new MemoryStream(fileBytes);

                using (excelFileStream)
                {
                    IWorkbook workbook = new HSSFWorkbook(excelFileStream);
                    ISheet sheet = workbook.GetSheet(sheetName);

                    table = RenderFromExcel(sheet, headerRowIndex);
                }

                return table;
            }

            /// <summary>
            /// Excel 表格转换成 DataTable
            /// </summary>
            /// <param name="fileName"></param>
            /// <param name="sheetIndex"></param>
            /// <returns></returns>
            public static DataTable RenderFromExcel(string fileName, int sheetIndex)
            {
                return RenderFromExcel(fileName, sheetIndex, 0);
            }

            /// <summary>
            /// Excel 表格转换成 DataTable
            /// </summary>
            /// <param name="fileName"></param>
            /// <param name="sheetIndex"></param>
            /// <param name="headerRowIndex">标题行索引号，如第一行为0</param>
            /// <returns></returns>
            public static DataTable RenderFromExcel(string fileName, int sheetIndex, int headerRowIndex)
            {
                DataTable table = null;
                byte[] fileBytes = System.IO.File.ReadAllBytes(fileName);
                Stream excelFileStream = new MemoryStream(fileBytes);

                using (excelFileStream)
                {
                    IWorkbook workbook = new HSSFWorkbook(excelFileStream);
                    ISheet sheet = workbook.GetSheetAt(sheetIndex);

                    table = RenderFromExcel(sheet, headerRowIndex);
                }

                return table;
            }

            #endregion

            /// <summary>
            /// Excel 表格转换成 DataTable
            /// </summary>
            /// <param name="sheet">表格</param>
            /// <param name="headerRowIndex">标题行索引号，如第一行为0</param>
            /// <returns></returns>
            private static DataTable RenderFromExcel(ISheet sheet, int headerRowIndex)
            {
                DataTable table = new DataTable();

                IRow headerRow = sheet.GetRow(headerRowIndex);
                int cellCount = headerRow.LastCellNum;//LastCellNum = PhysicalNumberOfCells
                int rowCount = sheet.LastRowNum;//LastRowNum = PhysicalNumberOfRows - 1

                //handling header.
                for (int i = headerRow.FirstCellNum; i < cellCount; i++)
                {
                    DataColumn column = new DataColumn(headerRow.GetCell(i).StringCellValue);
                    table.Columns.Add(column);
                }

                for (int i = (sheet.FirstRowNum + 1); i <= rowCount; i++)
                {
                    IRow row = sheet.GetRow(i);
                    DataRow dataRow = table.NewRow();

                    if (row != null)
                    {
                        for (int j = row.FirstCellNum; j < cellCount; j++)
                        {
                            if (row.GetCell(j) != null)
                                dataRow[j] = GetCellValue(row.GetCell(j));
                        }
                    }

                    table.Rows.Add(dataRow);
                }

                return table;
            }
        }
    }
}