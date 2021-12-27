using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FJDPXT.Model;
using FJDPXT.EntityClass;
using System.Transactions;

namespace FJDPXT.Areas.ElectronicsTicket.Controllers
{
    public class TicketModifyController : Controller
    {
        // GET: ElectronicsTicket/TicketModify
        //电子票证-->票证查询/修改

        //实例化model
        FJDPXTEntities1 myModel = new FJDPXTEntities1();

        #region 重定向到登录界面
        
        private int loginUserID = 0;

        //protected override void OnActionExecuted(ActionExecutedContext filterContext)
        //{
        //    base.OnActionExecuted(filterContext);

        //    //验证用户登录
        //    if (Session["userID"] == null)
        //    {
        //        //没有登录，重定向 ,,不会执行后续的方法，而是直接跳转到登录页面
        //        filterContext.Result = Redirect(@Url.Content("~/Main/Login"));
        //    }
        //    else
        //    {
        //        loginUserID = (int)Session["userID"];
        //    }
        //    loginUserID = 1;
        //}

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
            //loginUserID = 1;
        }

        #endregion

        #region 主页面
        
        public ActionResult Index()
        {
            //查询所有的城市机场信息
            List<S_Airport> airports= (from tbAirport in myModel.S_Airport
                                 select tbAirport).ToList();
            //查询所有的证件类型
            List<S_CertificatesType> CertificatesTypes = (from tbCertificatesType in myModel.S_CertificatesType
                                                          select tbCertificatesType).ToList();

            //传递到页面
            ViewBag.airports = airports;
            ViewBag.CertificatesTypes = CertificatesTypes;

            return View();
        }

        #endregion

        #region 票证信息数据初始化&多条件查询


        /// <summary>
        /// 票证结果(条件查询)
        /// </summary>
        /// <param name="layuiTablePage"></param>
        /// <param name="flightDate">航班日期</param>
        /// <param name="orange">起始机场</param>
        /// <param name="destination">到达机场</param>
        /// <param name="segmentNo">航段编号</param>
        /// <param name="ticketNo">机票号</param>
        /// <param name="flightCode">航班号</param>
        /// <param name="StarTime">起始日期</param>
        /// <param name="EndTime">结束日期</param>
        /// <param name="certificatesTypeID">证件类型</param>
        /// <param name="certificatesCode">证件号码</param>
        /// <returns></returns>
        public ActionResult SelectTickets(LayuiTablePage layuiTablePage, string flightDate, int? orangeId, int? destinationId, int? segmentNo, string ticketNo, string flightCode, string StarTime, string EndTime, int? certificatesTypeID, string certificatesCode)
        {
            

            //(1)方法调用
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
                              orderby tbETicket.ETicketID descending//倒叙排序               
                              select new ETicketVo
                              {
                                  PNRID = tbPNR.PNRID, //PNRID
                                  ETicketID = tbETicket.ETicketID,//电子客票ID
                                  PNRNo = tbPNR.PNRNo,//PNR
                                  flightCode = tbFlight.flightCode,//航班号=航班名称
                                  ticketNo = tbETicket.ticketNo,//票号
                                  TicketingTime = tbPNRTicketing.TicketingTime.ToString(),//出票日期
                                  userName = tbUser.userName,//营业员
                                  JobNumber = tbUser.jobNumber,//营业员编号
                                  passengerName = tbPNRPassenger.passengerName,//旅客姓名
                                  certificatesCode = tbPNRPassenger.certificatesCode,//证件号码
                                  passengerType = tbPassengerType.passengerType,//旅客类型
                                  passengerNo = tbPNRPassenger.passengerNo.Value,//旅客编号
                                  orangeId = tbAirport.airportID,//出发城市 ID
                                  orangeCode = tbAirport.airportCode,//出发城市 -三字代码  
                                  orangeName = tbAirport.airportName,//出发城市 -始发地   
                                  destinationId = tbAirportA.airportID,//到达城市Id              
                                  destinationCode = tbAirportA.airportCode,//到达城市 -三字代码 
                                  destinationName = tbAirportA.airportName,//到达城市 -目的地
                                  flightDate = tbFlight.flightDate,//航班日期   
                                  flightDateStr = tbFlight.flightDate.ToString(),//起飞日期 -字符串  
                                  ticketPrice = tbETicket.ticketPrice,//票价
                                  cabinTypeName = tbCabinType.cabinTypeName,//舱位等级
                                  segmentNo = tbPNRSegment.segmentNo,//航空公司记录编号
                                  seatNum = tbFlightCabin.seatNum,//座位数
                                  certificatesTypeID = tbPNRPassenger.certificatesTypeID,
                                  eTicketStatus = tbETicketStatus.eTicketStatusName,//联票状态
                                  EnglishName = tbETicketStatus.EnglishName,
                                  invoiceStatus = tbInvoiceStatus.invoiceStatus,//发票状态
                                  PNRSegmentID = tbPNRSegment.PNRSegmentID,//PNR航段ID
                              };
            //条件筛选
            if (!string.IsNullOrEmpty(flightDate))
            {
                try
                {
                    DateTime detflightDate = Convert.ToDateTime(flightDate);
                    listETicket = listETicket.Where(p => p.flightDate == detflightDate);
                }
                catch (Exception e)
                {
                    Console.Write(e);
                }
            }
            //起飞机场（三字代码）
            if (orangeId > 0)
            {
                listETicket = listETicket.Where(m => m.orangeId == orangeId);
            }
            //到达机场（三字代码）
            if (destinationId > 0)
            {
                listETicket = listETicket.Where(m => m.destinationId == destinationId);
            }
            /// <param name="segmentNo">航空公司记录编号:航段编号</param>
            if (segmentNo > 0)
            {
                listETicket = listETicket.Where(m => m.segmentNo == segmentNo);
            }
            //机票号码
            if (!string.IsNullOrEmpty(ticketNo))
            {
                ticketNo = "E781-" + ticketNo;
                listETicket = listETicket.Where(m => m.ticketNo.Equals(ticketNo));
            }
            //航班号
            if (!string.IsNullOrEmpty(flightCode))
            {
                listETicket = listETicket.Where(m => m.flightCode.Equals(flightCode));
            }
            //起始日期和结束日期不为空
            if (!string.IsNullOrEmpty(StarTime) && !string.IsNullOrEmpty(EndTime))
            {
                try
                {
                    DateTime detStarTime = Convert.ToDateTime(StarTime);
                    DateTime detEndTime = Convert.ToDateTime(EndTime);
                    listETicket = listETicket.Where(p => p.flightDate >= detStarTime && p.flightDate <= detEndTime);
                }
                catch (Exception e)
                {
                    Console.Write(e);
                }
            }
            /// <param name="certificatesTypeID">证件类型</param>
            if (certificatesTypeID > 0)
            {
                listETicket = listETicket.Where(m => m.certificatesTypeID == certificatesTypeID);
            }
            /// <param name="certificatesCode">证件号码</param>
            if (!string.IsNullOrEmpty(certificatesCode))
            {
                listETicket = listETicket.Where(m => m.certificatesCode.Equals(certificatesCode));
            }
            var TotalRow = listETicket.Count();
            List<ETicketVo> data = listETicket
                                   .Skip(layuiTablePage.GetStartIndex())
                                   .Take(layuiTablePage.limit)
                                   .ToList();
            LayuiTableData<ETicketVo> layuiTableData = new LayuiTableData<ETicketVo>()
            {
                count = TotalRow,
                data = data
            };

            return Json(layuiTableData, JsonRequestBehavior.AllowGet);

        }

        #endregion

        #region 票证查询结果
        public ActionResult SelectTicketById(int ETicketID)
        {
            try
            {
                //根据电子客票ID查询电子客票
                var dbETicket = (from tbUser in myModel.S_User
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
                                           flightDate = tbFlight.flightDate,//航班日期   
                                           flightDateStr = tbFlight.flightDate.ToString(),//起飞日期 -字符串  
                                           departureTime = tbFlight.departureTime.ToString(),//起飞时间
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

                ViewBag.dbETicket = dbETicket;
                
                return Json(dbETicket, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Console.Write(e);
                return null;
            }

        }

        #endregion

        #region 票号跳转页面

        public ActionResult OrderInfoShow(int ETicketID)
        {
            try
            {
                //将数据传递到页面
                ViewBag.ETicketID = ETicketID;

                #region 打印换开
                //获取表格里面最大编号
                string strTicketNo = (from tbAcademe in myModel.B_ETicket
                                      select tbAcademe).ToList()
                                    .OrderByDescending(m => m.ticketNo).ToList().FirstOrDefault().ticketNo;
                //并去除前缀 “E781-", 获取最大票号+1.
                int currentNow = Convert.ToInt32(strTicketNo.Remove(0, 5));
                int currentNext = currentNow + 1;
                string strTicketNoNext = currentNext.ToString("0000000000");
                //传递到页面
                ViewBag.NewTicketNo = strTicketNoNext;

                #endregion

                #region 作废

                
                string strInvalidCodeNext = "";
                //获取表格里面最大的作废编号
                try
                {
                    string strInvalidCode = (from tbETicketInvalid in myModel.B_ETicketInvalid
                                             select tbETicketInvalid).ToList()
                                             .OrderByDescending(m => m.InvalidCode).ToList().FirstOrDefault().InvalidCode;
                    //并去除前缀 “ZF-202001070001", 获取最大票号+1.
                    int intMaximum = Convert.ToInt32(strInvalidCode.Remove(0, 11));
                    int intNext = intMaximum + 1;
                    strInvalidCodeNext="ZF-"+DateTime.Now.ToString("yyyyMMdd")+intNext.ToString("0000");
                }
                catch (Exception e)
                {
                    Console.Write(e);
                    
                }
                //传递到页面
                ViewBag.NewInvalidCode = strInvalidCodeNext;

                return View();
                #endregion
            }
            catch (Exception e)
            {
                //出现异常,重定向到订单查询页面
                Console.Write(e);
                return RedirectToAction("Index");
            }

            




        }
        #endregion

        #region 票证操作

        #region 打印换开
        /// <summary>
        /// 
        /// </summary>
        /// <param name="intETicketID"></param>
        /// <param name="strTicketNo">新的票号</param>
        /// <returns></returns>
        public ActionResult PrintChange(int intETicketID, string strTicketNo)
        {
            /*操作：（1）、B_电子客票：发票状态==打印换开，更改票号=最大票号*/
            ReturnJson rtMsg = new ReturnJson();

            try
            {
                B_ETicket dbETicket = (from tbETicket in myModel.B_ETicket
                                       where tbETicket.ETicketID == intETicketID
                                       select tbETicket).Single();
                dbETicket.invoiceStatusID = 6;//发票状态 ID( 1：未开发票  2：已开发票  3: 发票已回收  4: 新的电子客票信息  5: 变更结果  6: 打印换开)
                dbETicket.ticketNo = strTicketNo;
                myModel.Entry(dbETicket).State = System.Data.Entity.EntityState.Modified;
                if (myModel.SaveChanges() > 0)
                {
                    rtMsg.State = true;
                    rtMsg.Text = "打印换开成功!";
                }
                else
                {
                    rtMsg.Text = "打印换开失败!";
                }
            }
            catch (Exception e)
            {
                Console.Write(e);
                rtMsg.Text = "数据异常!";
            }

            return Json(rtMsg, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region ET航班更改

        public ActionResult SelectFlightPage()
        {
            //查询所有的城市机场信息
            List<S_Airport> airports = (from tbAirport in myModel.S_Airport
                                        select tbAirport).ToList();
            //传递到页面
            ViewBag.airports = airports;

            return View();
        }

        public ActionResult SelectFlights(int segmentNum, int[] airports, string[] dates)
        {
            //存放各个航段的航班
            List<SegmentVo> listSegment = new List<SegmentVo>();
            //数据验证 航段数=机场数-1=起飞日期个数
            if (segmentNum == airports.Length - 1 && segmentNum == dates.Length)
            {
                //遍历各个航段查询符合要求的数据
                for (int segmentIndex = 0; segmentIndex < segmentNum; segmentIndex++)
                {
                    //存放本航段的数据
                    SegmentVo segment = new SegmentVo();

                    //查询该航段的机场城市信息
                    //该航段的机场id
                    int orangeID = airports[segmentIndex];
                    int destinationID = airports[segmentIndex + 1];
                    //根据机场ID查询机场信息
                    S_Airport orangeAirport = myModel.S_Airport.Single(o => o.airportID == orangeID);
                    S_Airport destinationAirport = myModel.S_Airport.Single(o => o.airportID == destinationID);
                    //设置机场城市信息
                    segment.orangeCityName = orangeAirport.cityName;//起飞城市名称
                    segment.orangeAirportName = orangeAirport.airportName;//起飞机场名称
                    segment.destinationCityName = destinationAirport.cityName;//到达城市名称
                    segment.destinationAirportName = destinationAirport.airportName;//到达机场名称

                    //设置航班起飞日期
                    segment.strDate = dates[segmentIndex];
                    DateTime dtFlightDate = Convert.ToDateTime(dates[segmentIndex]);

                    //查询满足指定起飞机场ID,到达机场ID,起飞日期的航班
                    var queryLinq = from tabFlight in myModel.S_Flight
                                    join tabOrangeAirport in myModel.S_Airport on tabFlight.orangeID equals tabOrangeAirport.airportID
                                    join tabDestinationAirport in myModel.S_Airport on tabFlight.destinationID equals tabDestinationAirport.airportID
                                    join tabPlanType in myModel.S_PlanType on tabFlight.planTypeID equals tabPlanType.planTypeID
                                    orderby tabFlight.departureTime //根据起飞时间排序
                                    where tabFlight.orangeID == orangeID
                                    && tabFlight.destinationID == destinationID
                                    && tabFlight.flightDate == dtFlightDate
                                    select new FlightVo
                                    {
                                        flightID = tabFlight.flightID, //航班ID
                                        flightCode = tabFlight.flightCode, //航班号
                                        orangeID = tabFlight.orangeID, //起飞机场ID
                                        destinationID = tabFlight.destinationID, //目的地机场ID
                                        flightDate = tabFlight.flightDate, //起飞日期
                                        departureTime = tabFlight.departureTime, //起飞时间
                                        arrivalTime = tabFlight.arrivalTime, //降落时间
                                        planTypeID = tabFlight.planTypeID, //飞机机型ID
                                        standardPrice = tabFlight.standardPrice, //标准价格
                                        //扩展字段
                                        orangeCityName = tabOrangeAirport.cityName, //起飞城市名称
                                        orangeAirportName = tabOrangeAirport.airportName, //起飞机场名称
                                        destinationCityName = tabDestinationAirport.cityName, //降落城市名称
                                        destinationAirportName = tabDestinationAirport.airportName, //降落机场名称
                                        planTypeName = tabPlanType.planTypeName, //机型名称
                                        //子查询--查询出航班对应的所有舱位信息（舱位还有可售座位：售出座位<舱位座位）
                                        flightCabins = (from tabFlightCabin in myModel.S_FlightCabin
                                                        join tabCabinType in myModel.S_CabinType on tabFlightCabin.cabinTypeID equals tabCabinType.cabinTypeID
                                                        where tabFlightCabin.flightID == tabFlight.flightID
                                                              && tabFlightCabin.sellSeatNum < tabFlightCabin.seatNum
                                                        select new FlightCabinVo
                                                        {
                                                            flightCabinID = tabFlightCabin.flightCabinID, //航班舱位ID
                                                            flightID = tabFlightCabin.flightID, //航班ID
                                                            cabinTypeID = tabFlightCabin.cabinTypeID, //舱位类型ID
                                                            seatNum = tabFlightCabin.seatNum, //座位数
                                                            cabinPrice = tabFlightCabin.cabinPrice, //舱位价格
                                                            sellSeatNum = tabFlightCabin.sellSeatNum, //卖出座位数
                                                                                                      //扩展字段
                                                            cabinTypeCode = tabCabinType.cabinTypeCode, //舱位类型编号
                                                            cabinTypeName = tabCabinType.cabinTypeName //舱位类型名称
                                                        }).ToList()
                                    };
                    //检查航班是否为今天,如果是,判断该航班是否已经过了起飞时间
                    DateTime dtToday = DateTime.Now.Date;//获取日期 只到年月日
                    TimeSpan timeSpanNow = DateTime.Now.TimeOfDay;//当前的时间
                    if (dtFlightDate == dtToday)
                    {
                        queryLinq = queryLinq.Where(o => o.departureTime > timeSpanNow);
                    }
                    //查询出数据
                    List<FlightVo> listFlightVos = queryLinq.ToList();
                    //查询出的航班数据保存到segment中
                    segment.flightList = listFlightVos;
                    //将segment添加到listSegment集合中
                    listSegment.Add(segment);
                }

            }
            //以JSON格式返回 listSegment数据
            return Json(listSegment, JsonRequestBehavior.AllowGet);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="intETicketID"></param>
        /// <param name="flightCabinIDs">存放各个航段选择的航班</param>
        /// <param name="strChangeReason">变更原因</param>
        /// <param name="intPNRSegmentID">航段ID</param>
        /// <returns></returns>
        public ActionResult ChangeFlight(int intETicketID, int[] flightCabinIDs, string strChangeReason, int intPNRSegmentID)
        {
            #region 操作流程


            /*
                需求：先查询 PNR，修改 PNR， 
                然后记下需要变更的航段序号，输入( 数字), 输入相应的PNR（缺省为当前票的PNR） 
                确认后，发票状态显示：变更结果。 
            */

            /*操作：
            （1）、作废旧的B_PNR航段表的数据
            （2）、旧的：B_PNR航段表对应的修改S_航班舱位表  -- 售出座位加上-1
            （3）、修改：B_电子客票：发票状态== 5: 变更结果；航班ID=最新
            （4）、新增：B_PNR航段表。
            （5）、新的：B_PNR航段表对应的修改S_航班舱位表   -- 售出座位加上+1
            （6）、修改：B_PNR出票组的PNR航段ID，
            （7）、新增：B_航班变更表数据。
            */
            #endregion

            //根据航班舱位ID获取航班ID
            List<FlightCabinInfo> flightCabinInfos = new List<FlightCabinInfo>();
            for (int i = 0; i < flightCabinIDs.Length; i++)
            {
                //获取航班舱位ID
                int flightCabinID = flightCabinIDs[i];
                //根据航班舱位ID查询航班舱位信息
                FlightCabinInfo flightCabinInfo = (from tabFlightCabin in myModel.S_FlightCabin
                                                   join tabCabinType in myModel.S_CabinType on tabFlightCabin.cabinTypeID equals tabCabinType.cabinTypeID //舱位等级
                                                   join tabFlight in myModel.S_Flight on tabFlightCabin.flightID equals tabFlight.flightID //航班表 
                                                   join tabOrangeAirport in myModel.S_Airport on tabFlight.orangeID equals tabOrangeAirport.airportID // 机场表-起飞机场
                                                   join tabDestinationAirport in myModel.S_Airport on tabFlight.destinationID equals tabDestinationAirport.airportID //机场表-降落机场 
                                                   where tabFlightCabin.flightCabinID == flightCabinID
                                                   select new FlightCabinInfo
                                                   {
                                                       flightID = tabFlight.flightID,//航班ID                                                       
                                                   }).SingleOrDefault();
                //将查询出来的flightCabinInfo添加到集合flightCabinInfos
                flightCabinInfos.Add(flightCabinInfo);
            }
            //航班ID
            int intflightID = flightCabinInfos[0].flightID;

            ReturnJson rtMsg = new ReturnJson();
            try
            {
                //开启事务
                using (TransactionScope scope = new TransactionScope())
                {
                    //①作废旧的B_PNR航段表的数据
                    B_PNRSegment dbPNRSegment = (from tbPNRSegment in myModel.B_PNRSegment
                                                 where tbPNRSegment.PNRSegmentID == intPNRSegmentID
                                                 select tbPNRSegment).Single();
                    int? oldPNRID = dbPNRSegment.PNRID;
                    int? oldflightCabinID = dbPNRSegment.flightCabinID;
                    dbPNRSegment.invalid = false;//有效否--无效
                    myModel.Entry(dbPNRSegment).State = System.Data.Entity.EntityState.Modified;
                    myModel.SaveChanges();
                    //②修改S_航班舱位表  -- 售出座位加上-1
                    //判断未售出座位数是否足够,即 表中已经售出座位数+本次订座数量<=航班舱位座位数
                    S_FlightCabin sFlightCabin = (from tbFlightCabin in myModel.S_FlightCabin
                                                  where tbFlightCabin.flightCabinID == oldflightCabinID
                                                  select tbFlightCabin).Single();
                    //修改航班舱位售出座位数-1
                    sFlightCabin.sellSeatNum -= 1;
                    myModel.Entry(sFlightCabin).State = System.Data.Entity.EntityState.Modified;
                    myModel.SaveChanges();

                    //(3）、修改：B_电子客票：发票状态== 5: 变更结果
                    B_ETicket dbETicket = (from tbETicket in myModel.B_ETicket
                                           where tbETicket.ETicketID == intETicketID
                                           select tbETicket).Single();
                    int? oldFlightID = dbETicket.flightID;
                    dbETicket.invoiceStatusID = 5;//发票状态ID(1：未开发票  2：已开发票  3: 发票已回收  4: 新的电子客票信息  5: 变更结果  6: 打印换开)
                    dbETicket.flightID = intflightID;//新航班ID
                    dbETicket.tickingTime = DateTime.Now;//出票时间
                    myModel.Entry(dbETicket).State = System.Data.Entity.EntityState.Modified;
                    myModel.SaveChanges();

                    //新增B_PNR航段表
                    //新增B_航班变更表数据
                    B_FlightChange dbFlightChange = new B_FlightChange();
                    dbFlightChange.ETicketID = dbETicket.ETicketID;//电子客票ID
                    dbFlightChange.oldPNRSegmentID = oldFlightID;//原航段ID
                    dbFlightChange.oldPNRSegmentID = intflightID;//新航段ID
                    dbFlightChange.operatorID = Convert.ToInt32(Session["userID"]);//操作人ID
                    dbFlightChange.operatingTtime = DateTime.Now;//操作时间
                    //如果“换开原因”不为“空”则为“不自愿” 便赋值给“dbFlightChange”的换开原因
                    if (strChangeReason != "")
                    {
                        dbFlightChange.isVoluntary = false;//是否自愿
                        dbFlightChange.changeReason = strChangeReason;//换开原因
                    }
                    else
                    {
                        dbFlightChange.isVoluntary = true;//自愿
                    }
                    //执行新增操作
                    myModel.B_FlightChange.Add(dbFlightChange);
                    if (myModel.SaveChanges() > 0)
                    {
                        rtMsg.State = true;
                        rtMsg.Text = "ET航班更改成功!";
                    }
                    else
                    {
                        rtMsg.Text = "ET航班更改失败!";
                    }
                    scope.Complete();
                    rtMsg.Object = intETicketID;
                }
            }
            catch (Exception e)
            {
                Console.Write(e);
                rtMsg.Text = "数据异常!";
            }


            return Json(rtMsg, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region T4联打印

        public ActionResult T4Print(int ETicketID)
        {
            ReturnJson rtMsg = new ReturnJson();

            try
            {
                B_ETicket dbETicket = (from tbETicket in myModel.B_ETicket
                                       where tbETicket.ETicketID == ETicketID
                                       select tbETicket).Single();
                dbETicket.invoiceStatusID = 2;//发票状态ID(1：未开发票  2：已开发票  3: 发票已回收  4: 新的电子客票信息  5: 变更结果  6: 打印换开)
                myModel.Entry(dbETicket).State = System.Data.Entity.EntityState.Modified;
                if (myModel.SaveChanges() > 0)
                {
                    rtMsg.State = true;
                }
            }
            catch (Exception e)
            {
                Console.Write(e);
                rtMsg.Text = "数据异常";
            }

            return Json(rtMsg, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region 作废

        public ActionResult Invalid(int intUserID, int intETicketID, decimal decAgencyFee, decimal decActualRefunfMoney, string strInvalidCode)
        {
            /*
             操作：
             （1）修改：原电子客票: 票联状态 = 已作废 
             （2）新增：B_电子客票作废
            */
            ReturnJson rtMsg = new ReturnJson();

            try
            {
                B_ETicket dbETicket = (from tbETicket in myModel.B_ETicket
                                       where tbETicket.ETicketID == intETicketID
                                       select tbETicket).Single();

                dbETicket.eTicketStatusID = 4;//票联状态ID(1：可供使用 2：打印换开  3：退票  4:已作废 )
                myModel.Entry(dbETicket).State = System.Data.Entity.EntityState.Modified;
                if (myModel.SaveChanges() > 0)
                {
                    //新增:电子客票表作废
                    B_ETicketInvalid dbETicketInvalid = new B_ETicketInvalid();
                    dbETicketInvalid.ETicketID = intETicketID;//电子客票ID
                    dbETicketInvalid.agencyFee = decAgencyFee;//代理费
                    dbETicketInvalid.actualRefunfMoney = decActualRefunfMoney;//实退金额
                    dbETicketInvalid.InvalidCode = strInvalidCode;//作废单号
                    dbETicketInvalid.operatorID = intUserID;//操作人ID
                    dbETicketInvalid.invalidTime = DateTime.Now;//作废时间
                    myModel.B_ETicketInvalid.Add(dbETicketInvalid);
                    if (myModel.SaveChanges() > 0)
                    {
                        rtMsg.State = true;
                        rtMsg.Text = "电子客票已作废 ！";
                    }
                    else
                    {
                        rtMsg.Text = "电子客票作废失败 ！";
                    }
                }
                else
                {
                    rtMsg.Text = "电子客票已作废失败！";
                }
            }
            catch (Exception e)
            {
                Console.Write(e);
            }

            return Json(rtMsg, JsonRequestBehavior.AllowGet);
        }


        #endregion

        #region 退票

        /// <summary>
        /// 退票
        /// </summary>
        /// <param name="intUserID">操作人ID</param>
        /// <param name="intETicketID">电子客票ID</param>
        /// <param name="decRefundMoney">退款金额</param>
        /// <param name="decRefundAgencyFee">代理费退回</param>
        /// <param name="decActualRefunfMoney">实退金额</param>
        /// <param name="strChangeReason">退票原因</param>
        /// <returns></returns>
        public ActionResult doRefund(int intUserID, int intETicketID, decimal decRefundMoney, decimal decRefundAgencyFee, decimal decActualRefunfMoney, string strChangeReason, int isVoluntary)
        {
            /*
                操作：
                （1）修改：原电子客票: 票联状态 = 退票/REFUND
                （2）新增：B_退票
                （3）修改：S_虚拟账户退款（注意：这里是航空公司退费给代理人金额）
            */

            ReturnJson rtMsg = new ReturnJson();
            try
            {
                //开启事务
                using (TransactionScope scope = new TransactionScope())
                {
                    //修改原电子客票的票联状态
                    B_ETicket dbETicket = (from tbETicket in myModel.B_ETicket
                                           where tbETicket.ETicketID == intETicketID
                                           select tbETicket).Single();
                    dbETicket.eTicketStatusID = 3;//票联状态ID(1：可供使用 2：打印换开  3：退票  4:已作废 )
                    myModel.Entry(dbETicket).State = System.Data.Entity.EntityState.Modified;
                    myModel.SaveChanges();
                    
                    //新增退票表数据 
                    B_TicketRefund dbTicketRefund = new B_TicketRefund()
                    {
                        ETicketID = intETicketID,//电子客票ID
                        refundMoney = decRefundMoney,//退款金额
                        refundAgencyFee = decRefundAgencyFee,//代理费退回
                        actualRefunfMoney = decActualRefunfMoney,//实退金额
                        operatorID = intUserID,//操作人ID
                        operatingTtime = DateTime.Now,//操作时间 
                        changeReason = strChangeReason,
                        isVoluntary = isVoluntary == 1 ? true : false,
                    };
                    myModel.B_TicketRefund.Add(dbTicketRefund);
                    myModel.SaveChanges();

                    //3.修改虚拟账户余额(+退款金额，代理费为代理商退款给用户时扣除的费用，当前系统使用者为代理商)
                    S_VirtualAccount dbVirtualAccount = (from tbVirtualAccount in myModel.S_VirtualAccount
                                                         where tbVirtualAccount.userID == intUserID
                                                         select tbVirtualAccount).Single();
                    dbVirtualAccount.accountBalance += decRefundMoney;
                    myModel.Entry(dbVirtualAccount).State = System.Data.Entity.EntityState.Modified;
                    myModel.SaveChanges();

                    //4.当前虚拟账户，新增交易记录
                    B_TransactionRecord dbTransactionRecord = new B_TransactionRecord()
                    {
                        virtualAccountID = dbVirtualAccount.virtualAccountID,//虚拟账户ID
                        transactionType = 1,//交易类型-收入
                        transactionMoney = decRefundMoney,//交易金额
                        transactionTime = DateTime.Now,//交易时间
                        userID = intUserID//用户ID
                    };
                    myModel.B_TransactionRecord.Add(dbTransactionRecord);
                    myModel.SaveChanges();

                    //提交事务
                    scope.Complete();
                    //返回数据
                    rtMsg.State = true;
                    rtMsg.Text = "退票成功！";

                };
            }
            catch (Exception e)
            {
                Console.Write(e);
                rtMsg.Text = "数据异常！";
            }

            return Json(rtMsg, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #endregion


    }
}