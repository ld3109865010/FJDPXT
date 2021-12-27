using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FJDPXT.EntityClass;
using FJDPXT.Model;

namespace FJDPXT.Areas.ElectronicsTicket.Controllers
{
    public class TicketPrintController : Controller
    {
        // GET: ElectronicsTicket/TicketPrint
        //电子票证-->电子客票打印换开
        //实例化model
        FJDPXTEntities1 myModel = new FJDPXTEntities1();

        #region 重定向到登录界面

        private int loginUserID = 0;
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            //try
            //{
            //    loginUserID = Convert.ToInt32(Session["userID"].ToString());
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e);
            //    filterContext.Result = Redirect(Url.Content("~/Main/Login"));//重定向到登录
            //}
            loginUserID = 1;
        }

        #endregion
        
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult SelectTicketsAll(LayuiTablePage layuiTablePage,string ticketNo)
        {
            var listETicket = from tbUser in myModel.S_User
                              join tbETicket in myModel.B_ETicket on tbUser.userID equals tbETicket.operatorID
                              join tbFlight in myModel.S_Flight on tbETicket.flightID equals tbFlight.flightID
                              join tbAirport in myModel.S_Airport on tbFlight.orangeID equals tbAirport.airportID
                              join tbAirportA in myModel.S_Airport on tbFlight.destinationID equals tbAirportA.airportID
                              join tbFlightCabin in myModel.S_FlightCabin on tbETicket.flightCabinID equals tbFlightCabin.flightCabinID
                              join tbCabinType in myModel.S_CabinType on tbETicket.cabinTypeID equals tbCabinType.cabinTypeID
                              join tbETicketStatus in myModel.S_ETicketStatus on tbETicket.eTicketStatusID equals tbETicketStatus.eTicketStatusID
                              join tbInvoiceStatus in myModel.S_InvoiceStatus on tbETicket.invoiceStatusID equals tbInvoiceStatus.invoiceStatusID
                              join tbPNRTicketing in myModel.B_PNRTicketing on tbETicket.ETicketID equals tbPNRTicketing.ETicketID
                              join tbPNR in myModel.B_PNR on tbPNRTicketing.PNRID equals tbPNR.PNRID
                              join tbPNRPassenger in myModel.B_PNRPassenger on tbPNRTicketing.PNRPassengerID equals tbPNRPassenger.PNRPassengerID
                              join tbPNRSegment in myModel.B_PNRSegment on tbPNRTicketing.PNRSegmentID equals tbPNRSegment.PNRSegmentID
                              join tbPassengerType in myModel.S_PassengerType on tbPNRPassenger.passengerTypeID equals tbPassengerType.passengerTypeID
                              join tbCertificatesType in myModel.S_CertificatesType on tbPNRPassenger.certificatesTypeID equals tbCertificatesType.certificatesTypeID//证件类型表
                              //使用orderby按学生ID进行descending（倒叙）排序  
                              //注意：要进行分页一定要用orderby排序
                              orderby tbETicket.ETicketID descending
                              select new ETicketVo
                              {
                                  /*
                                  * VO是value object的缩写;
                                  * 作用：它是为了页面显示取值方便。所以将数据封装为一个对象。这对象也就是我们说的VO；
                                  * VO则主要用于逻辑层(Controllers)和表示层(Views)之间数据处理封装。
                                  */
                                  ETicketID = tbETicket.ETicketID,//电子客票ID
                                  ticketNo = tbETicket.ticketNo,//票号  
                                  passengerName = tbPNRPassenger.passengerName,//旅客姓名
                                  flightCode = tbFlight.flightCode,//航班号    
                                  flightDateStr = tbFlight.flightDate.ToString(),//起飞日期
                                  departureTime = tbFlight.departureTime.ToString(),//起飞时间    
                                  orangeName = tbAirport.airportName + tbAirport.airportCode,//出发城市    
                                  destinationName = tbAirportA.airportName + tbAirportA.airportCode,//到达城市 
                                  certificatesType = tbCertificatesType.certificatesType,//证件类型    
                                  certificatesCode = tbPNRPassenger.certificatesCode,//证件号码
                                  eTicketStatus = tbETicketStatus.eTicketStatusName,//联票状态
                                  invoiceStatus = tbInvoiceStatus.invoiceStatus,//发票状态
                              };
            //条件筛选
            if (!string.IsNullOrEmpty(ticketNo))
            {
                listETicket = listETicket.Where(m => m.ticketNo.Contains(ticketNo));
            }
            var TotalRow = listETicket.Count();
            /*
             Skip表示从第几条数据开始，也就是说在这之前有多少条数据
             Take的意思是显示多少条数据，也就相当于我们常用的pagesize
             举例：Skip(1).Take(4)
             上面这段语句的意思是从第二条数据开始显示4条
           */
            List<ETicketVo> data = listETicket
                                    .Skip(layuiTablePage.GetStartIndex())
                                    .Take(layuiTablePage.limit)
                                    .ToList();
             
            //调用分页封装类
            LayuiTableData<ETicketVo> layuiTableData = new LayuiTableData<ETicketVo>()
            {
                data = data,
                count = TotalRow
            };

            return Json(layuiTableData, JsonRequestBehavior.AllowGet);
        }

        public ActionResult TicketPrint(int ETicketID)
        {
            try
            {
                ViewBag.ETicketID = ETicketID;
                return View();
            }
            catch (Exception e)
            {
                Console.Write(e);
                return Redirect("Index");
            }
        }

        public ActionResult doT4Print(int eTicketID)
        {
            ReturnJson rtMsg = new ReturnJson();
            rtMsg.State = false;
            try
            {
                B_ETicket eTicket = myModel.B_ETicket.Single(e => e.ETicketID == eTicketID);
                eTicket.invoiceStatusID = 2;//已开发票
                myModel.Entry(eTicket).State = System.Data.Entity.EntityState.Modified;
                if (myModel.SaveChanges() > 0)
                {
                    rtMsg.State = true;
                }

            }
            catch (Exception e)
            {
                Console.Write(e);
                rtMsg.Text = "数据异常，打印失败！";
            }
            return Json(rtMsg, JsonRequestBehavior.AllowGet);
        }

        public ActionResult SelectTicketById(int ETicketID)
        {
            try
            {
                //根据电子客票ID查询电子客票
                ETicketVo dbETicket = (from tbUser in myModel.S_User
                                       join tbETicket in myModel.B_ETicket on tbUser.userID equals tbETicket.operatorID
                                       join tbFlight in myModel.S_Flight on tbETicket.flightID equals tbFlight.flightID
                                       join tbAirport in myModel.S_Airport on tbFlight.orangeID equals tbAirport.airportID
                                       join tbAirportA in myModel.S_Airport on tbFlight.destinationID equals tbAirportA.airportID
                                       join tbFlightCabin in myModel.S_FlightCabin on tbETicket.flightCabinID equals tbFlightCabin.flightCabinID
                                       join tbCabinType in myModel.S_CabinType on tbETicket.cabinTypeID equals tbCabinType.cabinTypeID
                                       join tbETicketStatus in myModel.S_ETicketStatus on tbETicket.eTicketStatusID equals tbETicketStatus.eTicketStatusID
                                       join tbInvoiceStatus in myModel.S_InvoiceStatus on tbETicket.invoiceStatusID equals tbInvoiceStatus.invoiceStatusID
                                       join tbPNRTicketing in myModel.B_PNRTicketing on tbETicket.ETicketID equals tbPNRTicketing.ETicketID
                                       join tbPNR in myModel.B_PNR on tbPNRTicketing.PNRID equals tbPNR.PNRID
                                       join tbPNRPassenger in myModel.B_PNRPassenger on tbPNRTicketing.PNRPassengerID equals tbPNRPassenger.PNRPassengerID
                                       join tbPNRSegment in myModel.B_PNRSegment on tbPNRTicketing.PNRSegmentID equals tbPNRSegment.PNRSegmentID
                                       join tbPassengerType in myModel.S_PassengerType on tbPNRPassenger.passengerTypeID equals tbPassengerType.passengerTypeID
                                       orderby tbETicket.ETicketID descending//倒叙排序  
                                       where tbETicket.ETicketID == ETicketID
                                       select new ETicketVo
                                       {
                                           ETicketID = tbETicket.ETicketID,//电子客票ID
                                           PNRNo = tbPNR.PNRNo,//PNR
                                           flightCode = tbFlight.flightCode,//航班号=航班名称
                                           flightDate = tbFlight.flightDate,//航班日期  
                                           flightDateStr = tbFlight.flightDate.ToString(),//起飞日期 -字符串   
                                           departureTime = tbFlight.departureTime.ToString(),//起飞时间

                                           ticketNo = tbETicket.ticketNo,//票号
                                           TicketingTime = tbPNRTicketing.TicketingTime.ToString(),//出票日期
                                           userName = tbUser.userName,//营业员
                                           JobNumber = tbUser.jobNumber,//营业员编号
                                           passengerName = tbPNRPassenger.passengerName,//旅客姓名
                                           certificatesCode = tbPNRPassenger.certificatesCode,//证件号码
                                           passengerType = tbPassengerType.passengerType,//旅客类型
                                           passengerNo = tbPNRPassenger.passengerNo.Value,//旅客编号
                                           orangeCode = tbAirport.airportCode,//出发城市 -三字代码  
                                           orangeName = tbAirport.airportName,//出发城市 -始发地   
                                           destinationCode = tbAirportA.airportCode,//到达城市 -三字代码 
                                           destinationName = tbAirportA.airportName,//到达城市 -目的地 
                                           ticketPrice = tbETicket.ticketPrice,//票价                                          
                                           cabinTypeName = tbCabinType.cabinTypeName,//舱位等级
                                           segmentNo = tbPNRSegment.segmentNo,//航空公司记录编号
                                           seatNum = tbFlightCabin.seatNum,//座位数
                                           certificatesTypeID = tbPNRPassenger.certificatesTypeID,
                                           eTicketStatus = tbETicketStatus.eTicketStatusName,//联票状态
                                           EnglishName = tbETicketStatus.EnglishName,
                                           invoiceStatus = tbInvoiceStatus.invoiceStatus,//发票状态
                                           PNRSegmentID = tbPNRSegment.PNRSegmentID,//PNR航段ID
                                       }).SingleOrDefault();
                return Json(dbETicket, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Console.Write(e);
                return null;
            }
        }
    }
}