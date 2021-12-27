using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using FJDPXT.EntityClass;
using FJDPXT.Model;

namespace FJDPXT.Areas.OthersMaintenance.Controllers
{
    public class ExportLKdataController : Controller
    {
        // GET: OthersMaintenance/ExportLKdata
        //其他-->导出旅客数据
        FJDPXTEntities1 myModel = new FJDPXTEntities1();
        #region 重定向到登录界面
        private int loginUserID = 0;

        /// <summary>
        /// 在执行Action方法前执行该方法
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            try
            {
                loginUserID = Convert.ToInt32(Session["userID"].ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                filterContext.Result = Redirect(Url.Content("~/Main/Login"));//重定向到登录
            }
        }
        #endregion

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult SelectPassengerData(LayuiTablePage layuiTablePage, string startEndDate)
        {
            var query = from tabPNRPassenger in myModel.B_PNRPassenger
                        join tbPassengerType in myModel.S_PassengerType
                          on tabPNRPassenger.passengerTypeID equals tbPassengerType.passengerTypeID
                        join tbCertificatesType in myModel.S_CertificatesType
                          on tabPNRPassenger.certificatesTypeID equals tbCertificatesType.certificatesTypeID
                        join tbPNR in myModel.B_PNR
                          on tabPNRPassenger.PNRID equals tbPNR.PNRID
                        orderby tabPNRPassenger.PNRPassengerID descending
                        select new PassengerVo()
                        {
                            passengerName = tabPNRPassenger.passengerName,
                            passengerType = tbPassengerType.passengerType,
                            certificatesType = tbCertificatesType.certificatesType,
                            certificatesCode = tabPNRPassenger.certificatesCode,
                            contactName = tbPNR.contactName,
                            contactPhone = tbPNR.contactPhone,
                            createTime = tbPNR.createTime.Value,
                        };
            //判断是否选择时间段并条件筛选
            if (!string.IsNullOrEmpty(startEndDate))
            {
                startEndDate = startEndDate.Replace(" - ", "~");
                string[] strs = startEndDate.Split('~');////根据 "~"分割字符串
                if (strs.Length == 2)
                {
                    DateTime dtStart = Convert.ToDateTime(strs[0]);
                    DateTime dtEnd = Convert.ToDateTime(strs[1]);
                    dtEnd = dtEnd.AddDays(1);//DateTime获取到哪天 而时分秒默认为00:00:00; 
                    query = query.Where(o => o.createTime >= dtStart && o.createTime < dtEnd);
                }
            }
            //查询总条数
            int totalRow = query.Count();

            //分页查询数据
            List<PassengerVo> listData = query
                .Skip(layuiTablePage.GetStartIndex())
                .Take(layuiTablePage.limit)
                .ToList();

            //构建返回页面数据
            LayuiTableData<PassengerVo> layuiTableData = new LayuiTableData<PassengerVo>()
            {
                count = totalRow,
                data = listData
            };

            return Json(layuiTableData, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ExportLKDATA(string startEndDate) {
            try
            {
                #region 查询出需要导出的数据
                var query = from tabPNRPassenger in myModel.B_PNRPassenger
                            join tbPassengerType in myModel.S_PassengerType
                              on tabPNRPassenger.passengerTypeID equals tbPassengerType.passengerTypeID
                            join tbCertificatesType in myModel.S_CertificatesType
                              on tabPNRPassenger.certificatesTypeID equals tbCertificatesType.certificatesTypeID
                            join tbPNR in myModel.B_PNR
                              on tabPNRPassenger.PNRID equals tbPNR.PNRID
                            orderby tabPNRPassenger.PNRPassengerID
                            select new PassengerVo()
                            {
                                passengerName = tabPNRPassenger.passengerName,
                                passengerType = tbPassengerType.passengerType,
                                certificatesType = tbCertificatesType.certificatesType,
                                certificatesCode = tabPNRPassenger.certificatesCode,
                                contactName = tbPNR.contactName,
                                contactPhone = tbPNR.contactPhone,
                                createTime = tbPNR.createTime.Value,
                            };
                //判断是否选择时间段并条件筛选
                if (!string.IsNullOrEmpty(startEndDate))
                {
                    startEndDate = startEndDate.Replace(" - ", "~");
                    string[] strs = startEndDate.Split('~');////根据 "~"分割字符串
                    if (strs.Length == 2)
                    {
                        DateTime dtStart = Convert.ToDateTime(strs[0]);
                        DateTime dtEnd = Convert.ToDateTime(strs[1]);
                        dtEnd = dtEnd.AddDays(1);//DateTime获取到哪天 而时分秒默认为00:00:00; 
                        query = query.Where(o => o.createTime >= dtStart && o.createTime < dtEnd);
                    }
                }
                List<PassengerVo> list = query.ToList();

                #endregion

                #region NPOI导出Excel

                //1-创建工作薄
                NPOI.HSSF.UserModel.HSSFWorkbook workbook = new NPOI.HSSF.UserModel.HSSFWorkbook();
                //2-创建工作表
                //NPOI.SS.UserModel.ISheet sheet1 = workbook.CreateSheet("旅客信息");
                NPOI.SS.UserModel.ISheet sheet1 = workbook.CreateSheet();
                workbook.SetSheetName(0, "旅客信息");//修改工作表名称

                //3-设置表标题(行)
                //3.1创建行
                NPOI.SS.UserModel.IRow rowTitle = sheet1.CreateRow(0);//索引
                rowTitle.HeightInPoints = 35;//行高 HeightInPoints的单位是点，而Height的单位是1/20个点，所以Height的值永远是HeightInPoints的20倍
                //3.2-创建单元格(列)
                NPOI.SS.UserModel.ICell cell0 = rowTitle.CreateCell(0);
                //3.3-单元格设置值
                string strTitle = "旅客数据";
                //拼接标题
                if (!string.IsNullOrEmpty(startEndDate))
                {
                    strTitle += "    " + startEndDate;
                }
                cell0.SetCellValue(strTitle);
                //3.4-合并单元格
                sheet1.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(0, 0, 0, 6));/*firstRow,lastRow,firstCol,lastCol*/
                //3.5设置单元格样式
                NPOI.SS.UserModel.ICellStyle cellStyle_Title = workbook.CreateCellStyle();
                cellStyle_Title.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;//水平居中
                cellStyle_Title.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;//垂直居中
                NPOI.SS.UserModel.IFont font_title = workbook.CreateFont();//声明字体
                font_title.Color = NPOI.HSSF.Util.HSSFColor.Blue.Index;//设置字体颜色
                font_title.Boldweight = (short)NPOI.SS.UserModel.FontBoldWeight.Bold;//加粗体
                font_title.FontHeightInPoints = 18;//设置字体大小
                cellStyle_Title.SetFont(font_title);//设置单元格字体

                cell0.CellStyle = cellStyle_Title;//设置单元格样式

                //4-设置表头
                //4.1-创建一行
                NPOI.SS.UserModel.IRow row1 = sheet1.CreateRow(1);//给sheet添加第一行的表头行
                row1.Height = 22 * 20;//设置行高
                //4.2创建单元格 并设置值
                row1.CreateCell(0).SetCellValue("序号");
                row1.CreateCell(1).SetCellValue("旅客姓名");
                row1.CreateCell(2).SetCellValue("旅客类型");
                row1.CreateCell(3).SetCellValue("证件类型");
                row1.CreateCell(4).SetCellValue("证件号码");
                row1.CreateCell(5).SetCellValue("联系人姓名");
                row1.CreateCell(6).SetCellValue("联系人电话");
                //4.3创建表头的样式
                NPOI.SS.UserModel.ICellStyle cellStyle_header = workbook.CreateCellStyle();
                cellStyle_header.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;//水平居中
                cellStyle_header.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;//垂直居中
                //设置背景颜色
                cellStyle_header.FillPattern = NPOI.SS.UserModel.FillPattern.SolidForeground;
                cellStyle_header.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Aqua.Index;
                //设置边框实线
                cellStyle_header.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
                cellStyle_header.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
                cellStyle_header.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
                cellStyle_header.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
                //循坏给单元格设置样式
                for (int i = 0; i < row1.Cells.Count; i++)
                {
                    row1.GetCell(i).CellStyle = cellStyle_header;
                }

                //5.遍历查询到的数据,设置表格数据
                //5.1-创建数据内部部分 单元格样式
                NPOI.SS.UserModel.ICellStyle cellstyle_value = workbook.CreateCellStyle();//声明样式
                cellstyle_value.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;//水平居中
                cellstyle_value.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;//垂直居中
                //设置边框线为实线
                cellstyle_value.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
                cellstyle_value.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
                cellstyle_value.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
                cellstyle_value.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
                //5.2-遍历数据,创建数据部分行列
                for (int i = 0; i < list.Count; i++)
                {
                    //5.2.1-创建行
                    NPOI.SS.UserModel.IRow row = sheet1.CreateRow(2 + i);//标题和表头已经占了两行
                    row.Height = 22 * 20;//设置行高
                    //5.2.2-创建单元格，并设置值
                    row.CreateCell(0).SetCellValue(i + 1);//序号
                    row.CreateCell(1).SetCellValue(list[i].passengerName);
                    row.CreateCell(2).SetCellValue(list[i].passengerType);
                    row.CreateCell(3).SetCellValue(list[i].certificatesType);
                    row.CreateCell(4).SetCellValue(list[i].certificatesCode);
                    row.CreateCell(5).SetCellValue(list[i].contactName);
                    row.CreateCell(6).SetCellValue(list[i].contactPhone);
                    //5.2.3-循坏给每个单元格设置样式
                    for (int j = 0; j < row.Cells.Count; j++)
                    {
                        row.GetCell(j).CellStyle = cellstyle_value;
                    }
                }
                //6-设置列宽为自适应
                for (int i = 0; i < sheet1.GetRow(1).Cells.Count; i++)
                {
                    sheet1.AutoSizeColumn(i);
                    sheet1.SetColumnWidth(i, sheet1.GetColumnWidth(i) * 17 / 10);
                }
                //7-把创建好的Excel输出到浏览器
                string fileName = "旅客信息" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-ffff") + ".xls";
                //把Excel转化为流输出
                MemoryStream BookStream = new MemoryStream();//定义流
                workbook.Write(BookStream);//将工作薄写入流
                BookStream.Seek(0, SeekOrigin.Begin);//输出之前调用Seek(偏移量,游标位置)
                return File(BookStream, "application/vnd.ms-excel", fileName);

                #endregion

                #region 根据模板导出数据
                ////== 1 - 检查模板文件是否存在
                //// Server.MapPath 将相对的路径转为实际的物理路径
                //string templatePath = Server.MapPath("~/Document/LKDataTemplate.xls");
                ////模板是否存在
                //if (!System.IO.File.Exists(templatePath))
                //{
                //    //如果不存在,就返回失败信息
                //    return Content("导出失败,请联系网站管理人员");
                //}
                ////使用NPOI打开模板
                //FileStream templateStream = System.IO.File.Open(templatePath, FileMode.Open);
                ////使用NPOI打开模板获取一个工作薄
                //NPOI.HSSF.UserModel.HSSFWorkbook excelBookTemplate = new NPOI.HSSF.UserModel.HSSFWorkbook.(templateStream);
                #endregion
            }
            catch (Exception e)
            {
                Console.Write(e);
                return Content("数据导出异常");
            }
        }
    }
}