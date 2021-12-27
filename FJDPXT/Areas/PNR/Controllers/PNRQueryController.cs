using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FJDPXT.Model;
using FJDPXT.EntityClass;
using System.Transactions;
using FJDPXT.Common;

namespace FJDPXT.Areas.PNR.Controllers
{
    public class PNRQueryController : Controller
    {
        // GET: PNR/PNRQuery
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

        #region PNR信息查询

        public ActionResult PNRQuery()
        {
            return View();
        }

        public ActionResult SelectPNR(LayuiTablePage layuiTablePage,string PNRNo,string passengerName,string flightCode,string flightDate)
        {
            var tempPNRInfor = from tbPassenger in myModel.B_PNRPassenger
                               join tbPNR in myModel.B_PNR
                               on tbPassenger.PNRID equals tbPNR.PNRID
                               join tbSegment in myModel.B_PNRSegment
                               on tbPNR.PNRID equals tbSegment.PNRID
                               join tbFlight in myModel.S_Flight
                               on tbSegment.flightID equals tbFlight.flightID
                               select new
                               {
                                   PNRID = tbPNR.PNRID,
                                   passengerName = tbPassenger.passengerName,
                                   flightCode = tbFlight.flightCode,
                                   PNRNo = tbPNR.PNRNo,
                                   PNRStatus = tbPNR.PNRStatus,
                                   flightDate = tbFlight.flightDate
                               };

            //条件查询
            //PNR编号
            if (!string.IsNullOrEmpty(PNRNo)) {
                //tempPNRInfor = tempPNRInfor.Where(o => o.PNRNo == PNRNo.Trim());//精确查询
                tempPNRInfor = tempPNRInfor.Where(o => o.PNRNo.Contains(PNRNo.Trim()));//模糊查询
            }
            //旅客名称
            if (!string.IsNullOrEmpty(passengerName))
            {
                tempPNRInfor = tempPNRInfor.Where(o => o.passengerName == passengerName.Trim());
                //tempPNRInfor = tempPNRInfor.Where(o => o.passengerName.Contains(passengerName.Trim()));//模糊查询
            }
            //航班编号
            if (!string.IsNullOrEmpty(flightCode))
            {
                tempPNRInfor = tempPNRInfor.Where(o => o.flightCode == flightCode.Trim());
                //tempPNRInfor = tempPNRInfor.Where(o => o.flightCode.Contains(flightCode.Trim()));//模糊查询
            }
            //航班日期
            if (!string.IsNullOrEmpty(flightDate))
            {
                DateTime dtflightDate = Convert.ToDateTime(flightDate.Trim());
                tempPNRInfor = tempPNRInfor.Where(o => o.flightDate == dtflightDate);
            }

            //根据PNRID分组查询
            var listPNRInfor = from taPNRInfor in tempPNRInfor
                               orderby taPNRInfor.PNRID descending
                               group taPNRInfor by taPNRInfor.PNRID into tbPNR
                               select new PNRVo
                               {
                                   PNRID = tbPNR.Key,
                                   flightCode = tbPNR.FirstOrDefault().flightCode,
                                   PNRNo = tbPNR.FirstOrDefault().PNRNo,
                                   PNRStatus = tbPNR.FirstOrDefault().PNRStatus,
                                   flightDate = tbPNR.FirstOrDefault().flightDate,
                                   Passengers = (from tbPassenger in myModel.B_PNRPassenger
                                                 where tbPassenger.PNRID == tbPNR.Key
                                                 select tbPassenger).ToList()
                               };

            //计算数据总条数
            int totalRow = listPNRInfor.Count();

            //分页
            List<PNRVo> listPNR = listPNRInfor
                        .OrderByDescending(p => p.PNRID)
                        .Skip(layuiTablePage.GetStartIndex())
                        .Take(layuiTablePage.limit)
                        .ToList();

            //构建返回数据
            LayuiTableData<PNRVo> layuiTableData = new LayuiTableData<PNRVo>
            {
                count = totalRow,
                data = listPNR,
            };



            return Json(layuiTableData, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region PNR显示

        public ActionResult PnrInfoShow(int PNRID)
        {
            //将数据传递到页面
            ViewBag.PNRID = PNRID;

            return View();
        }

        public ActionResult SelectPNRInfo(int PNRID)
        {
            try
            {
                //1. PNR基本信息：编号，状态，联系人信息，出票组信息
                PNRVo pnrInfor = (from tbPNR in myModel.B_PNR
                                  where tbPNR.PNRID == PNRID
                                  select new PNRVo {
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
                                                            PNRPassengerID=tbPassenger.PNRPassengerID,
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
                                                                 departureTime=tbflight.departureTime,
                                                                 arrivalTime=tbflight.arrivalTime,
                                                                 flightDate=tbflight.flightDate,
                                                                 cabinTypeName=tbCabinType.cabinTypeName,
                                                                 cabinPrice=tbCabinType.basisPrice.Value,
                                                                 seatNum=tbSegment.bookSeatNum.Value,
                                                                 segmentType=tbSegment.segmentType,
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
                                                         select new PNRTicketingVo {
                                                             ticketNo = tbticket.ticketNo,
                                                             segmentNo = tbPNRsegment.segmentNo.Value,
                                                             passengerNo = tbPNRpassenger.passengerNo.Value,
                                                         }).ToList();


                return Json(new {
                    pnrInfor= pnrInfor,
                    listPNROtherInfo= listPNROtherInfo,
                    listflightCabinInfo= listflightCabinInfo,
                    listPassengerInfor= listPassengerInfor,
                    listPNRTicketing= listPNRTicketing
                } , JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return Json("数据异常", JsonRequestBehavior.AllowGet);
        }


        #endregion

        #region PNR复制

        public ActionResult CopyPNR(int PNRID) {
            ViewBag.PNRID = PNRID;
            return View();
        }
        
        #endregion

        #region PNR取消

        public ActionResult cancelPNR(int PNRID) {
            ReturnJson rtMsg = new ReturnJson();

            using (TransactionScope scope = new TransactionScope()) {
                try
                {
                    //查询PNR表
                    B_PNR pnrInfor = myModel.B_PNR.Single(p => p.PNRID == PNRID);
                    //判断,只有状态为1时才可以执行PNR取消的操作
                    if (pnrInfor.PNRStatus == 1) {
                        //1.修改PNR状态
                        pnrInfor.PNRStatus = 0;
                        //保存修改
                        myModel.Entry(pnrInfor).State = System.Data.Entity.EntityState.Modified;
                        myModel.SaveChanges();

                        //2.查询航段表
                        List<B_PNRSegment> listSegment = (from tbSegemt in myModel.B_PNRSegment
                                                          where tbSegemt.PNRID == PNRID
                                                          select tbSegemt).ToList();
                        foreach (B_PNRSegment segment in listSegment) {
                            //3.修改订座情况
                            segment.bookSeatInfo = 0; //订座情况(bookSeatInfo): 1：已预订座位；2：已经出票 0：取消订座
                            //保存
                            myModel.Entry(segment).State = System.Data.Entity.EntityState.Modified;
                            myModel.SaveChanges();

                            //4. 修改对应的航班舱位信息
                            S_FlightCabin flightCabin = (from tbFlightCabin in myModel.S_FlightCabin
                                                         where tbFlightCabin.flightCabinID == segment.flightCabinID
                                                         select tbFlightCabin).Single();
                            flightCabin.sellSeatNum -= segment.bookSeatNum;//订座数量-bookSeatNum数
                            myModel.Entry(flightCabin).State = System.Data.Entity.EntityState.Modified;
                            myModel.SaveChanges();

                        }
                        //提交事务
                        scope.Complete();

                        //返回成功信息
                        rtMsg.State = true;
                        rtMsg.Text = "PNR取消成功";


                    }
                    else
                    {
                        if (pnrInfor.PNRStatus == 0)
                        {
                            rtMsg.Text = "该PNR已经取消，无法再次取消!";
                        }

                        if (pnrInfor.PNRStatus == 2)
                        {
                            rtMsg.Text = "该PNR已经部分出票，无法取消！";
                        }

                        if (pnrInfor.PNRStatus == 3)
                        {
                            rtMsg.Text = "该PNR已经全部出票，无法取消！";
                        }
                    }

                    
                }
                catch (Exception e)
                {
                    Console.Write(e);
                }





            }
                return Json(rtMsg, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region PNR分离

        public ActionResult SplitPNR(int PNRID)
        {
            ViewBag.PNRID = PNRID;

            return View();
        }

        public ActionResult DoSplitPNR(int PNRID, List<int> pnrPassengerIDs)
        {
            ReturnJson rtMsg = new ReturnJson();

            using (TransactionScope scope = new TransactionScope()) {
                try
                {
                    //查询出需要分离的PNR信息
                    B_PNR oldPnrInfor = myModel.B_PNR.Single(p => p.PNRID == PNRID);

                    //PNR旅客信息
                    List<B_PNRPassenger> oldPassengerInfor = (from tbPNRPassenger in myModel.B_PNRPassenger
                                                              where tbPNRPassenger.PNRID == PNRID
                                                              select tbPNRPassenger).ToList();

                    //PNR航段信息
                    List<B_PNRSegment> oldSegmentInfor = (from tbPNRSegment in myModel.B_PNRSegment
                                                          where tbPNRSegment.PNRID == PNRID
                                                          select tbPNRSegment).ToList();

                    if (oldPnrInfor.PNRStatus == 1)
                    {
                        //判断原来的旅客要大于分离的旅客数
                        if (oldPassengerInfor.Count > pnrPassengerIDs.Count)
                        {
                            //1-新增新的PNR数据
                            //生成新的PNR编号
                            string strPNRNo = PNRCodeHelper.CreatePNR();
                            //创建表B_PNR对象
                            B_PNR pnr = new B_PNR
                            {
                                PNRNo = strPNRNo,
                                contactName = oldPnrInfor.contactName,
                                contactPhone = oldPnrInfor.contactPhone,
                                TicketingInfo = oldPnrInfor.TicketingInfo,
                                PNRStatus = 1,
                                operatorID = loginUserID,
                                createTime = DateTime.Now
                            };
                            //新增PNR
                            myModel.B_PNR.Add(pnr);
                            //保存数据
                            myModel.SaveChanges();
                            //新增的PNRID
                            int newPNRID = pnr.PNRID;

                            //2. 新增PNR航段信息，修改原PNR航段中的订座数
                            List<B_PNRSegment> newPnrSegments = new List<B_PNRSegment>();
                            foreach (B_PNRSegment oldPnrSegment in oldSegmentInfor)
                            {
                                //修改原PNR航段信息中的订座数
                                oldPnrSegment.bookSeatInfo -= pnrPassengerIDs.Count;
                                //保存修改
                                myModel.Entry(oldPnrSegment).State = System.Data.Entity.EntityState.Modified;
                                myModel.SaveChanges();

                                //创建新表B_PNRSegment对象
                                B_PNRSegment newPnrSegment = new B_PNRSegment
                                {
                                    PNRID = newPNRID,
                                    segmentNo = oldPnrSegment.segmentNo,
                                    flightID = oldPnrSegment.flightID,
                                    cabinTypeID = oldPnrSegment.cabinTypeID,
                                    flightCabinID = oldPnrSegment.flightCabinID,
                                    bookSeatNum = pnrPassengerIDs.Count,
                                    bookSeatInfo = oldPnrSegment.bookSeatInfo,
                                    segmentType = oldPnrSegment.segmentType,
                                    invalid = true
                                };
                                newPnrSegments.Add(newPnrSegment);
                            }
                            //保存
                            myModel.B_PNRSegment.AddRange(newPnrSegments);
                            myModel.SaveChanges();

                            //3-修改分离出来的PNRPassenger信息
                            //将分离出来的PNRPassenger的PNRID改为newPNRID
                            foreach (B_PNRPassenger oldPnrPassenger in oldPassengerInfor)
                            {
                                //判断PNRPassenger的ID是否包含在需要分离的PNRPassengerID列表中  pnrPassengerID这是上个页面定义存放分离ID的数组
                                if (pnrPassengerIDs.Contains(oldPnrPassenger.PNRPassengerID))
                                {
                                    //将PNRID改成newPNRID
                                    oldPnrPassenger.PNRID = newPNRID;

                                    //保存修改
                                    myModel.Entry(oldPnrPassenger).State = System.Data.Entity.EntityState.Modified;
                                    myModel.SaveChanges();
                                }
                                //else
                                //{
                                //    rtMsg.Text = "错误";
                                //    return Json(rtMsg,JsonRequestBehavior.AllowGet);
                                //}
                                
                            }

                            //PNR分离完成,提交事务
                            scope.Complete();

                            //返回成功的信息
                            rtMsg.State = true;
                            rtMsg.Text = "PNR分离成功!";
                        }
                        else {
                            rtMsg.Text = "不能分离PNR中所有的旅客";
                        }
                    }
                    else
                    {
                        if (oldPnrInfor.PNRStatus == 0)
                        {
                            rtMsg.Text = "该PNR已经取消，无法分离";
                        }

                        if (oldPnrInfor.PNRStatus == 2)
                        {
                            rtMsg.Text = "该PNR已经部分出票，无法分离";
                        }

                        if (oldPnrInfor.PNRStatus == 3)
                        {
                            rtMsg.Text = "该PNR已经全部出票，无法分离";
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.Write(e);
                    rtMsg.Text = "PNR分离异常！";
                }

            }


                return Json(rtMsg, JsonRequestBehavior.AllowGet);



        }

        #endregion

        #region 添加旅客

        public ActionResult addPassengerPage(int PNRID)
        {
            //计算出目前最多可以添加多少旅客
            int maxAddNum = 0;

            //查询目前旅客人数
            int passengerNum = (from tbPNRPassenger in myModel.B_PNRPassenger
                                where tbPNRPassenger.PNRID == PNRID
                                select tbPNRPassenger).Count();

            //理论上，目前最多可添加旅客人数为（9-目前旅客人数）
            maxAddNum = 9 - passengerNum;

            //查询航班舱位信息
            List<S_FlightCabin> listflightCabins = (from tbPnrSegment in myModel.B_PNRSegment
                                                   join tbFlightCabin in myModel.S_FlightCabin
                                                   on tbPnrSegment.flightCabinID equals tbFlightCabin.flightCabinID
                                                   where tbPnrSegment.PNRID == PNRID
                                                   select tbFlightCabin).ToList();

            //比较得到目前最多(有效)可添加旅客数
            foreach (S_FlightCabin listflightCabin in listflightCabins)
            {
                if (maxAddNum > (listflightCabin.seatNum - listflightCabin.sellSeatNum)) {
                    maxAddNum = listflightCabin.seatNum.Value - listflightCabin.sellSeatNum.Value;
                }
            }

            //下拉框数据查询-旅客类型
            List<S_PassengerType> passengerType = (from tbPassengerType in myModel.S_PassengerType
                                                   select tbPassengerType).ToList();
            //下拉框数据查询-证件类型
            List<S_CertificatesType> certificatesType = (from tbCertificatesType in myModel.S_CertificatesType
                                                         select tbCertificatesType).ToList();
            //将数据传到页面
            ViewBag.PNRID = PNRID;//PNRID
            ViewBag.maxAddNum = maxAddNum;//最多可添加旅客数        
            ViewBag.passengerType = passengerType;//旅客类型
            ViewBag.certificatesType = certificatesType;//证件类型



            return View();
        }

        public ActionResult addPNRPassenger(int PNRID,List<PassengerVo> passengerInfors)
        {
            ReturnJson rtMsg = new ReturnJson();

            try
            {
                //查询PNR信息
                B_PNR pnrInfor = myModel.B_PNR.Single(p => p.PNRID == PNRID);

                //判断PNR状态
                if (pnrInfor.PNRStatus == 1)
                {
                    //查询目前旅客列表
                    List<B_PNRPassenger> dbPassengers = (from tbPassenger in myModel.B_PNRPassenger
                                                         where tbPassenger.PNRID == PNRID
                                                         select tbPassenger).ToList();

                    //遍历循坏页面数据
                    //数据验证
                    //===1、验证输入的旅客信息（姓名，身份证号、旅客类型，常旅客号）
                    foreach (PassengerVo passenger in passengerInfors)
                    {
                        ////判断旅客姓名不能为空；如果为空，返回系统提示信息
                        if (string.IsNullOrEmpty(passenger.passengerName)) {
                            rtMsg.Text = string.Format("旅客 [{0}] 的姓名不能为空！", passenger.passengerNo);
                            return Json(rtMsg, JsonRequestBehavior.AllowGet);
                        }

                        //==如果旅客选择的是身份证，判断输入的旅客身份证号是否正确；判断身份证年龄和选择的乘客类型（成人、儿童）是否匹配
                        if (passenger.certificatesTypeID == 1) {
                            //检查身份证
                            if (!IdCardHelper.CheckIdCard(passenger.certificatesCode)) {
                                rtMsg.Text = string.Format("旅客 【{0}】 的身份证号输入不正确，请重新输入！", passenger.passengerNo);
                                return Json(rtMsg, JsonRequestBehavior.AllowGet);
                            }

                            // =检查乘客类型选择（成人、儿童）
                            int age = IdCardHelper.GetAgeByIdCard(passenger.certificatesCode);
                            if (passenger.passengerTypeID == 1 && age<18) {
                                rtMsg.Text = string.Format("旅客 【{0}】 的身份证年龄不满18周岁，与所选的“成人”不符合！", passenger.passengerNo);
                                return Json(rtMsg, JsonRequestBehavior.AllowGet);
                            }
                            if (passenger.passengerTypeID == 2 && age >= 18)
                            {
                                rtMsg.Text = string.Format("旅客 【{0}】 的身份证年龄已满18周岁，与所选的“儿童”不符合！", passenger.passengerNo);
                                return Json(rtMsg, JsonRequestBehavior.AllowGet);
                            }

                            //检查在本PNR数据库中旅客列表中是否已经存在该身份证号
                            foreach (B_PNRPassenger dbPassenger in dbPassengers)
                            {
                                if (dbPassenger.certificatesTypeID == 1 && dbPassenger.certificatesCode.Trim() == passenger.certificatesCode.Trim()) {
                                    rtMsg.Text = string.Format("添加的旅客【{0}】的身份证号和已经存在的旅客【{1}】的身份证号相同！请检查", passenger.passengerNo, dbPassenger.passengerNo);
                                    return Json(rtMsg, JsonRequestBehavior.AllowGet);
                                }
                            }
                        }
                        //检查常旅客号
                        if (!string.IsNullOrEmpty(passenger.frequentPassengerNo))
                        {
                            //根据常旅客号去数据库查询常旅客信息
                            try
                            {
                                S_FrequentPassenger frequentPassenger = (from tabFrequentPassenger in myModel.S_FrequentPassenger
                                                                         where tabFrequentPassenger.frequentPassengerNo == passenger.frequentPassengerNo.Trim()
                                                                         select tabFrequentPassenger).Single();

                                //存在，检查姓名，身份证是否对应
                                if (passenger.passengerName.Trim() != frequentPassenger.frequentPassengerName.Trim() ||
                                   passenger.certificatesCode.Trim() != frequentPassenger.certificatesCode.Trim())
                                {
                                    rtMsg.Text = string.Format("旅客 【{0}】 姓名和填写的常旅客信息不匹配，请检查。", passenger.passengerNo);
                                    return Json(rtMsg, JsonRequestBehavior.AllowGet);
                                }

                                //将常旅客中的常旅客ID赋值给旅客
                                passenger.frequentPassengerID = frequentPassenger.frequentPassengerID;
                            }
                            catch (Exception e)
                            {
                                Console.Write(e);
                            }
                        }
                    }

                    //检查新增的旅客身份证号是否相同
                    for (int i = 0; i <passengerInfors.Count; i++)
                    {
                        for (int j = i+1; j < passengerInfors.Count; j++)
                        {
                            if (passengerInfors[i].certificatesTypeID == 1 &&
                               passengerInfors[j].certificatesTypeID == 1 &&
                               passengerInfors[i].certificatesCode == passengerInfors[j].certificatesCode)
                            {
                                rtMsg.Text = string.Format("第【{0}】个旅客和第【{1}】个旅客的身份证号相同，请检查!", passengerInfors[i].passengerNo, passengerInfors[j].passengerNo);
                                return Json(rtMsg, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }
                    //开启事务,保存数据
                    using (TransactionScope scope = new TransactionScope())
                    {
                        //1-修改S_FlightCabin航班舱位表 (修改卖出座位数)
                        //获取航班舱位信息
                        List<S_FlightCabin> flightCabins = (from tbSegment in myModel.B_PNRSegment
                                                            join tbFlightCabin in myModel.S_FlightCabin
                                                            on tbSegment.flightCabinID equals tbFlightCabin.flightCabinID
                                                            where tbSegment.PNRID == PNRID
                                                            select tbFlightCabin).ToList();

                        foreach (S_FlightCabin flightCabin in flightCabins)
                        {
                            //判断对应航班的舱位座位数是否大于等于新增的旅客人数
                            if (flightCabin.seatNum - flightCabin.sellSeatNum >= passengerInfors.Count)
                            {
                                //修改卖出座位数
                                flightCabin.sellSeatNum += passengerInfors.Count;
                                //保存修改
                                myModel.Entry(flightCabin).State = System.Data.Entity.EntityState.Modified;
                                myModel.SaveChanges();
                            }
                            else
                            {
                                rtMsg.Text = "航班余票数不足，订座失败！";
                                return Json(rtMsg, JsonRequestBehavior.AllowGet);
                            }
                        }

                        //2. 修改B_PNRSegment航段表中的订座数
                        //获取PNR航段信息
                        List<B_PNRSegment> PNRSegments = (from tbSegment in myModel.B_PNRSegment
                                                          where tbSegment.PNRID == PNRID
                                                          select tbSegment).ToList();

                        //遍历航段信息
                        foreach (B_PNRSegment PNRSegment in PNRSegments)
                        {
                            //订座数=原订座数+添加的旅客数
                            PNRSegment.bookSeatNum += passengerInfors.Count;

                            //保存修改
                            myModel.Entry(PNRSegment).State=System.Data.Entity.EntityState.Modified;
                            myModel.SaveChanges();
                        }

                        //3. 新增B_PNRPassenger表中旅客信息
                        List<B_PNRPassenger> PNRPassengers = new List<B_PNRPassenger>();
                        //获取接下来的旅客序号 (dbPassengers当前的旅客数)
                        int passengerNo =dbPassengers.Count+1;
                        //遍历页面输入的旅客信息，逐一新增  
                        foreach (PassengerVo passengerInfor in passengerInfors)
                        {
                            B_PNRPassenger newPassenger = new B_PNRPassenger()
                            {
                                PNRID = PNRID,
                                passengerNo = passengerNo,
                                passengerName = passengerInfor.passengerName,
                                passengerTypeID = passengerInfor.passengerTypeID,
                                certificatesTypeID = passengerInfor.certificatesTypeID,
                                certificatesCode = passengerInfor.certificatesCode,
                                frequentPassengerID = passengerInfor.frequentPassengerID
                            };
                            PNRPassengers.Add(newPassenger);
                            passengerNo++;
                        }

                        //保存新增
                        myModel.B_PNRPassenger.AddRange(PNRPassengers);
                        myModel.SaveChanges();

                        //4.查询出当前PNR所有旅客信息，重新编排旅客编号
                        List<B_PNRPassenger> Passengers = (from tbPassenger in myModel.B_PNRPassenger
                                                           where tbPassenger.PNRID == PNRID
                                                           select tbPassenger).ToList();
                        for (int i = 0; i <Passengers.Count; i++)
                        {
                            Passengers[i].passengerNo = (i + 1);

                            //保存修改
                            myModel.Entry(Passengers[i]).State = System.Data.Entity.EntityState.Modified;
                            //myModel.SaveChanges();
                        }
                        if (myModel.SaveChanges() > 0)
                        {
                            //提交事件
                            scope.Complete();

                            //返回数据
                            rtMsg.State = true;
                            rtMsg.Text = "添加旅客成功!";
                        }
                       
                    }
                }
                else {
                    if (pnrInfor.PNRStatus == 0)
                    {
                        rtMsg.Text = "该PNR已经取消，不能添加旅客";
                    }
                    if (pnrInfor.PNRStatus == 2)
                    {
                        rtMsg.Text = "该PNR已经部分出票，不能添加旅客";
                    }
                    if (pnrInfor.PNRStatus == 3)
                    {
                        rtMsg.Text = "该PNR已经全部出票，不能添加旅客";
                    }
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

        #region 修改姓名

        public ActionResult EditPassengerName(int PNRID)
        {
            ViewBag.PNRID = PNRID;

            return View();
        }

        public ActionResult UpdatePassengerName(int PNRID, List<B_PNRPassenger> passengers)
        {
            ReturnJson rtMsg = new ReturnJson();

            //查询出数据库当前PNRID的旅客信息
            List<B_PNRPassenger> dbPassengers = (from tbPassenger in myModel.B_PNRPassenger
                                                 where tbPassenger.PNRID == PNRID
                                                 select tbPassenger).ToList();

            //开启事务
            using (TransactionScope scope = new TransactionScope())
            {
                try
                {
                    //遍历对应的PNRID所有的旅客信息
                    foreach (B_PNRPassenger dbPassenger in dbPassengers)
                    {
                        //遍历页面传递过来的旅客信息
                        foreach (B_PNRPassenger passenger in passengers)
                        {
                            //如果旅客的PNRPassengerID相等且姓名不一样，说明旅客姓名被修改过
                            if (dbPassenger.PNRPassengerID == passenger.PNRPassengerID &&
                                dbPassenger.passengerName != passenger.passengerName)
                            {
                                //数据验证==如果页面传递的名称为空，结束循环，返回
                                if (string.IsNullOrEmpty(passenger.passengerName))
                                {
                                    rtMsg.Text= string.Format("第【{0}】位旅客的姓名为空，修改失败，请检查！", dbPassenger.passengerNo);
                                    return Json(rtMsg, JsonRequestBehavior.AllowGet);
                                }
                                //修改表B_PNRPassenger中旅客姓名
                                dbPassenger.passengerName = passenger.passengerName.Trim();

                                //保存到数据库
                                myModel.Entry(dbPassenger).State = System.Data.Entity.EntityState.Modified;
                                myModel.SaveChanges();
                            }
                        }
                    }
                   
                        //提交事务
                        scope.Complete();

                        //返回数据
                        rtMsg.State = true;
                        rtMsg.Text = "旅客姓名修改成功!";

                }
                catch (Exception e)
                {
                    Console.Write(e);
                    rtMsg.Text = "数据异常!";
                }
            }
                return Json(rtMsg, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region 删除旅客

        public ActionResult RemovePassenger(int PNRID)
        {
            ViewBag.PNRID = PNRID;

            return View();
        }

        public ActionResult DeletePassenger(int PNRID, List<int> passengerIds)
        {
            ReturnJson rtMsg = new ReturnJson();

            //查询当前PNRID下所有的旅客信息
            int dbPassengerNum = (from tbPassenger in myModel.B_PNRPassenger
                                  where tbPassenger.PNRID == PNRID
                                  select tbPassenger).Count();

            //检查是否选中全部旅客信息
            if (dbPassengerNum > passengerIds.Count)
            {
                //开启事务
                using (TransactionScope scope = new TransactionScope())
                {
                    //1.删除旅客信息
                    //根据旅客ID查询出旅客信息
                    List<B_PNRPassenger> delPassenger = (from tbPassenger in myModel.B_PNRPassenger
                                                         where passengerIds.Contains(tbPassenger.PNRPassengerID)
                                                         select tbPassenger).ToList();
                    //删除数据,保存到数据库
                    myModel.B_PNRPassenger.RemoveRange(delPassenger);
                    myModel.SaveChanges();

                    //2-查询出当前PNR所有的旅客信息,重新编排旅客编号
                    List<B_PNRPassenger> Passengers = (from tbPassenger in myModel.B_PNRPassenger
                                                       where tbPassenger.PNRID == PNRID
                                                       select tbPassenger).ToList();
                    for (int i = 0; i <Passengers.Count; i++)
                    {
                        Passengers[i].passengerNo = (i + 1);

                        //保存修改
                        myModel.Entry(Passengers[i]).State = System.Data.Entity.EntityState.Modified;
                        myModel.SaveChanges();
                    }

                    //3.释放对应航班的座位
                    //3.1 查询对应PNR的航班舱位信息
                    List<S_FlightCabin> upDateFlightCabin = (from tbSegement in myModel.B_PNRSegment
                                                             join tbFlightCabin in myModel.S_FlightCabin
                                                             on tbSegement.flightCabinID equals tbFlightCabin.flightCabinID
                                                             where tbSegement.PNRID == PNRID
                                                             select tbFlightCabin).ToList();
                    //遍历航班舱位信息,修改“卖出座位数”
                    foreach (S_FlightCabin flightCabin in upDateFlightCabin)
                    {
                        //卖出座位数 = 当前卖出座位数 - 删除旅客人数
                        flightCabin.sellSeatNum -= passengerIds.Count;

                        //保存修改
                        myModel.Entry(flightCabin).State = System.Data.Entity.EntityState.Modified;
                        myModel.SaveChanges();

                    }

                    //3.2 查询出对应PNR的航段信息 (订座数)
                    List<B_PNRSegment> updatePNRSegment = (from tbPNRSegment in myModel.B_PNRSegment
                                                           where tbPNRSegment.PNRID == PNRID
                                                           select tbPNRSegment).ToList();
                    foreach (B_PNRSegment PNRSegment in updatePNRSegment)
                    {
                        //订座数=当前订座数-删除旅客人数
                        PNRSegment.bookSeatNum -= passengerIds.Count;

                        //保存修改
                        myModel.Entry(PNRSegment).State = System.Data.Entity.EntityState.Modified;
                        myModel.SaveChanges();
                    }
                    //提交事务
                    scope.Complete();

                    //返回成功数据
                    rtMsg.State = true;
                    rtMsg.Text = "旅客信息删除成功";
                }
            }
            else
            {
                rtMsg.Text = "不能删除当前PNR中全部旅客信息！";
            }

            return Json(rtMsg, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region 添加航段

        public ActionResult AddSegment(int PNRID,List<int> flightCabinIDs)
        {
            ReturnJson rtMsg = new ReturnJson();

            try
            {
                //开启事务
                using (TransactionScope scope = new TransactionScope())
                {
                    //查询出当前的PNR旅客的人数,用于判断剩余座位数是否足够
                    int passengerNum = myModel.B_PNRPassenger.Count(p => p.PNRID == PNRID);

                    //new航段表对象列表，存放新添加的航段信息
                    List<B_PNRSegment> PNRSegments = new List<B_PNRSegment>();

                    //2.遍历循环航班舱位信息
                    for (int i = 0; i <flightCabinIDs.Count; i++)
                    {
                        //3.获取航班舱位ID，查询航班舱位信息
                        int flightCabinID = flightCabinIDs[i];
                        S_FlightCabin flightCabin = myModel.S_FlightCabin.Single(p => p.flightCabinID == flightCabinID);

                        //4.判断当前航班舱位座位数是否充足
                        if (flightCabin.seatNum - flightCabin.sellSeatNum > passengerNum)
                        {
                            //5.修改航班舱位表中的“卖出座位数”
                            flightCabin.sellSeatNum += passengerNum;

                            //保存修改
                            myModel.Entry(flightCabin).State = System.Data.Entity.EntityState.Modified;
                            myModel.SaveChanges();

                            //6. 新增航段表数据
                            PNRSegments.Add(new B_PNRSegment()
                            {
                                PNRID = PNRID,
                                segmentNo = (i + 1),
                                flightID = flightCabin.flightID,
                                cabinTypeID = flightCabin.cabinTypeID,
                                flightCabinID = flightCabin.flightCabinID,
                                bookSeatNum = passengerNum,//订座数
                                bookSeatInfo = 1,//订座情况订座情况(bookSeatInfo):1：已预订座位；2：已经出票 0：取消订座
                                invalid = true//是否有效
                            });
                        
                        }
                        else
                        {
                            rtMsg.Text = "航班余票数不足，添加航段失败！";
                            return Json(rtMsg, JsonRequestBehavior.AllowGet);
                        }
                    }
                    //保存航段表到数据库
                    myModel.B_PNRSegment.AddRange(PNRSegments);
                    myModel.SaveChanges();

                    //7.查询PNR航班信息 并按照飞机的起飞日期 和 起飞时间排序
                    List<FlightCabinInfo> segmentInfos = (from tabPnrSegment in myModel.B_PNRSegment
                                                          join tabFlightCabin in myModel.S_FlightCabin on tabPnrSegment.flightCabinID equals tabFlightCabin.flightCabinID
                                                          join tabFlight in myModel.S_Flight on tabFlightCabin.flightID equals tabFlight.flightID
                                                          join tabCabinType in myModel.S_CabinType on tabFlightCabin.cabinTypeID equals tabCabinType.cabinTypeID
                                                          join tabOrangeAirport in myModel.S_Airport on tabFlight.orangeID equals tabOrangeAirport.airportID
                                                          join tabDestinationAirport in myModel.S_Airport on tabFlight.destinationID equals tabDestinationAirport.airportID
                                                          orderby tabFlight.flightDate,tabFlight.departureTime//按照航班日期&起飞时间顺序排序
                                                          where tabPnrSegment.PNRID == PNRID
                                                          select new FlightCabinInfo
                                                          {
                                                              flightID = tabFlight.flightID, //航班ID
                                                              flightCode = tabFlight.flightCode, //航班号
                                                              orangeID = tabFlight.orangeID, //始发地ID
                                                              destinationID = tabFlight.destinationID, //目的地ID
                                                              departureTime = tabFlight.departureTime, //起飞时间
                                                              arrivalTime = tabFlight.arrivalTime, //降落时间
                                                              planTypeID = tabFlight.planTypeID, //飞机类型ID
                                                              flightDate = tabFlight.flightDate, //航班时间
                                                              standardPrice = tabFlight.standardPrice, //标准价格
                                                                                                       //扩展字段
                                                              PNRSegmentID = tabPnrSegment.PNRSegmentID, //PNR航段信息
                                                              
                                                          }).ToList();

                    //8-将以上排序以后的信息，重新编排航段号
                    for (int i = 0; i <segmentInfos.Count; i++)
                    {
                        //获取航段ID
                        int segmentID = segmentInfos[i].PNRSegmentID;
                        //查询对应航段信息
                        B_PNRSegment pnrSegment = myModel.B_PNRSegment.SingleOrDefault(p => p.PNRSegmentID == segmentID);
                        //修改&保存航段表
                        if (pnrSegment != null)
                        {
                            pnrSegment.segmentNo = i + 1;
                            //保存修改
                            myModel.Entry(pnrSegment).State = System.Data.Entity.EntityState.Modified;
                            myModel.SaveChanges();
                        }
                    }
                    //提交事务
                    scope.Complete();

                    //返回数据
                    rtMsg.State = true;
                    rtMsg.Text = "添加航段成功";

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

        #region 删除航段
          
        public ActionResult removeSegmentPage(int PNRID)
        {
            ViewBag.PNRID = PNRID;
            return View();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="PNRID"></param>
        /// <param name="segmentIds">存放选择删除的航段ID</param>
        /// <returns></returns>
        public ActionResult delSegment(int PNRID,List<int> segmentIds)
        {
            ReturnJson rtMsg = new ReturnJson();

            using (TransactionScope scope = new TransactionScope())
            {
                try
                {
                    //查询要删除的航段信息
                    List<B_PNRSegment> PNRSegment = (from tbSegment in myModel.B_PNRSegment
                                                     where segmentIds.Contains(tbSegment.PNRSegmentID)
                                                     select tbSegment).ToList();

                    //遍历循坏航段信息,查询出对应的订座数,到相应的航班舱位表中是防止座位
                    foreach (B_PNRSegment Segment in PNRSegment)
                    {
                        //查询出对应的航班舱位信息
                        S_FlightCabin flightCabin = myModel.S_FlightCabin.Single(o => o.flightCabinID == Segment.flightCabinID);

                        //修改卖出的座位数
                        flightCabin.sellSeatNum -= Segment.bookSeatNum;
                        //保存修改
                        myModel.Entry(flightCabin).State = System.Data.Entity.EntityState.Modified;
                        myModel.SaveChanges();
                    }

                    //删除航段
                    myModel.B_PNRSegment.RemoveRange(PNRSegment);
                    myModel.SaveChanges();

                    //查询PNR航班信息 并按照飞机的起飞日期和起飞时间进行排序
                    List<FlightCabinInfo> segmentInfors = (from tbPnrSegment in myModel.B_PNRSegment
                                                           join tabFlight in myModel.S_Flight
                                                           on tbPnrSegment.flightID equals tabFlight.flightID
                                                           orderby tabFlight.flightDate, tabFlight.departureTime
                                                           where tbPnrSegment.PNRID == PNRID
                                                           select new FlightCabinInfo
                                                           {
                                                               flightID = tabFlight.flightID, //航班ID
                                                               flightCode = tabFlight.flightCode, //航班号
                                                               orangeID = tabFlight.orangeID, //始发地ID
                                                               destinationID = tabFlight.destinationID, //目的地ID
                                                               departureTime = tabFlight.departureTime, //起飞时间
                                                               arrivalTime = tabFlight.arrivalTime, //降落时间
                                                               planTypeID = tabFlight.planTypeID, //飞机类型ID
                                                               flightDate = tabFlight.flightDate, //航班时间
                                                               standardPrice = tabFlight.standardPrice, //标准价格
                                                                                                        //扩展字段
                                                               PNRSegmentID = tbPnrSegment.PNRSegmentID, //PNR航段信息

                                                           }).ToList();

                    //将以上排序后的信息,重新编排航段号
                    for (int i = 0; i < segmentInfors.Count; i++)
                    {
                        //获取航段ID
                        int segmentID = segmentInfors[i].PNRSegmentID;
                        //查询对应的航段信息
                        B_PNRSegment pnrSegment = myModel.B_PNRSegment.SingleOrDefault(p => p.PNRSegmentID == segmentID);
                        //修改&保存航段表
                        if (pnrSegment != null)
                        {
                            pnrSegment.segmentNo = i + 1;
                            myModel.Entry(pnrSegment).State = System.Data.Entity.EntityState.Modified;
                            myModel.SaveChanges();
                        }

                    }

                    //提交事务
                    scope.Complete();
                    //返回成功的数据
                    rtMsg.State = true;
                    rtMsg.Text = "删除航段成功!";
                    
                }
                catch (Exception e)
                {
                    Console.Write(e);
                    rtMsg.Text = "数据异常！";
                }

            }





            return Json(rtMsg, JsonRequestBehavior.AllowGet);  
        }

        #endregion

        #region 部分出票

        public ActionResult PartIssuanceTicket(int PNRID)
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

                //5. 查询 S_VirtualAccount 虚拟账户的信息
                VirtualAccountVo dbVirtualAccount = (from tbVirtualAccount in myModel.S_VirtualAccount
                                                     join tbUser in myModel.S_User on tbVirtualAccount.userID equals tbUser.userID
                                                     where tbVirtualAccount.userID == loginUserID
                                                     select new VirtualAccountVo
                                                     {
                                                         account = tbVirtualAccount.account,
                                                         accountBalance = tbVirtualAccount.accountBalance,
                                                         jobNumber = tbUser.jobNumber
                                                     }).Single();
                //6-查询出该PNR下出票的旅客列表
                List<int> ticketingPassengerIDs = (from tbPassenger in myModel.B_PNRPassenger
                                                   join tbTicketing in myModel.B_PNRTicketing on tbPassenger.PNRPassengerID equals tbTicketing.PNRPassengerID
                                                   where tbPassenger.PNRID == PNRID
                                                   select tbPassenger.PNRPassengerID).ToList();

                //return Json(new
                //{
                //    pnrInfor = pnrInfor,
                //    listPNROtherInfo = listPNROtherInfo,
                //    listflightCabinInfo = listflightCabinInfo,
                //    listPassengerInfor = listPassengerInfor,
                //    dbVirtualAccount = dbVirtualAccount,
                //    ticketingPassengerIDs= ticketingPassengerIDs,
                //}, JsonRequestBehavior.AllowGet);
                //将数据传递到页面
                ViewBag.pnrInfor = pnrInfor;
                ViewBag.listPassengerInfor = listPassengerInfor;
                ViewBag.listflightCabinInfo = listflightCabinInfo;
                ViewBag.listPNROtherInfo = listPNROtherInfo;
                ViewBag.dbVirtualAccount = dbVirtualAccount;
                ViewBag.ticketingPassengerIDs = ticketingPassengerIDs;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

            }
            return View();
        }
       
        /// <summary>
        /// 部分出票
        /// </summary>
        /// <param name="order">订单数据</param>
        /// <param name="payType">支付类型</param>
        /// <param name="checkedPassengerIDs">需要出票的旅客ID</param>
        /// <returns></returns>
        public ActionResult DoPartIssueTickets(B_Order order,int payType,List<int> checkedPassengerIDs/*,int PNRID*/)
        {
            ReturnJson rtMsg = new ReturnJson();

            //验证=>订单中的PNRID不能为0，支付金额=总金额-代理费
            if (order.PNRID != 0 && order.payMoney == order.totalPrice - order.agencyFee)
            {
                //开启事务
                using (TransactionScope scope = new TransactionScope())
                {
                    try
                    {
                        //查询出当前PNR下已出票的旅客ID
                        List<int> ticPassengerIDs = (from tbPassenger in myModel.B_PNRPassenger
                                                     join tbTicket in myModel.B_PNRTicketing
                                                     on tbPassenger.PNRPassengerID equals tbTicket.PNRPassengerID
                                                     where tbPassenger.PNRID == order.PNRID
                                                     select tbPassenger.PNRPassengerID).ToList();

                        //查询出未出票的旅客ID 该PNR下旅客，不在已经出票列表的旅客 
                        List<int> noTicPassengerIDs = (from tbPassenger in myModel.B_PNRPassenger
                                                       where tbPassenger.PNRID == order.PNRID && !ticPassengerIDs.Contains(tbPassenger.PNRPassengerID)
                                                       select tbPassenger.PNRPassengerID).ToList();

                        //数据验证
                        //1. 验证勾选的旅客ID是否包含在未出票的旅客ID列表中
                        foreach (int checkedPassengerID in checkedPassengerIDs)
                        {
                            if (!noTicPassengerIDs.Contains(checkedPassengerID))
                            {
                                rtMsg.Text = "已选列表中部分旅客已经出票，请检查后再试";
                                return Json(rtMsg, JsonRequestBehavior.AllowGet);
                            }
                        }

                        //2.判断本次出票后是否全部出票
                        bool isIssuanceAll = checkedPassengerIDs.Count == noTicPassengerIDs.Count;

                        //查询PNR表数据
                        B_PNR pnrInfor = myModel.B_PNR.Single(p => p.PNRID == order.PNRID);

                        //3.只有PNRID为订座或者部分出票的状态才可以进行出票的操作
                        if (pnrInfor.PNRStatus == 1 || pnrInfor.PNRStatus == 2)
                        {
                            //4.查询当前用户对应的虚拟账户信息，判断账户余额是否足够支付本次订单
                            S_VirtualAccount VirtualAccount = myModel.S_VirtualAccount.Single(v => v.userID == loginUserID);

                            //判断账户余额是否大于支付金额
                            if (VirtualAccount.accountBalance >= order.payMoney)
                            {
                                //出票操作
                                //1.生成订单
                                //1.1生成订单号（查询当前已生成的订单，编号+1）
                                DateTime dtToday = DateTime.Now;//今天的时间
                                DateTime dtNextday = dtToday.AddDays(1);//明天的时间
                                int nowOrderNum = (from tbOrder in myModel.B_Order
                                                   where tbOrder.payTime >= dtToday && tbOrder.payTime < dtNextday //得出今天的票号数
                                                   select tbOrder).Count();

                                string orderNo = "781" + dtToday.ToString("yyyyMMdd") + (nowOrderNum + 1).ToString("00000");

                                //1.2生成订单数据
                                order.orderNo = orderNo;
                                order.operatorID = loginUserID;
                                order.orderStatus = 2;
                                order.payTime = DateTime.Now;

                                //保存到数据库
                                myModel.B_Order.Add(order);
                                myModel.SaveChanges();

                                //获取出票订单ID
                                var orderId = order.orderID;

                                //3.根据PNR以及旅客ID查询出勾选的旅客信息
                                List<B_PNRPassenger> checkPassengerInfors = (from tbPassenger in myModel.B_PNRPassenger
                                                                             where tbPassenger.PNRID == order.PNRID && checkedPassengerIDs.Contains(tbPassenger.PNRPassengerID)
                                                                             select tbPassenger).ToList();

                                if (checkPassengerInfors.Count > 0)
                                {
                                    //验证当前用户票号是否足够，需要统计当前需要电子票证数（旅客人数*航段数）和当前用户可用票证数
                                    //4.查询航段信息
                                    List<FlightCabinInfo> flightCabinInfos = (from tbSegment in myModel.B_PNRSegment
                                                                              join tbFlight in myModel.S_Flight on tbSegment.flightID equals tbFlight.flightID
                                                                              join tbFlightCabin in myModel.S_FlightCabin on tbSegment.flightCabinID equals tbFlightCabin.flightCabinID
                                                                              join tbCabinType in myModel.S_CabinType on tbSegment.cabinTypeID equals tbCabinType.cabinTypeID
                                                                              where tbSegment.PNRID == order.PNRID &&
                                                                                    tbSegment.segmentType == 0 //航段类型（segmentType） int 默认值0   备注：0：代表正常（预订）生产航段，1：代表电子客票变更产生航段
                                                                                    && tbSegment.invalid == true
                                                                              select new FlightCabinInfo
                                                                              {
                                                                                  flightID = tbFlight.flightID,
                                                                                  cabinPrice = tbFlightCabin.cabinPrice.Value,
                                                                                  cabinTypeID = tbCabinType.cabinTypeID,
                                                                                  flightCabinID = tbFlightCabin.flightCabinID,
                                                                                  PNRSegmentID = tbSegment.PNRSegmentID
                                                                              }).ToList();

                                    //计算需要的电子票证书
                                    int needTicketNum = checkPassengerInfors.Count * flightCabinInfos.Count;

                                    //5.查询出当前用户的可用票号数
                                    List<S_Ticket> usableTicket = (from tbTicket in myModel.S_Ticket
                                                                   where tbTicket.userID == loginUserID && tbTicket.currentTicketNo.CompareTo(tbTicket.endTicketNo) <= 0
                                                                   select tbTicket).ToList();

                                    //计算各个航段的可用号码数
                                    List<TicketInfo> ticketInfors = new List<TicketInfo>();
                                    foreach (S_Ticket ticket in usableTicket)
                                    {
                                        //获取当前票号&结束票号
                                        string strCurNo = ticket.currentTicketNo.Trim();
                                        string strEndNo = ticket.endTicketNo.Trim();

                                        //计算可用票号
                                        int usableNum = Convert.ToInt32(strEndNo) - Convert.ToInt32(strCurNo);

                                        ticketInfors.Add(new TicketInfo()
                                        {
                                            ticket = ticket,
                                            canUseNum = usableNum,
                                            useNum = 0,
                                            currentTicketNo = ticket.currentTicketNo
                                        });
                                    }
                                    //6-计算所有号段总的可用票号是否大于等于当前所需票号
                                    if (ticketInfors.Sum(t => t.canUseNum) >= needTicketNum)
                                    {
                                        //7.先添加 电子客票表（B_ETicket） 再添加PNR出票组表（B_PNRTicketing） 
                                        //为每一个航段的每一个乘客出票 --双层循环
                                        foreach (FlightCabinInfo flightCabinInfo in flightCabinInfos)
                                        {
                                            foreach (B_PNRPassenger checkPassengerInfor in checkPassengerInfors)
                                            {
                                                //7.1 添加电子客票表（B_ETicket）
                                                //7.1.1 生成票号
                                                string strTicketNo = "";
                                                //遍历当前所有可用票号信息
                                                foreach (TicketInfo ticketInfor in ticketInfors)
                                                {
                                                    if (ticketInfor.canUseNum > 0)
                                                    {
                                                        //获取当前票号作为电子客票号
                                                        strTicketNo = ticketInfor.currentTicketNo;

                                                        //计算下一张客票号并保存
                                                        int curTicketNo = Convert.ToInt32(ticketInfor.currentTicketNo.Remove(0, 5));
                                                        int nextTicketNo = curTicketNo + 1;
                                                        string strNextTicketNo = nextTicketNo.ToString("0000000000");
                                                        //更新票号信息
                                                        ticketInfor.currentTicketNo = strNextTicketNo;
                                                        ticketInfor.canUseNum--;
                                                        ticketInfor.useNum++;
                                                        break;
                                                    }
                                                }
                                                //获取当前时间
                                                DateTime dtNow = DateTime.Now;

                                                //保存电子客票表
                                                B_ETicket eTicket = new B_ETicket()
                                                {
                                                    PNRPassengerID = checkPassengerInfor.PNRPassengerID,
                                                    oderID = order.orderID,
                                                    flightID = flightCabinInfo.flightID,
                                                    ticketNo = "E781-" + strTicketNo,
                                                    tickingTime = dtNow,
                                                    ticketPrice = flightCabinInfo.cabinPrice,
                                                    payTypeID = payType,
                                                    operatorID = loginUserID,
                                                    operatingTtime = dtNow,
                                                    eTicketStatusID = 1,
                                                    invoiceStatusID = 1,
                                                    cabinTypeID = flightCabinInfo.cabinTypeID,
                                                    flightCabinID = flightCabinInfo.flightCabinID
                                                };
                                                //保存到数据库
                                                myModel.B_ETicket.Add(eTicket);
                                                myModel.SaveChanges();
                                                //获取电子客票ID
                                                var eTicketID = eTicket.ETicketID;

                                                //7.2 添加PNR出票组表（B_PNRTicketing）
                                                B_PNRTicketing pnrTicket = new B_PNRTicketing()
                                                {
                                                    PNRID = order.PNRID,
                                                    PNRPassengerID = checkPassengerInfor.PNRPassengerID,
                                                    PNRSegmentID = flightCabinInfo.PNRSegmentID,
                                                    ETicketID = eTicketID,
                                                    TicketingTime = dtNow,
                                                    orderID = order.orderID
                                                };
                                                //保存到数据库
                                                myModel.B_PNRTicketing.Add(pnrTicket);
                                                myModel.SaveChanges();
                                            }
                                        }
                                        //8.修改表B_PNR中的PNR状态，全部出票为3，部分出票为2
                                        pnrInfor.PNRStatus = isIssuanceAll ? 3 : 2;
                                        //保存修改
                                        myModel.Entry(pnrInfor).State = System.Data.Entity.EntityState.Modified;
                                        myModel.SaveChanges();

                                        //9-修改表中B_PNRSegment中订座情况
                                        List<B_PNRSegment> PNRSegments = myModel.B_PNRSegment.Where(o => o.PNRID == order.PNRID).ToList();

                                        foreach (B_PNRSegment PNRSegment in PNRSegments)
                                        {
                                            PNRSegment.bookSeatInfo = isIssuanceAll ? 3 : 2;
                                            //保存修改
                                            myModel.Entry(PNRSegment).State = System.Data.Entity.EntityState.Modified;
                                            myModel.SaveChanges(); 
                                        }

                                        //10.更新用户的票号信息
                                        foreach (TicketInfo ticketInfo in ticketInfors)
                                        {
                                            if (ticketInfo.useNum > 0)
                                            {
                                                S_Ticket ticket = ticketInfo.ticket;
                                                ticket.currentTicketNo = ticketInfo.currentTicketNo;
                                                //保存修改
                                                myModel.Entry(ticket).State = System.Data.Entity.EntityState.Modified;
                                                myModel.SaveChanges();
                                            }
                                        }

                                        //11.修改当前账户余额
                                        VirtualAccount.accountBalance -= order.payMoney;
                                        myModel.Entry(VirtualAccount).State = System.Data.Entity.EntityState.Modified;
                                        myModel.SaveChanges();

                                        //12.新增虚拟账户的交易记录
                                        B_TransactionRecord transactionRecord = new B_TransactionRecord()
                                        {
                                            virtualAccountID = VirtualAccount.virtualAccountID,
                                            transactionType = 0,
                                            transactionMoney = order.payMoney,
                                            transactionTime = DateTime.Now,
                                            userID = loginUserID
                                        };
                                        //保存到数据库
                                        myModel.B_TransactionRecord.Add(transactionRecord);
                                        myModel.SaveChanges();

                                        //部分出票操作完成
                                        scope.Complete();

                                        rtMsg.State = true;
                                        rtMsg.Text = "出票成功";
                                        rtMsg.Object = orderId;
                                    }
                                    else
                                    {
                                        rtMsg.Text = "所需票数不足,出票失败!";
                                    }
                                }
                                else
                                {
                                    rtMsg.Text = "没有选中需要出票的旅客信息，出票失败！";
                                }
                            }
                            else
                            {
                                rtMsg.Text = "虚拟账户余额不足,请及时充值";
                            }
                        }
                        else
                        {
                            //如果是全部出票的情况下，部分出票也不允许
                            if (pnrInfor.PNRStatus == 3)
                            {
                                rtMsg.Text = string.Format("PNR： {0}已经全部出票，不能重复出票", pnrInfor.PNRNo);
                            }
                            else if (pnrInfor.PNRStatus == 0)
                            {
                                rtMsg.Text = string.Format("PNR： {0}已经取消订座，不能出票", pnrInfor.PNRNo);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.Write(e);
                        rtMsg.Text = "数据异常";
                    }

                }

            }
            else {
                rtMsg.Text = "数据异常";
            }

            return Json(rtMsg, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region 其他信息修改

        public ActionResult EditOtherInfo(int PNRID)
        {
            ViewBag.PNRID = PNRID;
            return View();
        }
        public ActionResult updateOtherInfor(int PNRID, string contactName, string contactPhone, List<int> delOtherInforIDs, string addOtherInfo, string ticketingInfo)
        {
            ReturnJson rtMsg = new ReturnJson();

            //数据验证
            //1.检查联系人姓名&联系人电话是否为空
            if (!string.IsNullOrEmpty(contactName) && !string.IsNullOrEmpty(contactPhone))
            {
                //2.检查出票信息
                if (!string.IsNullOrEmpty(contactName))
                {
                    //开启事务
                    using (TransactionScope scope = new TransactionScope())
                    {
                        try
                        {
                            //修改其他信息
                            //1. 修改表B_PNR中的联系人姓名&联系方式&出票组信息
                            //1.1 查询出需要修改的PNR信息
                            B_PNR pnr = myModel.B_PNR.Single(p => p.PNRID == PNRID);

                            //1.2修改数据
                            pnr.contactName = contactName;
                            pnr.contactPhone = contactPhone;
                            pnr.TicketingInfo = ticketingInfo;

                            //保存修改
                            myModel.Entry(pnr).State = System.Data.Entity.EntityState.Modified;
                            myModel.SaveChanges();

                            //2.删除选中的其他信息
                            if (delOtherInforIDs != null && delOtherInforIDs.Count > 0)
                            {
                                //查询需要删除的数据
                                List<B_PNROtherInfo> delOtherInfor = (from tbOtherInfo in myModel.B_PNROtherInfo
                                                                      where delOtherInforIDs.Contains(tbOtherInfo.PNROtherInfoID)
                                                                      select tbOtherInfo).ToList();

                                //保存删除
                                myModel.B_PNROtherInfo.RemoveRange(delOtherInfor);
                                myModel.SaveChanges();
                            }
                            

                            //3.新增PNR其他信息
                            //3.1 以换行符为分割依据，分割需要新增的PNR其他信息
                            if (addOtherInfo != null && addOtherInfo != "")
                            {
                                string[] strOtherInfor = addOtherInfo.Split(Environment.NewLine.ToCharArray());
                                List<B_PNROtherInfo> listPNROtherInfor = new List<B_PNROtherInfo>();
                                foreach (string pnrOtherInfor in strOtherInfor)
                                {
                                    listPNROtherInfor.Add(new B_PNROtherInfo()
                                    {
                                        PNRID = PNRID,
                                        otherInfo = pnrOtherInfor
                                    });
                                }
                                //保存新增
                                myModel.B_PNROtherInfo.AddRange(listPNROtherInfor);
                                myModel.SaveChanges();
                            }
                            //提交事务
                            scope.Complete();

                            //返回成功数据
                            rtMsg.State = true;
                            rtMsg.Text = "修改成功";
                        }
                        catch (Exception e)
                        {
                            Console.Write(e);
                            rtMsg.Text = "数据异常，修改失败！";
                        }

                    }
                }
                else
                {
                    rtMsg.Text = "出票信息为空,请检查";
                }
            }
            else
            {
                rtMsg.Text = "联系人姓名或电话为空,请检查";
            }


            return Json(rtMsg, JsonRequestBehavior.AllowGet);
        }
        #endregion

    }
}