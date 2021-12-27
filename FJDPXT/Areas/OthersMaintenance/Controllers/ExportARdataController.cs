using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FJDPXT.EntityClass;
using System.IO;
using FJDPXT.Model;

namespace FJDPXT.Areas.OthersMaintenance.Controllers
{
    public class ExportARdataController : Controller
    {
        // GET: OthersMaintenance/ExportARdata
        //其他-->导出AR数据
        //实例化myModel
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

        public ActionResult SelectARDate(LayuiTablePage layuiTablePage, string startEndDate)
        {
            var query = from tbOrder in myModel.B_Order
                        join tbPNR in myModel.B_PNR on tbOrder.PNRID equals tbPNR.PNRID
                        join tbUser in myModel.S_User on tbOrder.operatorID equals tbUser.userID
                        join tbUserGroup in myModel.S_UserGroup on tbUser.userGroupID equals tbUserGroup.userGroupID
                        orderby tbOrder.orderID descending
                        select new ExportARdataVo
                        {
                            orderNo = tbOrder.orderNo,//订单编号
                            payTime = tbOrder.payTime,//支付时间(筛选数据)
                            payTimeStr = tbOrder.payTime.ToString(),//支付时间(字符串)
                            totalPrice = tbOrder.totalPrice,//总票价
                            agencyFee = tbOrder.agencyFee,//代理费
                            payMoney = tbOrder.payMoney,//支付金额
                            userGroup = tbUserGroup.userGroupNumber,//用户组号
                            jobNumber = tbUser.jobNumber,//工号
                            PNR = tbPNR.PNRNo,//PNR编号
                            orderStatus = tbOrder.orderStatus//订单状态
                        };
            //判断是否选择了时间段
            if (!string.IsNullOrEmpty(startEndDate))
            {
                startEndDate = startEndDate.Replace(" - ", "~");//加了空格才能选中2020-07-15 - 2020-07-31 中间拼接的横杠
                string[] strs = startEndDate.Split('~');//根据“~”分割字符串
                if (strs.Length == 2)
                {
                    DateTime dtStart = Convert.ToDateTime(strs[0]);
                    DateTime dtEnd = Convert.ToDateTime(strs[1]);
                    query = query.Where(o => o.payTime >= dtStart && o.payTime <= dtEnd);
                }
            }
            //查询总条数
            int totalRow = query.Count();

            //分页查询数据
            List<ExportARdataVo> listData = query
                                            .Skip(layuiTablePage.GetStartIndex())
                                            .Take(layuiTablePage.limit)
                                            .ToList();
            //构建返回页面数据
            LayuiTableData<ExportARdataVo> layuiTableData = new LayuiTableData<ExportARdataVo>()
            {
                data = listData,
                count = totalRow
            };

            return Json(layuiTableData, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ExportARData(string startEndDate)
        {
            try
            {
                #region 筛选导出数据

            var query = from tbOrder in myModel.B_Order
                        join tbPNR in myModel.B_PNR on tbOrder.PNRID equals tbPNR.PNRID
                        join tbUser in myModel.S_User on tbOrder.operatorID equals tbUser.userID
                        join tbUserGroup in myModel.S_UserGroup on tbUser.userGroupID equals tbUserGroup.userGroupID
                        orderby tbOrder.orderID
                        select new ExportARdataVo
                        {
                            orderNo = tbOrder.orderNo,//订单编号
                            payTime = tbOrder.payTime,//支付时间(筛选数据)
                            payTimeStr = tbOrder.payTime.ToString(),//支付时间(字符串)
                            totalPrice = tbOrder.totalPrice,//总票价
                            agencyFee = tbOrder.agencyFee,//代理费
                            payMoney = tbOrder.payMoney,//支付金额
                            userGroup = tbUserGroup.userGroupNumber,//用户组号
                            jobNumber = tbUser.jobNumber,//工号
                            PNR = tbPNR.PNRNo,//PNR编号
                            orderStatus = tbOrder.orderStatus//订单状态
                        };
            //判断是否选择了时间段
            if (!string.IsNullOrEmpty(startEndDate))
            {
                startEndDate = startEndDate.Replace(" - ", "~");//加了空格才能选中2020-07-15 - 2020-07-31 中间拼接的横杠
                string[] strs = startEndDate.Split('~');//根据“~”分割字符串
                if (strs.Length == 2)
                {
                    DateTime dtStart = Convert.ToDateTime(strs[0]);
                    DateTime dtEnd = Convert.ToDateTime(strs[1]);
                    dtEnd = dtEnd.AddDays(1);
                    query = query.Where(o => o.payTime >= dtStart && o.payTime < dtEnd);
                }
            }
            List<ExportARdataVo> list = query.ToList();

                #endregion

                #region 根据模板文件的Excel导出

                //==1-检查模板文件是否存在
                // Server.MapPath 将相对的路径转为实际的物理路径
                string templatePath = Server.MapPath("~/Document/ARDataTemplate.xls");
                //判断模板是否存在
                if (!System.IO.File.Exists(templatePath))
                {
                    //如果不存在,就返回失败信息
                    return Content("导出失败,请联系网站管理人员");
                }
                //==2-使用NPOI打开模板Excel
                //=2.1-使用文件打开模板文件
                FileStream templateStream = System.IO.File.Open(templatePath, FileMode.Open);
                //=2.2-使用NPOI打开模板Excel 得到一个工作簿
                NPOI.HSSF.UserModel.HSSFWorkbook excelBookTemplate = new NPOI.HSSF.UserModel.HSSFWorkbook(templateStream);
                //==3-打开模板所在第一个工作表 
                NPOI.SS.UserModel.ISheet sheet = excelBookTemplate.GetSheetAt(0);
                //构建单元格样式
                NPOI.SS.UserModel.ICellStyle style = excelBookTemplate.CreateCellStyle();

                //==4-设置标题，如果筛选时间段不为空就拼接上筛选时间段
                if (!string.IsNullOrEmpty(startEndDate))
                {
                    NPOI.SS.UserModel.IRow rowTitle = sheet.GetRow(0);
                    rowTitle.GetCell(0).SetCellValue("订单数据    " + startEndDate);
                }
                //==5-往模板中填充数据
                //=5.1-设置数据单元格的样式
                style.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;//水平居中
                style.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;//垂直居中
                                                                                     //设置边框为实线
                style.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
                style.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
                style.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
                style.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;

                //==5.2-开始填充数据
                int index = 2;//目前这个模板数据开始填充数据的行索引为2 标题行0 表头行1
                              //遍历查询出的数据 填充Excel单元格
                for (int i = 0; i < list.Count; i++)
                {
                    NPOI.SS.UserModel.IRow row = sheet.CreateRow(index);//给sheet添加一行
                    row.Height = 22 * 20;//设置行高
                                         //设置单元格数据
                    row.CreateCell(0).SetCellValue(i + 1);//序号
                    row.CreateCell(1).SetCellValue(list[i].orderNo);//订单号
                    row.CreateCell(2).SetCellValue(list[i].payTime.ToString());//支付时间
                    row.CreateCell(3).SetCellValue(list[i].totalPrice.ToString());//总金额
                    row.CreateCell(4).SetCellValue(list[i].agencyFee.ToString());//代理费
                    row.CreateCell(5).SetCellValue(list[i].payMoney.ToString());//支付金额
                    row.CreateCell(6).SetCellValue(list[i].userGroup);//用户组
                    row.CreateCell(7).SetCellValue(list[i].jobNumber);//工号
                    row.CreateCell(8).SetCellValue(list[i].PNR);//PNR
                                                                //设置单元格样式
                    for (int j = 0; j < row.Cells.Count; j++)
                    {
                        row.GetCell(j).CellStyle = style;
                    }
                    index++;
                }
                //以流的方式返回
                string fileName = "订单信息" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-ffff") + ".xls";
                //把Excel转化为流，输出
                MemoryStream BookStream = new MemoryStream();//定义内存流
                excelBookTemplate.Write(BookStream);//将工作薄写入内存流
                BookStream.Seek(0, SeekOrigin.Begin);/*将偏移量0设为游标的起点*///输出之前调用Seek（偏移量，游标位置）方法：获取文件流的长度
                                                                     /*参数:File(fileStream要发送到响应的流,
                                                                             contentType内容类型（MIME 类型）,
                                                                             fileDownloadName浏览器中显示的文件下载对话框内要使用的文件名。)  */
                return File(BookStream, "application/vnd.ms-excel", fileName);

                #endregion

                #region NPOI导出数据

            //    //创建工作薄
            //    NPOI.SS.UserModel.IWorkbook workbook = new NPOI.HSSF.UserModel.HSSFWorkbook();
            ////创建工作表
            //NPOI.SS.UserModel.ISheet sheet = workbook.CreateSheet("订单信息");
            ////创建标题行
            //NPOI.SS.UserModel.IRow rowTitle = sheet.CreateRow(0);
            //rowTitle.HeightInPoints = 35;//设置行高
            ////创建单元格
            //NPOI.SS.UserModel.ICell cellTiTle = rowTitle.CreateCell(0);
            //cellTiTle.SetCellValue("订单数据");//修改标题
            ////合并单元格
            //sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(0, 0, 0, 8));
            ////设置单元格样式
            //NPOI.SS.UserModel.ICellStyle cellTitle_style = workbook.CreateCellStyle();
            //cellTitle_style.Alignment =NPOI.SS.UserModel.HorizontalAlignment.Center;
            //cellTitle_style.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            //NPOI.SS.UserModel.IFont cell_font = workbook.CreateFont();
            //cell_font.Color = NPOI.HSSF.Util.HSSFColor.Blue.Index;
            //cell_font.FontHeight = 18 * 20;
            //cell_font.Boldweight = (short)NPOI.SS.UserModel.FontBoldWeight.Bold;
            //cellTitle_style.SetFont(cell_font);//设置单元格样式
            //cellTiTle.CellStyle = cellTitle_style;
            ////创建表头行
            //NPOI.SS.UserModel.IRow rowHeader = sheet.CreateRow(1);
            //rowHeader.HeightInPoints = 22;
            ////创建单元格并设置值
            //rowHeader.CreateCell(0).SetCellValue("序号");
            //rowHeader.CreateCell(1).SetCellValue("订单号");
            //rowHeader.CreateCell(2).SetCellValue("支付日期");
            //rowHeader.CreateCell(3).SetCellValue("总金额");
            //rowHeader.CreateCell(4).SetCellValue("代理费");
            //rowHeader.CreateCell(5).SetCellValue("实付金额");
            //rowHeader.CreateCell(6).SetCellValue("组号");
            //rowHeader.CreateCell(7).SetCellValue("工号");
            //rowHeader.CreateCell(8).SetCellValue("PNR");
            ////创建表头的样式
            //NPOI.SS.UserModel.ICellStyle cellHeader_style = workbook.CreateCellStyle();
            //cellHeader_style.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            //cellHeader_style.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            ////NPOI.SS.UserModel.IFont cellHeader_font = workbook.CreateFont();
            ////设置字体
            ////cellHeader_font.FontHeightInPoints = 30;
            ////cellHeader_font.Color = NPOI.HSSF.Util.HSSFColor.Red.Index;
            ////设置背景颜色
            //cellHeader_style.FillPattern = NPOI.SS.UserModel.FillPattern.SolidForeground;
            //cellHeader_style.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Aqua.Index;
            ////设置边框实线
            //cellHeader_style.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            //cellHeader_style.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            //cellHeader_style.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            //cellHeader_style.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            
            //for (int i = 0; i <rowHeader.Cells.Count; i++)
            //{
            //    rowHeader.GetCell(i).CellStyle = cellHeader_style;
            //}

            ////5.遍历查询到的数据,设置表格数据
            ////5.1-创建数据内部部分 单元格样式
            //NPOI.SS.UserModel.ICellStyle cell_style = workbook.CreateCellStyle();
            //cell_style.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            //cell_style.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;

            //cell_style.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            //cell_style.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            //cell_style.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            //cell_style.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;

            //for (int i = 0; i < list.Count; i++)
            //{
            //    //创建行
            //    NPOI.SS.UserModel.IRow row = sheet.CreateRow(i + 2);
            //    //设置行高
            //    row.HeightInPoints = 22;
            //    //创建单元格并设置值
            //    row.CreateCell(0).SetCellValue(i + 1);
            //    row.CreateCell(1).SetCellValue(list[i].orderNo);
            //    row.CreateCell(2).SetCellValue(list[i].payTimeStr);
            //    row.CreateCell(3).SetCellValue(list[i].totalPrice.ToString());
            //    row.CreateCell(4).SetCellValue(list[i].agencyFee.ToString());
            //    row.CreateCell(5).SetCellValue(list[i].payMoney.ToString());
            //    row.CreateCell(6).SetCellValue(list[i].userGroup);
            //    row.CreateCell(7).SetCellValue(list[i].jobNumber);
            //    row.CreateCell(8).SetCellValue(list[i].PNR);
            //    //循坏给每一个单元格附加样式
            //    for (int j = 0; j < row.Cells.Count; j++)
            //    {
            //        row.GetCell(j).CellStyle = cell_style;
            //    }
            //}
            ////6列宽自适应
            //for (int i = 0; i < sheet.GetRow(1).Cells.Count; i++)
            //{
            //    sheet.AutoSizeColumn(i);
            //    sheet.SetColumnWidth(i, sheet.GetColumnWidth(i) * 17 / 10);
            //}
            ////7.把创建好的Excel输出到浏览器中
            //string fileName ="订单信息"+ DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-ffff") + ".xls";
            ////把Excel转化为流输出
            //MemoryStream BookStream = new MemoryStream();//定义流
            //workbook.Write(BookStream);
            //BookStream.Seek(0, SeekOrigin.Begin);
            //return File(BookStream, "application/vnd.ms-excel", fileName);

            #endregion
            }
            catch (Exception e)
            {
                Console.Write(e);
                return Content("导出失败");
            }
           
        }
    }
}