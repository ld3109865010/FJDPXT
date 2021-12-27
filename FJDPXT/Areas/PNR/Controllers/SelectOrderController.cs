using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FJDPXT.Model;
using FJDPXT.EntityClass;

namespace FJDPXT.Areas.PNR.Controllers
{
    public class SelectOrderController : Controller
    {
        // GET: PNR/SelectOrder
        FJDPXTEntities1 myModel = new FJDPXTEntities1();

        private int loginUserID = 0;

        ///// <summary>
        ///// 在执行Action方法前执行该方法
        ///// </summary>
        ///// <param name="filterContext"></param>
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
        public ActionResult SelectOrder()
        {
            return View();
        }

        public ActionResult doSelectOrder(LayuiTablePage layuiTablePage, string orderNum, int? userGroupID,
            string jobNumber, string startDate, string endDate, string PNRNo)
        {
            var  order = from tbOrder in myModel.B_Order
                         join tbPNR in myModel.B_PNR on tbOrder.PNRID equals tbPNR.PNRID
                         join tbUser in myModel.S_User on tbOrder.operatorID equals tbUser.userID
                         join tbUserGroup in myModel.S_UserGroup on tbUser.userGroupID equals tbUserGroup.userGroupID
                         orderby tbOrder.orderID descending
                         select new OrderVo
                         {
                             orderID = tbOrder.orderID,
                             orderNo = tbOrder.orderNo,
                             PNRID = tbPNR.PNRID,
                             PNRNo = tbPNR.PNRNo,
                             totalPrice = tbOrder.totalPrice,
                             agencyFee = tbOrder.agencyFee,
                             payMoney = tbOrder.payMoney,
                             userGroupNumber = tbUserGroup.userGroupNumber,
                             jobNumber = tbUser.jobNumber,
                             payTime = tbOrder.payTime,
                             orderStatus=tbOrder.orderStatus,
                             userGroupID = tbUserGroup.userGroupID,
                             operatorID = tbOrder.operatorID
                         };
            //多条件查询
            //订单编号
            if (!string.IsNullOrEmpty(orderNum))
            {
                //order = order.Where(n => n.orderNo == orderNum.Trim());
                order = order.Where(m => m.orderNo.Contains(orderNum));
            }
            //用户组ID
            if (userGroupID>0)
            {
                order = order.Where(n => n.userGroupID == userGroupID);
            }
            else//没有选择用户组ID的情况下查询当前用户的订单
            {
                order = order.Where(n => n.operatorID == loginUserID);
            }
            //用户工号
            if (!string.IsNullOrEmpty(jobNumber))
            {
                order = order.Where(n => n.jobNumber == jobNumber.Trim());
            }
            //起始时间
            if (!string.IsNullOrEmpty(startDate))
            {
                DateTime dtStartDate = Convert.ToDateTime(startDate.Trim());
                //条件筛选
                order = order.Where(d => d.payTime >= dtStartDate);
            }
            //结束时间
            if (!string.IsNullOrEmpty(endDate))
            {
                //try
                //{
                    DateTime dtEndDate = Convert.ToDateTime(endDate.Trim());
                    order = order.Where(d => d.payTime <= dtEndDate);
                //}
                //catch (Exception e)
                //{
                //    Console.Write(e);
                //}
            }
            //`
            if (!string.IsNullOrEmpty(PNRNo))
            {
                PNRNo = PNRNo.Trim();
                order = order.Where(q => q.PNRNo == PNRNo);
            }
            //计算数据的总行数
            int totalCount = order.Count();
            
            //分页查询
            List<OrderVo> listOrder=order
                                    .Skip(layuiTablePage.GetStartIndex())
                                    .Take(layuiTablePage.limit)
                                    .ToList();


            //构建LayuiTableData需要的数据
            LayuiTableData<OrderVo> layuiTableData = new LayuiTableData<OrderVo>()
            {
                count = totalCount,//总行数
                data = listOrder//数据
            };
            
            return Json(layuiTableData, JsonRequestBehavior.AllowGet);
        }

        public ActionResult selectUserGroup()
        {
            //声明存放所有用户组信息的列表
            List<SelectVo> rtSelect = new List<SelectVo>();

            //查询当前用户所在的用户组
            List<SelectVo> curUserGroup = (from tbUsergroup in myModel.S_UserGroup
                                           join tbUser in myModel.S_User
                                           on tbUsergroup.userGroupID equals tbUser.userGroupID
                                           where tbUser.userID == loginUserID
                                           select new SelectVo
                                           {
                                               id = tbUsergroup.userGroupID,
                                               text = tbUsergroup.userGroupNumber
                                           }).ToList();

            //将当前用户所在用户组放进用户组信息列表
            rtSelect.AddRange(curUserGroup);

            //查询当前用户的下级用户组信息
            foreach (SelectVo UserGroup in curUserGroup)
            {
                //递归查询所有下级用户组
                List<S_UserGroup> getChildUserGroup = GetChildGroup(UserGroup.id).ToList();

                //将返回的下级用户组的信息构建成指定返回格式
                List<SelectVo> childUserGroup = (from tbChildGroup in getChildUserGroup
                                                 select new SelectVo
                                                 {
                                                     id = tbChildGroup.userGroupID,
                                                     text = tbChildGroup.userGroupNumber
                                                 }).ToList();
                //将下级用户组信息放进用户组信息列表
                rtSelect.AddRange(childUserGroup);
            }


            return Json(rtSelect, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 递归查询下用户组
        /// </summary>
        /// <param name="superiorUserGroupID">上级用户组ID</param>
        /// <returns></returns>
        private IEnumerable<S_UserGroup> GetChildGroup(int superiorUserGroupID)
        {
            var childUserGroup = from tbUserGroup in myModel.S_UserGroup
                                 where tbUserGroup.superiorUserGroupID == superiorUserGroupID
                                 select tbUserGroup;

            return childUserGroup.ToList().Concat(childUserGroup.ToList().SelectMany(m => GetChildGroup(m.userGroupID)));
        }

        /// <summary>
        /// 显示订单信息页面
        /// </summary>
        /// <param name="orderID"></param>
        /// <returns></returns>
        public ActionResult OrderInfoShow(int? orderID,int? ETicket)
        {
            try
            {
                //查询订单信息
                OrderVo orderInfor = (from tbOrder in myModel.B_Order
                                      join tbPnr in myModel.B_PNR on tbOrder.PNRID equals tbPnr.PNRID
                                      join tbUser in myModel.S_User on tbOrder.operatorID equals tbUser.userID
                                      join tbUserGroup in myModel.S_UserGroup on tbUser.userGroupID equals tbUserGroup.userGroupID
                                      join tbVirtualAccount in myModel.S_VirtualAccount on tbUser.userID equals tbVirtualAccount.userID
                                      where tbOrder.orderID == orderID
                                      select new OrderVo
                                      {
                                          orderNo = tbOrder.orderNo,
                                          PNRNo = tbPnr.PNRNo,
                                          userGroupNumber = tbUserGroup.userGroupNumber,
                                          jobNumber = tbUser.jobNumber,
                                          account = tbVirtualAccount.account,//支付账号
                                          payMoney = tbOrder.payMoney,//支付金额
                                          payTime = tbOrder.payTime,//支付时间
                                          remark = tbOrder.remark//备注
                                      }).Single();

                //查询订单对应的票号信息
                List<PNRTicketingVo> listPNRTicketing = (from tbPNRTicketing in myModel.B_PNRTicketing
                                                         join tbPassenger in myModel.B_PNRPassenger on tbPNRTicketing.PNRPassengerID equals tbPassenger.PNRPassengerID
                                                         join tbSegment in myModel.B_PNRSegment on tbPNRTicketing.PNRSegmentID equals tbSegment.PNRSegmentID
                                                         join tbFlight in myModel.S_Flight on tbSegment.flightID equals tbFlight.flightID
                                                         join tbOrange in myModel.S_Airport on tbFlight.orangeID equals tbOrange.airportID
                                                         join tbDestination in myModel.S_Airport on tbFlight.destinationID equals tbDestination.airportID
                                                         join tbCabinType in myModel.S_CabinType on tbSegment.cabinTypeID equals tbCabinType.cabinTypeID
                                                         join tbETicket in myModel.B_ETicket on tbPNRTicketing.ETicketID equals tbETicket.ETicketID
                                                         where tbPNRTicketing.orderID == orderID ||tbPNRTicketing.ETicketID==ETicket
                                                         select new PNRTicketingVo
                                                         {
                                                             passengerNo = tbPassenger.passengerNo.Value,//旅客编号
                                                             passengerName = tbPassenger.passengerName,//旅客姓名
                                                             segmentNo = tbSegment.segmentNo.Value,//航段号
                                                             flightCode = tbFlight.flightCode,//航班编号
                                                             orangeCity = tbOrange.cityName,//起飞城市名称
                                                             destinationCity = tbDestination.cityName,//到达城市名称
                                                             departureTime = tbFlight.departureTime.Value,//起飞时间
                                                             arrivalTime = tbFlight.arrivalTime.Value,//到达事件
                                                             cabinTypeCode = tbCabinType.cabinTypeCode,//舱位等级
                                                             ticketNo = tbETicket.ticketNo//票号
                                                         }).ToList();

                //将数据返回页面
                ViewBag.orderInfor = orderInfor;
                ViewBag.listPNRTicketing= listPNRTicketing;
                return View();
            }
            catch (Exception e)
            {
                Console.Write(e);
                //出现异常，重定向回订单查询页面
                return RedirectToAction("selectUserGroup");
            }
        }
        /// <summary>
        /// 显示PNR信息页面
        /// </summary>
        /// <param name="pnrID"></param>
        /// <returns></returns>
        public ActionResult pnrInfoShow(int PNRID)
        {
            try
            {
                //1. PNR基本信息：编号，状态，联系人信息，出票组信息
                PNRVo pnrInfor = (from tbPNR in myModel.B_PNR
                                  where tbPNR.PNRID == PNRID
                                  select new PNRVo
                                  {
                                      PNRID = tbPNR.PNRID,
                                      PNRNo = tbPNR.PNRNo,
                                      contactName = tbPNR.contactName,
                                      contactPhone = tbPNR.contactPhone,
                                      TicketingInfo = tbPNR.TicketingInfo,
                                      PNRStatus = tbPNR.PNRStatus
                                  }).Single();

                //2.旅客信息  (常旅客左连接)
                List<PassengerVo> listPassengerInfor = (from tbPassenger in myModel.B_PNRPassenger
                                                        join tbPassngerType in myModel.S_PassengerType on tbPassenger.passengerTypeID equals tbPassngerType.passengerTypeID
                                                        join tbCertificateType in myModel.S_CertificatesType on tbPassenger.certificatesTypeID equals tbCertificateType.certificatesTypeID
                                                        join tbFrequentPassenger in myModel.S_FrequentPassenger on tbPassenger.frequentPassengerID equals tbFrequentPassenger.frequentPassengerID
                                                        into temp
                                                        from tbTemp in temp.DefaultIfEmpty()
                                                        where tbPassenger.PNRID == PNRID
                                                        select new PassengerVo
                                                        {
                                                            PNRPassengerID = tbPassenger.PNRPassengerID,
                                                            passengerName = tbPassenger.passengerName,
                                                            passengerTypeID = tbPassenger.passengerTypeID,
                                                            passengerType = tbPassngerType.passengerType,
                                                            certificatesTypeID = tbPassenger.certificatesTypeID,
                                                            certificatesType = tbCertificateType.certificatesType,
                                                            certificatesCode = tbPassenger.certificatesCode,
                                                            frequentPassengerNo = tbTemp != null ? tbTemp.frequentPassengerNo : string.Empty
                                                        }).ToList();

                //3-航航班舱位信息
                List<FlightCabinInfo> listflightCabinInfo = (from tbSegment in myModel.B_PNRSegment
                                                             join tbflight in myModel.S_Flight
                                                             on tbSegment.flightID equals tbflight.flightID
                                                             join tbOrangeAirport in myModel.S_Airport
                                                             on tbflight.orangeID equals tbOrangeAirport.airportID
                                                             join tbDestinationAirport in myModel.S_Airport
                                                             on tbflight.destinationID equals tbDestinationAirport.airportID
                                                             join tbflightCabin in myModel.S_FlightCabin
                                                             on tbSegment.flightCabinID equals tbflightCabin.flightCabinID
                                                             join tbCabinType in myModel.S_CabinType
                                                             on tbSegment.cabinTypeID equals tbCabinType.cabinTypeID
                                                             orderby tbflight.flightDate, tbflight.departureTime
                                                             where tbSegment.PNRID == PNRID
                                                             select new FlightCabinInfo
                                                             {
                                                                 PNRSegmentID = tbSegment.PNRSegmentID,
                                                                 segmentNo = tbSegment.segmentNo.Value,
                                                                 flightCode = tbflight.flightCode,
                                                                 orangeCity = tbOrangeAirport.cityName,
                                                                 destinationCity = tbDestinationAirport.cityName,
                                                                 departureTime = tbflight.departureTime,
                                                                 arrivalTime = tbflight.arrivalTime,
                                                                 flightDate = tbflight.flightDate,
                                                                 cabinTypeName = tbCabinType.cabinTypeName,
                                                                 cabinPrice = tbCabinType.basisPrice.Value,
                                                                 seatNum = tbSegment.bookSeatNum.Value,
                                                                 segmentType = tbSegment.segmentType,
                                                             }).ToList();

                //4-其他信息
                List<B_PNROtherInfo> listPNROtherInfo = myModel.B_PNROtherInfo.Where(o => o.PNRID == PNRID).ToList();

                //5-出票组信息
                List<PNRTicketingVo> listPNRTicketing = (from tbPNRTicketing in myModel.B_PNRTicketing
                                                         join tbticket in myModel.B_ETicket
                                                         on tbPNRTicketing.ETicketID equals tbticket.ETicketID
                                                         join tbPNRpassenger in myModel.B_PNRPassenger
                                                         on tbPNRTicketing.PNRPassengerID equals tbPNRpassenger.PNRPassengerID
                                                         join tbPNRsegment in myModel.B_PNRSegment
                                                         on tbPNRTicketing.PNRSegmentID equals tbPNRsegment.PNRSegmentID
                                                         where tbPNRTicketing.PNRID == PNRID
                                                         select new PNRTicketingVo
                                                         {
                                                             ticketNo = tbticket.ticketNo,
                                                             segmentNo = tbPNRsegment.segmentNo.Value,
                                                             passengerNo = tbPNRpassenger.passengerNo.Value,
                                                         }).ToList();


                //将数据传到视图层
                ViewBag.PNRInfor = pnrInfor;
                ViewBag.passengerInfor = listPassengerInfor;
                ViewBag.flightCabinInfor = listflightCabinInfo;
                ViewBag.otherInfor = listPNROtherInfo;
                ViewBag.listPNRTicketing = listPNRTicketing;

                return View();
            }
            catch (Exception e)
            {
                Console.Write(e);
                //出现异常，重定向回订单查询页面
                return RedirectToAction("selectUserGroup");
            }

        }
    }
}