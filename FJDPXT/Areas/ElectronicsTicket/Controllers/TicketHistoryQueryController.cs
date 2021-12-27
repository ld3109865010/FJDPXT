using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FJDPXT.Model;
using FJDPXT.EntityClass;

namespace FJDPXT.Areas.ElectronicsTicket.Controllers
{
    public class TicketHistoryQueryController : Controller
    {
        // GET: ElectronicsTicket/TicketHistoryQuery
        //电子票证--票证历史记录查询
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
            //查询所有的机场信息
            List<S_Airport> airports = (from tbAirport in myModel.S_Airport
                                        select tbAirport).ToList();
            ViewBag.airports = airports;
            return View();
        }

        public ActionResult SelectTicketHistoryAll(LayuiTablePage layuiTablePage, string ticketNo, string passengerName, string flightCode, string flightDateStr, int? orangeId, int? destinationId)
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
                                  ticketNo = tbETicket.ticketNo,//票号  
                                  passengerName = tbPNRPassenger.passengerName,//旅客姓名
                                  flightCode = tbFlight.flightCode,//航班号    
                                  flightDateStr = tbFlight.flightDate.ToString(),//起飞日期
                                  flightDate = tbFlight.flightDate,
                                  departureTime = tbFlight.departureTime.ToString(),//起飞时间    
                                  orangeId = tbAirport.airportID,
                                  orangeName = tbAirport.airportName + tbAirport.airportCode,//出发城市    
                                  destinationId = tbAirportA.airportID,
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
            if (!string.IsNullOrEmpty(passengerName))
            {
                listETicket = listETicket.Where(m => m.passengerName.Contains(passengerName));
            }
            if (!string.IsNullOrEmpty(flightCode))
            {
                listETicket = listETicket.Where(m => m.flightCode.Contains(flightCode));
            }
            if (!string.IsNullOrEmpty(flightDateStr))
            {
                listETicket = listETicket.Where(m => m.flightDateStr.Contains(flightDateStr));
            }
            if (orangeId > 0)
            {
                listETicket = listETicket.Where(m => m.orangeId == orangeId);
            }
            if (destinationId>0)
            {
                listETicket = listETicket.Where(m => m.destinationId==destinationId);
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
    }
}