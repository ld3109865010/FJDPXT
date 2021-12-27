using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FJDPXT.Model;
using FJDPXT.EntityClass;
using FJDPXT.Common;
using System.Transactions;

namespace FJDPXT.Areas.PNR
{
    public class PNRAppointmentController : Controller
    {
        // GET: PNR/PNRAppointment
        //PNR-->PNR预定
        FJDPXTEntities1 myModel = new FJDPXTEntities1();
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
        public ActionResult Index()
        {
            //查询所有的机场数据
            List<S_Airport> airports = myModel.S_Airport.ToList();

            //传递数据到页面
            ViewBag.airports = airports;

            //清除session中关于PNR预订的部分  Remove清除指定name的session
            Session.Remove("flightCabinIDs");
            Session.Remove("flightCabinInfos");

            return View();
        }

        #region 航班\舱位选择

        public ActionResult SelectFlightPage()
        {
            return View();
        }
        //查询航段 航班 航班舱位信息
        public ActionResult SelectFlight(int segmentNum, int[] airports, string[] dates)
        {
            //容器-存放查询出的信息
            List<SegmentVo> listSegment = new List<SegmentVo>();

            //验证接收到的参数是否正确 航段数=机场ID数组长度-1 = 起飞日期数组长度
            if (segmentNum == airports.Length - 1 && segmentNum == dates.Length)
            {
                //循坏航段数 查询出需要的数据
                for (int segmentIndex = 0; segmentIndex < segmentNum; segmentIndex++)
                {
                    //存放本航段的数据
                    SegmentVo segmentVo = new SegmentVo();

                    //1-本航段的起飞降落机场ID
                    int orangeID = airports[segmentIndex];//起飞机场ID
                    int destinationID = airports[segmentIndex + 1];//降落机场ID

                    //2-查询出起飞机场和降落机场的数据
                    S_Airport orangeAirport = myModel.S_Airport.Single(o => o.airportID == orangeID);
                    S_Airport destinationAirport = myModel.S_Airport.Single(o => o.airportID == destinationID);

                    //2.1-设置segmentVo的起飞机场和降落机场信息
                    segmentVo.orangeCityName = orangeAirport.cityName;
                    segmentVo.orangeAirportName = orangeAirport.airportName;
                    segmentVo.destinationCityName = destinationAirport.cityName;
                    segmentVo.destinationAirportName = destinationAirport.airportName;

                    //3-设置航段的日期
                    segmentVo.strDate = dates[segmentIndex];
                    DateTime flightDate = Convert.ToDateTime(dates[segmentIndex]);

                    //4-查询符合该航段条线的航班
                    var query = from tabFlight in myModel.S_Flight
                                join tabOrangeAirport in myModel.S_Airport
                                on tabFlight.orangeID equals tabOrangeAirport.airportID
                                join tabDestinationAirport in myModel.S_Airport
                                on tabFlight.destinationID equals tabDestinationAirport.airportID
                                join tabPlanType in myModel.S_PlanType
                                on tabFlight.planTypeID equals tabPlanType.planTypeID
                                where tabFlight.flightDate == flightDate &&
                                tabFlight.orangeID == orangeID &&
                                tabFlight.destinationID == destinationID
                                orderby tabFlight.departureTime  //根据航班的起飞时间从早到晚排序
                                select new FlightVo()
                                {
                                    flightID = tabFlight.flightID,//航班ID
                                    flightCode = tabFlight.flightCode,//航班号
                                    orangeID = tabFlight.orangeID,//起飞机场ID
                                    destinationID = tabFlight.destinationID,//降落机场ID
                                    flightDate = tabFlight.flightDate,//航班日期
                                    departureTime = tabFlight.departureTime,//航班起飞时间
                                    arrivalTime = tabFlight.arrivalTime,//降落时间
                                    planTypeID = tabFlight.planTypeID,//机型ID
                                    standardPrice = tabFlight.standardPrice,//标准价格
                                    //扩展字段
                                    orangeCityName = tabOrangeAirport.cityName,//起飞城市名称
                                    orangeAirportName = tabOrangeAirport.airportName,//起飞机场名称
                                    destinationCityName = tabDestinationAirport.cityName,//降落城市名称
                                    destinationAirportName = tabDestinationAirport.airportName,//降落机场名称
                                    planTypeName = tabPlanType.planTypeName,//飞机机型名称
                                    //使用子查询 查询出航班的舱位数据
                                    flightCabins = (from tabFlightCabin in myModel.S_FlightCabin
                                                    join tabCabinType in myModel.S_CabinType
                                                    on tabFlightCabin.cabinTypeID equals tabCabinType.cabinTypeID
                                                    where tabFlightCabin.flightID == tabFlight.flightID
                                                    select new FlightCabinVo()
                                                    {
                                                        flightCabinID = tabFlightCabin.flightCabinID,//航班舱位ID
                                                        flightID = tabFlightCabin.flightID,//航班ID
                                                        cabinTypeID = tabFlightCabin.cabinTypeID,//舱位等级ID
                                                        seatNum = tabFlightCabin.seatNum,//座位数
                                                        cabinPrice = tabFlightCabin.cabinPrice,//价格
                                                        sellSeatNum = tabFlightCabin.sellSeatNum,//售出座位数
                                                        //扩展字段
                                                        cabinTypeCode = tabCabinType.cabinTypeCode,//舱位等级编号
                                                        cabinTypeName = tabCabinType.cabinTypeName//舱位等级名称
                                                    }).ToList()

                                };
                    //判断日期是否是今天
                    DateTime dtToDay = DateTime.Now.Date;//获取今天的日期部分
                    TimeSpan tsNow = DateTime.Now.TimeOfDay;//获取现在的时间
                    //判断查询的航班是否是今天的
                    if (flightDate == dtToDay)
                    {
                        query = query.Where(o => o.departureTime > tsNow);
                    }
                    //查询出航段对应的航班数据
                    List<FlightVo> flightList = query.ToList();
                    segmentVo.flightList = flightList;

                    //把segmentVo添加到listSegment
                    listSegment.Add(segmentVo);
                }
            }


            return Json(listSegment, JsonRequestBehavior.AllowGet);
        }




        #endregion

        #region 旅客信息录入页面

        public ActionResult EnterInfoPage(int[] flightCabinIDs,int passengerNum)
        {
            //===查询出用户选择的航班舱位数据
            List<FlightCabinInfo> flightCabinInfos = new List<FlightCabinInfo>();
            for (int i = 0; i < flightCabinIDs.Length; i++)
            {
                //取出航班舱位ID
                int flightCabinID = flightCabinIDs[i];

                //查询航班信息 航班舱位信息
                FlightCabinInfo flightCabinInfo= (from tabFlightCabin in myModel.S_FlightCabin
                                                  join tabFlight in myModel.S_Flight
                                                   on tabFlightCabin.flightID equals tabFlight.flightID
                                                  join tabOrangeAirport in myModel.S_Airport
                                                   on tabFlight.orangeID equals tabOrangeAirport.airportID
                                                  join tabDestinationAirport in myModel.S_Airport
                                                   on tabFlight.destinationID equals tabDestinationAirport.airportID
                                                  join tabCabinType in myModel.S_CabinType
                                                   on tabFlightCabin.cabinTypeID equals tabCabinType.cabinTypeID
                                                  where tabFlightCabin.flightCabinID == flightCabinID
                                                  select new FlightCabinInfo()
                                                  {
                                                      flightID = tabFlight.flightID,//航班ID
                                                      flightCode = tabFlight.flightCode,//航班号
                                                      orangeID = tabFlight.orangeID,//始发地ID
                                                      destinationID = tabFlight.destinationID,//目的地ID
                                                      departureTime = tabFlight.departureTime,//起飞时间
                                                      arrivalTime = tabFlight.arrivalTime,//降落时间
                                                      planTypeID = tabFlight.planTypeID,//飞机类型ID
                                                      flightDate = tabFlight.flightDate,//起飞日期
                                                      standardPrice = tabFlight.standardPrice,//标准价格

                                                      cabinTypeID = tabFlightCabin.cabinTypeID.Value,//舱位类型ID
                                                      cabinTypeCode = tabCabinType.cabinTypeCode,//舱位类型编号
                                                      cabinTypeName = tabCabinType.cabinTypeName,//舱位类型名称
                                                      cabinPrice = tabFlightCabin.cabinPrice.Value,//舱位价格
                                                      seatNum = passengerNum,//预订座位数量 就是乘客数量
                                                      orangeCity = tabOrangeAirport.cityName,//始发地城市名称
                                                      destinationCity = tabDestinationAirport.cityName//目的地城市名称
                                                  }).SingleOrDefault() ;
                flightCabinInfos.Add(flightCabinInfo);
            }
            //查询旅客类型 for 下拉框
            List<S_PassengerType> passengerTypes = myModel.S_PassengerType.ToList();

            //查询证件类型 for 下拉框
            List<S_CertificatesType> certificatesTypes = myModel.S_CertificatesType.ToList();

            //把数据传递到页面
            ViewBag.flightCabinInfos = flightCabinInfos;//旅客选择航班舱位信息
            ViewBag.passengerNum = passengerNum;//旅客人数
            ViewBag.passengerTypes = passengerTypes;//旅客类型
            ViewBag.certificatesTypes = certificatesTypes;//证件类型

            //==将选择的航班舱位ID保存到session，用于最后保存PNR时数据验证
            Session["flightCabinIDs"] = flightCabinIDs;
            Session["flightCabinInfos"] = flightCabinInfos;
            

            return View();
        }

        public ActionResult EnterInfo(List<PassengerVo> passengers, string contactName, string contactPhone,
            string ticketingInfo, string PNROtherInfo, int[] flightCabinIDs)
        {
            ReturnJson msg = new ReturnJson();

            //数据验证
            //旅客信息
            if (passengers != null)
            {
                //遍历旅客信息
                for (int i = 0; i <passengers.Count; i++)
                {
                    //旅客姓名
                    if (string.IsNullOrEmpty(passengers[i].passengerName))
                    {
                        msg.Text = "第"+(i+1)+"个旅客姓名未填写";
                        return Json(msg, JsonRequestBehavior.AllowGet);
                    }
                    //旅客证件号
                    if (string.IsNullOrEmpty(passengers[i].certificatesCode))
                    {
                        msg.Text = "第" + (i + 1) + "个旅客证件号未填写";
                        return Json(msg, JsonRequestBehavior.AllowGet);
                    }
                    if (passengers[i].certificatesTypeID == 1)
                    {
                        //验证身份证号是否正确
                        if (!IdCardHelper.CheckIdCard(passengers[i].certificatesCode))
                        {
                            msg.Text = "第" + (i + 1) + "个旅客证件号不正确,请重新填写";
                            return Json(msg, JsonRequestBehavior.AllowGet);
                        }
                        //根据身份证号获取年龄
                        int passengerAge = IdCardHelper.GetAgeByIdCard(passengers[i].certificatesCode);
                        //判断旅客类型和年龄是否符合
                        //选择的旅客类型为成人,但是身份证年龄小于18
                        if(passengers[i].passengerTypeID == 1 && passengerAge < 18)
                        {
                            msg.Text = "第" + (i + 1) + "个旅客年龄小于18与所选的[成人]不符,请重新选择";
                            return Json(msg, JsonRequestBehavior.AllowGet);
                        }
                        //选择的旅客类型为儿童,但是身份证年龄大于18
                        if (passengers[i].passengerTypeID == 2 && passengerAge > 18)
                        {
                            msg.Text = "第" + (i + 1) + "个旅客年龄大于18与所选的[儿童]不符,请重新选择";
                            return Json(msg, JsonRequestBehavior.AllowGet);
                        }
                        //判断身份证是否重复
                        for (int j = i+1; j <passengers.Count; j++)
                        {
                            if(passengers[j].certificatesTypeID==1 &&
                                passengers[i].certificatesCode.Trim() == passengers[j].certificatesCode.Trim())
                            {
                                msg.Text = string.Format("第{0}个旅客的身份证号与第{1}个旅客的身份证号相同,请检查", i + 1, j + 1);
                                return Json(msg, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }
                    //====检查常旅客号
                    //判断是否输入常旅客号
                    if (!string.IsNullOrEmpty(passengers[i].frequentPassengerNo))
                    {
                        try
                        {
                            string frequentPassengerNo = passengers[i].frequentPassengerNo.Trim();
                            //判断常旅客号是否存在
                            S_FrequentPassenger dbFrequentPassenger = myModel.S_FrequentPassenger
                                .Single(o => o.frequentPassengerNo == frequentPassengerNo);
                            //判断常旅客号信息和添加的用户信息是否一致
                            if (dbFrequentPassenger.frequentPassengerName.Trim() == passengers[i].passengerName.Trim() &&
                                dbFrequentPassenger.certificatesTypeID == passengers[i].certificatesTypeID &&
                                dbFrequentPassenger.certificatesCode.Trim() == passengers[i].certificatesCode.Trim())
                            {
                                //常旅客号信息是否匹配
                                //检查常旅客号的同时把 常旅客号转成对应的常旅客ID
                                passengers[i].frequentPassengerID = dbFrequentPassenger.frequentPassengerID;
                            }
                            else
                            {
                                msg.Text = string.Format("第{0}个旅客的信息与常旅客信息不匹配,请检查", i + 1);
                                return Json(msg, JsonRequestBehavior.AllowGet);
                            }
                            
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            msg.Text = string.Format("第{0}个常旅客号不存在,请检查", i + 1);
                            return Json(msg, JsonRequestBehavior.AllowGet);
                        }
                    }

                }
            }
            else
            {
                msg.Text="数据异常,请重试";
            }

            //==验证flightCabinIDs
            //比较传递的flightCabinIDs和session中的ID是否一致
            //as C#提供的一个数据转换关键字 ,如果可以转换,就返回转换后的值,如果不能转换就返回null,不会出现异常
            //as 使用时搭配引用数据类型
            int[] sessionflightCabinIDs = Session["flightCabinIDs"] as int[];
            //判断sessionflightCabinIDs是否有数据
            if (sessionflightCabinIDs == null)
            {
                msg.Text = "数据异常1";
                return Json(msg, JsonRequestBehavior.AllowGet);
            }
            //判断sessionflightCabinIDs的长度和传递sessionFlightCabinIDs元素个数是否一致
            if (sessionflightCabinIDs.Length != flightCabinIDs.Length)
            {
                msg.Text = "数据异常2";
                return Json(msg, JsonRequestBehavior.AllowGet);
            }
            //判断sessionflightCabinIDs和传递sessionFlightCabinIDs是否一致
            for (int i = 0; i < sessionflightCabinIDs.Length; i++)
            {
                if (sessionflightCabinIDs[i] != flightCabinIDs[i])
                {
                    msg.Text = "数据异常3";
                    return Json(msg, JsonRequestBehavior.AllowGet);
                }
            }
            //联系人姓名 联系人电话
            if (string.IsNullOrEmpty(contactName))
            {
                msg.Text="请填写联系人姓名";
                return Json(msg, JsonRequestBehavior.AllowGet);

            }
            if (string.IsNullOrEmpty(contactPhone))
            {
                msg.Text = "请填写联系人电话";
                return Json(msg, JsonRequestBehavior.AllowGet);
            }
            //保存数据
            //1-保存 B_PNR表
            //=1.1-生成PNR编号
            string strPNRNo = PNRCodeHelper.CreatePNR();
            //=1.2-构建要保存的B_PNR对象
            B_PNR savePNR = new B_PNR()
            {
                PNRNo = strPNRNo,//PNR 编号
                contactName = contactName,//联系人姓名
                contactPhone = contactPhone,//联系人电话
                TicketingInfo = ticketingInfo,//出票信息
                PNRStatus = 1,//1：PNR生成后默认状态，即已经订座（HK）,2:部分出票，3：全部出票，0：取消订座   -1：pnr删除
                operatorID = loginUserID,//操作人Id 就是当前登录用户ID
                createTime = DateTime.Now//创建时间 现在
            };
            //涉及多表的操作 启用事务
            using (TransactionScope scope = new TransactionScope())
            {
                try
                {
                    //=1.3-保存B_PNR 信息
                    myModel.B_PNR.Add(savePNR);
                    if (myModel.SaveChanges() > 0)
                    {
                        //=1.4-获取保存后的PNRID
                        int PNRID = savePNR.PNRID;


                        //==2-保存PNR 旅客信息
                        //=2.1-把List<PassengerVo>转为 List<B_PNRPassenger>类型
                        //保存转换后的数据
                        List<B_PNRPassenger> savePNRPassengers = new List<B_PNRPassenger>();
                        foreach (PassengerVo passenger in passengers)
                        {
                            savePNRPassengers.Add(new B_PNRPassenger()
                            {
                                PNRID = PNRID,//PNRID
                                passengerNo = passenger.passengerNo,//PNR旅客编号
                                passengerName = passenger.passengerName,///旅客姓名
                                passengerTypeID = passenger.passengerTypeID,//旅客类型Id
                                certificatesTypeID = passenger.certificatesTypeID,//旅客证件类型ID
                                certificatesCode = passenger.certificatesCode,//证件号
                                frequentPassengerID = passenger.frequentPassengerID//常旅客ID
                            });
                        }
                        //=2.2-保存savePNRPassengers到数据库
                        myModel.B_PNRPassenger.AddRange(savePNRPassengers);
                        myModel.SaveChanges();


                        //==3-保存PNR航段信息 B_PNRSegment
                        //在保存航段信息的同时 需要修改航班舱位表S_FlightCabin对应数据的售出座位数
                        //=3.1-遍历用户选择的航班舱位ID,构建需要保存的PNR航段信息 B_PNRSegment数据 
                        //      以及修改航班舱位表S_FlightCabin对应数据的售出座位数                  
                        List<B_PNRSegment> savePNRSegments = new List<B_PNRSegment>();
                        for (int i = 0; i <flightCabinIDs.Length; i++)
                        {
                            int flightCabinID = flightCabinIDs[i];
                            try
                            {
                                //3.1.1-根据航班舱位ID,查询出航班舱位数据
                                S_FlightCabin dbFlightCabin = myModel.S_FlightCabin.Single
                                    (o => o.flightCabinID == flightCabinID);
                                //3.1.2-判断剩余座位数是否充足 旅客人数<=座位数-售出座位数
                                if (passengers.Count <= dbFlightCabin.seatNum - dbFlightCabin.sellSeatNum)
                                {
                                    //剩余座位数足够
                                    //3.1.3-修改航班舱位的售出座位数 (占座)  售出座位数=售出座位数+旅客人数
                                    dbFlightCabin.sellSeatNum = dbFlightCabin.sellSeatNum + passengers.Count;
                                    //3.1.4-保存修改后的航班舱位数据
                                    myModel.Entry(dbFlightCabin).State = System.Data.Entity.EntityState.Modified;
                                    myModel.SaveChanges();

                                    //3.1.5-构建需要保存PNR航段数据
                                    savePNRSegments.Add(new B_PNRSegment()
                                    {
                                        PNRID = PNRID,//PNRID
                                        segmentNo = i + 1,//PNR航段序号
                                        flightID = dbFlightCabin.flightID,//航班ID
                                        cabinTypeID = dbFlightCabin.cabinTypeID,//舱位等级Id
                                        flightCabinID = dbFlightCabin.flightCabinID,//航班舱位ID
                                        bookSeatNum = passengers.Count,//订座数=旅客人数
                                        bookSeatInfo = 1,//订座情况: 1：已预订座位；,2:部分出票，3：全部出票,0：取消订座 
                                        segmentType = 0,//航段类型标识  0:预订 1:换开
                                        invalid = true,//数据有效

                                    });
                                }
                                else
                                {
                                    msg.Text = "选择的航班舱位剩余座位数不足,请重新选择";
                                    return Json(msg, JsonRequestBehavior.AllowGet);
                                }
                                
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                msg.Text = "选择的航班舱位信息异常,请检查";
                                return Json(msg, JsonRequestBehavior.AllowGet);
                            }
                        }
                        //=3.2-保存PNR航段信息到数据库
                        myModel.B_PNRSegment.AddRange(savePNRSegments);
                        myModel.SaveChanges();

                        //==4-保存PNR的其他信息 B_PNROtherInfo
                        List<B_PNROtherInfo> savePNROtherInfos = new List<B_PNROtherInfo>();
                        //4.1-按照换行符分割PNR其他信息
                        string[] strOtherInfos = PNROtherInfo.Split(Environment.NewLine.ToArray());
                        foreach (string strOtherInfo in strOtherInfos)
                        {
                            if (!string.IsNullOrEmpty(strOtherInfo))
                            {
                                savePNROtherInfos.Add(new B_PNROtherInfo()
                                {
                                    PNRID = PNRID,
                                    otherInfo = strOtherInfo,
                                });
                            }
                        }
                        //4.2-保存PNR信息到数据库
                        myModel.B_PNROtherInfo.AddRange(savePNROtherInfos);
                        myModel.SaveChanges();

                        //提交事务
                        scope.Complete();

                        //清除session
                        Session.Remove("flightCabinIDs");
                        Session.Remove("flightCabinInfos");

                        //返回PNR生成成功
                        msg.State = true;
                        msg.Text = "预定成功!请确认订座信息!";
                        msg.Code = PNRID.ToString();//将pnrID返回页面
                    }

                    else
                    {
                        msg.Text = "订座失败";
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    
                }


            }




            return Json(msg, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region PNR信息确认

        public ActionResult PnrInfoConfirm(int PNRID)
        {
            try
            {
                //1-查询出PNR信息
                B_PNR pnrInfo = myModel.B_PNR.Single(o => o.PNRID == PNRID);

                //2-查询PNR旅客信息
                List<PassengerVo> passengers =
                     (from tabPassenger in myModel.B_PNRPassenger
                          //旅客类型
                      join tabPassengerType in myModel.S_PassengerType
                         on tabPassenger.passengerTypeID equals tabPassengerType.passengerTypeID
                      //证件类型
                      join tabCertificatesType in myModel.S_CertificatesType
                         on tabPassenger.certificatesTypeID equals tabCertificatesType.certificatesTypeID
                      //常旅客表 左连接tabFrequentPassengerA与tabFrequentPassengerB都是临时变量可以随便取 下面不会再用
                      join tabFrequentPassengerA in myModel.S_FrequentPassenger
                      on tabPassenger.frequentPassengerID equals tabFrequentPassengerA.frequentPassengerID
                      into tabFrequentPassengerB
                      from tabFrequentPassenger in tabFrequentPassengerB.DefaultIfEmpty()
                      where tabPassenger.PNRID == PNRID          //DefaultIfEmpty返回指定序列的元素;如果序列为空,则返回单一实例集合中的类型参数的默认值(null);
                      select new PassengerVo()
                      {
                          PNRPassengerID = tabPassenger.PNRPassengerID,//pnr旅客Id
                          PNRID = tabPassenger.PNRID,//PNRID
                          passengerNo = tabPassenger.passengerNo,//pnr旅客编号
                          passengerName = tabPassenger.passengerName,//pnr旅客姓名
                          passengerTypeID = tabPassenger.passengerTypeID,//旅客类型ID
                          certificatesTypeID = tabPassenger.certificatesTypeID,//旅客证件类型ID
                          certificatesCode = tabPassenger.certificatesCode,//证件号
                          frequentPassengerID = tabPassenger.frequentPassengerID,//常旅客Id
                          //扩展
                          passengerType = tabPassengerType.passengerType,//旅客类型
                          certificatesType = tabCertificatesType.certificatesType,//证件类型
                          //左连接表的取值可能为空所以得 使用三目运算符
                          frequentPassengerNo = tabFrequentPassenger != null ? tabFrequentPassenger.frequentPassengerNo : ""//常旅客号
                      }).ToList();

                //==3-查询PNR航段信息
                List<FlightCabinInfo> flightCabinInfos=
                    (from tabPNRSegment in myModel.B_PNRSegment
                         //航班舱位表
                     join tabFlightCabin in myModel.S_FlightCabin
                        on tabPNRSegment.flightCabinID equals tabFlightCabin.flightCabinID
                     //航班表
                     join tabFlight in myModel.S_Flight
                        on tabPNRSegment.flightID equals tabFlight.flightID
                     //舱位等级表
                     join tabCabinType in myModel.S_CabinType
                       on tabPNRSegment.cabinTypeID equals tabCabinType.cabinTypeID
                     //机场表 起飞
                     join tabOrangeAirport in myModel.S_Airport
                       on tabFlight.orangeID equals tabOrangeAirport.airportID
                     //机场表 降落
                     join tabDestinationAirport in myModel.S_Airport
                       on tabFlight.destinationID equals tabDestinationAirport.airportID
                     //机型表
                     join tabPlanType in myModel.S_PlanType
                        on tabFlight.planTypeID equals tabPlanType.planTypeID
                     //条件
                     where tabPNRSegment.PNRID == PNRID
                     select new FlightCabinInfo()
                     {
                         flightID = tabFlight.flightID,//航班ID
                         flightCode = tabFlight.flightCode,//航班号
                         orangeID = tabFlight.orangeID,//起飞机场ID
                         destinationID = tabFlight.destinationID,//降落机场ID
                         flightDate = tabFlight.flightDate,//航班日期
                         departureTime = tabFlight.departureTime,//起飞时间
                         arrivalTime = tabFlight.arrivalTime,//降落时间
                         planTypeID = tabFlight.planTypeID,//机型ID
                         standardPrice = tabFlight.standardPrice,//标准价格

                         PNRSegmentID = tabPNRSegment.PNRSegmentID,//航段ID
                         segmentNo = tabPNRSegment.segmentNo.Value,//航段号
                         flightCabinID = tabPNRSegment.flightCabinID.Value,//航班舱位ID
                         cabinTypeID = tabPNRSegment.cabinTypeID.Value,//航班舱位类型ID
                         seatNum = tabPNRSegment.bookSeatNum.Value,//订座数量
                         bookSeatInfo = tabPNRSegment.bookSeatInfo.Value,//订座状态
                         segmentType = tabPNRSegment.segmentType,//航段类型
                         //扩展
                         orangeCity = tabOrangeAirport.cityName,//起飞城市
                         destinationCity = tabDestinationAirport.cityName,//降落城市
                         planTypeName = tabPlanType.planTypeName,//机型
                         cabinPrice = tabFlightCabin.cabinPrice.Value,//舱位价格
                         cabinTypeCode = tabCabinType.cabinTypeCode,//舱位等级编号
                         cabinTypeName = tabCabinType.cabinTypeName//舱位等级名称

                     }).ToList();
                //=4-查询其他PNR信息
                List<B_PNROtherInfo> pnrOtherInfos = myModel.B_PNROtherInfo
                    .Where(o => o.PNRID == PNRID)
                    .ToList();

                //=5-查询登录用户对应的虚拟账户信息
                VirtualAccountVo virtualAccount =
                    (from tabVirtualAccount in myModel.S_VirtualAccount
                     join tabUser in myModel.S_User
                        on tabVirtualAccount.userID equals tabUser.userID
                     where tabVirtualAccount.userID == loginUserID
                     select new VirtualAccountVo()
                     {
                         virtualAccountID = tabVirtualAccount.virtualAccountID,//虚拟账户ID
                         account = tabVirtualAccount.account,//虚拟账号
                         accountBalance = tabVirtualAccount.accountBalance,//虚拟账户余额
                         userID = tabVirtualAccount.userID,//用户ID
                         //扩展
                         jobNumber = tabUser.jobNumber//工号
                     }).Single();

                //将查询出的数据传递到页面
                ViewBag.pnrInfo = pnrInfo;
                ViewBag.passengers = passengers;
                ViewBag.flightCabinInfos = flightCabinInfos;
                ViewBag.pnrOtherInfos = pnrOtherInfos;
                ViewBag.virtualAccount = virtualAccount;

                return View();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return RedirectToAction("Index");
            }
            
        }


        #endregion

        #region 出票操作

        public ActionResult IssueTickets(B_Order order,int payType)
        {
            ReturnJson msg = new ReturnJson();

            //======数据验证
            //1-验证PNRID 不为0, 并且 支付金额=总金额-代理费
            if(order.PNRID !=0 && order.payMoney == order.totalPrice - order.agencyFee)
            {
                //开启事务
                using (TransactionScope scope=new TransactionScope())
                {
                    try
                    {
                        //2-验证PNR状态
                        B_PNR dbPNR = myModel.B_PNR.Single(o => o.PNRID == order.PNRID);
                        //2.1-验证PNR状态是否为1,1：PNR生成后默认状态，即已经订座（HK）,2:部分出票，3：全部出票，0：取消订座/pnr删除
                        if (dbPNR.PNRStatus == 1)
                        {
                            //3验证虚拟账户余额是否大于或等于支付金额
                            S_VirtualAccount dbVirtualAccount = myModel.S_VirtualAccount
                                .Single(o => o.userID == loginUserID);
                            //3.1-判断虚拟账户余额是否大于等予支付金额
                            if (dbVirtualAccount.accountBalance >= order.payMoney)
                            {
                                //4-检查PNR旅客数  避免没有旅客然后还继续出票 出现逻辑混乱
                                List<B_PNRPassenger> dbpassengers = myModel.B_PNRPassenger.
                                    Where(o => o.PNRID == order.PNRID)
                                    .ToList();
                                //4.1-PNR中的旅客数需要大于0
                                if (dbpassengers.Count > 0)
                                {
                                    //5-检查PNR下的航段数
                                    List<FlightCabinInfo> flightCabinInfos =
                                        (from tabPNRSegment in myModel.B_PNRSegment
                                             //航班舱位表
                                         join tabFlightCabin in myModel.S_FlightCabin
                                            on tabPNRSegment.flightCabinID equals tabFlightCabin.flightCabinID
                                         //航班表
                                         join tabFlight in myModel.S_Flight
                                            on tabPNRSegment.flightID equals tabFlight.flightID
                                         //舱位等级表
                                         join tabCabinType in myModel.S_CabinType
                                           on tabPNRSegment.cabinTypeID equals tabCabinType.cabinTypeID
                                         //机场表 起飞
                                         join tabOrangeAirport in myModel.S_Airport
                                           on tabFlight.orangeID equals tabOrangeAirport.airportID
                                         //机场表 降落
                                         join tabDestinationAirport in myModel.S_Airport
                                           on tabFlight.destinationID equals tabDestinationAirport.airportID
                                         //机型表
                                         join tabPlanType in myModel.S_PlanType
                                            on tabFlight.planTypeID equals tabPlanType.planTypeID
                                         //条件
                                         where tabPNRSegment.PNRID == order.PNRID
                                         select new FlightCabinInfo()
                                         {
                                             flightID = tabFlight.flightID,//航班ID
                                             flightCode = tabFlight.flightCode,//航班号
                                             orangeID = tabFlight.orangeID,//起飞机场ID
                                             destinationID = tabFlight.destinationID,//降落机场ID
                                             flightDate = tabFlight.flightDate,//航班日期
                                             departureTime = tabFlight.departureTime,//起飞时间
                                             arrivalTime = tabFlight.arrivalTime,//降落时间
                                             planTypeID = tabFlight.planTypeID,//机型ID
                                             standardPrice = tabFlight.standardPrice,//标准价格

                                             PNRSegmentID = tabPNRSegment.PNRSegmentID,//航段ID
                                             segmentNo = tabPNRSegment.segmentNo.Value,//航段号
                                             flightCabinID = tabPNRSegment.flightCabinID.Value,//航班舱位ID
                                             cabinTypeID = tabPNRSegment.cabinTypeID.Value,//航班舱位类型ID
                                             seatNum = tabPNRSegment.bookSeatNum.Value,//订座数量
                                             bookSeatInfo = tabPNRSegment.bookSeatInfo.Value,//订座状态
                                             segmentType = tabPNRSegment.segmentType,//航段类型
                                             //扩展
                                             orangeCity = tabOrangeAirport.cityName,//起飞城市
                                             destinationCity = tabDestinationAirport.cityName,//降落城市
                                             planTypeName = tabPlanType.planTypeName,//机型
                                             cabinPrice = tabFlightCabin.cabinPrice.Value,//舱位价格
                                             cabinTypeCode = tabCabinType.cabinTypeCode,//舱位等级编号
                                             cabinTypeName = tabCabinType.cabinTypeName//舱位等级名称

                                         }).ToList();
                                    //5.1- 判断PNR航段数
                                    if (flightCabinInfos.Count > 0)
                                    {
                                        //6- 判断当前用户的票号数是否大于等于需要的票号数
                                        //6.1-计算需要的票号数 旅客数*航段数
                                        int needTicketNum = dbpassengers.Count * flightCabinInfos.Count;

                                        //6.2-查询用户的票号信息
                                        List<S_Ticket> dbUserTickets =
                                            (from tabTicket in myModel.S_Ticket
                                             where tabTicket.userID == loginUserID &&//当前登录用户
                                             tabTicket.currentTicketNo.CompareTo(tabTicket.endTicketNo) <= 0 &&// 当前票号小于等于结束票号
                                             tabTicket.isEnable == true// 票号需要被启用
                                             select tabTicket
                                             ).ToList();
                                        //6.3-统计出该用户各个票号段的可用客票信息
                                        List<TicketInfo> ticketInfos = new List<TicketInfo>();
                                        foreach (S_Ticket userTicket in dbUserTickets)
                                        {
                                            //获取当前票号和结票号
                                            string currentTicketNo = userTicket.currentTicketNo;
                                            string endTicketNo = userTicket.endTicketNo;
                                            //计算可用使用的票数
                                            int canUseTicketNum = Convert.ToInt32(endTicketNo) - Convert.ToInt32(currentTicketNo) + 1;

                                            //存放ticketInfos
                                            ticketInfos.Add(new TicketInfo()
                                            {
                                                ticket = userTicket,
                                                canUseNum = canUseTicketNum,//可用票数
                                                useNum = 0,//本次使用数 0,目前还没有使用
                                                currentTicketNo = currentTicketNo
                                            });
                                        }
                                            //6.4-计算总的可用票数              Sum累和
                                            int totalCanUseTicketNum = ticketInfos.Sum(o => o.canUseNum);
                                            //6.5- 判断总的可用票数是否大于等于需要的票号数
                                            if (totalCanUseTicketNum >= needTicketNum)
                                            {
                                            //数据保存
                                            DateTime dtNow = DateTime.Now;
                                            //1-保存订单信息 B_order
                                            //1.1-生成订单的编号
                                            string strOrederNo = "";
                                            //1.1.1-查询出当天的订单数
                                            DateTime dtToday = dtNow.Date;
                                            DateTime dtTomorrow = dtNow.AddDays(1);
                                            int todayOrderCount=(from tabOrder in myModel.B_Order
                                                                 where tabOrder.payTime>=dtToday &&
                                                                 tabOrder.payTime<dtTomorrow
                                                                 select tabOrder).Count();

                                            //1.1.2-拼接订单编号
                                            strOrederNo = "781" + dtNow.ToString("yyyyMMdd") + (todayOrderCount+1).ToString("00000");

                                            //1.2-设置订单的其他信息
                                            order.orderNo = strOrederNo;//订单编号
                                            order.operatorID=loginUserID;//操作人ID
                                            order.orderStatus = 1;// 1:未支付，2：订单已经支付；0：订单取消
                                            order.payTime = dtNow;//订单支付时间

                                            //1.3-保存订单信息到数据库
                                            myModel.B_Order.Add(order);
                                            if (myModel.SaveChanges() > 0)
                                            {
                                                //1.4-获取出订单ID
                                                int saveOrderID = order.orderID;

                                                //2-遍历PNR航段和PNR旅客数据,生成保存电子客票(B_ETicket)信息,
                                                //  再保存PNR出票组信息(B_PNRTicketing)
                                                //2.1-双层循环 遍历PNR航段 和 PNR旅客
                                                foreach (FlightCabinInfo flightCabinInfo in flightCabinInfos)
                                                {
                                                    foreach (B_PNRPassenger passenger in dbpassengers)
                                                    {
                                                        //2.2-获取出电子客票号
                                                        string strTicketNo = "";
                                                        //2.2.1-遍历用户的票号信息
                                                        foreach (TicketInfo ticketInfo in ticketInfos)
                                                        {
                                                            //判断该票号信息是否还有可以使用的票
                                                            if (ticketInfo.canUseNum > 0)
                                                            {
                                                                //获取当前票号
                                                                strTicketNo = "E781-" + ticketInfo.currentTicketNo;
                                                                //计算下一票号
                                                                int intTicketNo = Convert.ToInt32(ticketInfo.currentTicketNo);
                                                                string nextTicketNo = (intTicketNo + 1).ToString("0000000000");

                                                                //更新ticketInfo的信息
                                                                ticketInfo.currentTicketNo = nextTicketNo;//当前票号
                                                                ticketInfo.canUseNum--;//可用票数-1
                                                                ticketInfo.useNum++;//使用票数+1

                                                                break;
                                                            }

                                                        }
                                                        //2.2.2-判断票号不为空
                                                        if (!string.IsNullOrEmpty(strTicketNo))
                                                        {
                                                            //2.3-保存电子客票(B_ETicket)信息
                                                            B_ETicket saveETicket = new B_ETicket()
                                                            {
                                                                PNRPassengerID = passenger.PNRPassengerID,//PNR旅客ID
                                                                oderID = saveOrderID,//订单ID
                                                                flightID = flightCabinInfo.flightID,//航班ID
                                                                flightCabinID = flightCabinInfo.flightCabinID,//航班舱位ID
                                                                cabinTypeID = flightCabinInfo.cabinTypeID,//舱位等级ID
                                                                ticketPrice = flightCabinInfo.cabinPrice,//舱位价格
                                                                ticketNo = strTicketNo,//电子票号
                                                                tickingTime = dtNow,//出票时间
                                                                payTypeID = payType,//付款方式
                                                                operatorID = loginUserID,//操作人ID
                                                                operatingTtime = dtNow,//操作时间
                                                                eTicketStatusID = 1, //1：正常状态 2：换开  3：退票
                                                                invoiceStatusID = 1, //1：未开发票 2：发票已回收 
                                                            };
                                                            myModel.B_ETicket.Add(saveETicket);
                                                            myModel.SaveChanges();

                                                            //2.4-获取电子客票保存后的主键ID
                                                            int saveETicketID = saveETicket.ETicketID;
                                                            //判断电子客票ID是否大于0
                                                            if (saveETicketID > 0)
                                                            {
                                                                ////2.5-PNR出票组信息(B_PNRTicketing)
                                                                B_PNRTicketing savePNRTicketing = new B_PNRTicketing()
                                                                {
                                                                    PNRID = order.PNRID, //PNRID 
                                                                    PNRPassengerID = passenger.PNRPassengerID, //PNR旅客ID
                                                                    PNRSegmentID = flightCabinInfo.PNRSegmentID, //PNR航段ID
                                                                    ETicketID = saveETicketID, //电子客票ID
                                                                    TicketingTime = dtNow, //出票时间
                                                                    orderID = saveOrderID, //订单ID
                                                                };
                                                                myModel.B_PNRTicketing.Add(savePNRTicketing);
                                                                if (myModel.SaveChanges() > 0)
                                                                {

                                                                }
                                                                else
                                                                {
                                                                    msg.Text = "PNR出票组信息保存失败,出票失败";
                                                                    return Json(msg, JsonRequestBehavior.AllowGet);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                msg.Text = "电子客票保存失败,出票失败";
                                                                return Json(msg, JsonRequestBehavior.AllowGet);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            msg.Text = "电子票号获取失败,无法出票";
                                                            return Json(msg, JsonRequestBehavior.AllowGet);
                                                        };
                                                    }
                                                }
                                                // 3 - 修改PNR状态为3 全部出票
                                                dbPNR.PNRStatus = 3;//全部出票
                                                myModel.Entry(dbPNR).State = System.Data.Entity.EntityState.Modified;//保存修改状态
                                                if (myModel.SaveChanges() > 0)
                                                {
                                                    //4-修改航段出票信息（状态）为 3：全部出票
                                                    //4.1-查询出PNR对应的航段信息
                                                    List<B_PNRSegment> dbPNRSegments = myModel.B_PNRSegment
                                                        .Where(o => o.PNRID == order.PNRID)
                                                        .ToList();
                                                    //4.2-遍历修改PNR航段
                                                    foreach (B_PNRSegment dbPNRSegment in dbPNRSegments)
                                                    {
                                                        //修改订座情况 为 2:已经出票
                                                        dbPNRSegment.bookSeatInfo = 2;
                                                        myModel.Entry(dbPNRSegment).State = System.Data.Entity.EntityState.Modified;
                                                        if (myModel.SaveChanges() <1)
                                                        {
                                                            msg.Text= "PNR航段订座情况更新失败,出票失败";
                                                            return Json(msg, JsonRequestBehavior.AllowGet);
                                                        }
                                                    }
                                                    //5-更新用户的票号信息
                                                    //5.1-变遍历票号信息
                                                    foreach (TicketInfo ticketInfo in ticketInfos)
                                                    {
                                                        //5.2-判断该票号信息是否有使用-有使用才需要更新
                                                        if (ticketInfo.useNum > 0)
                                                        {
                                                            //获取出需要更新的S_Ticket对象
                                                            S_Ticket saveTicket = ticketInfo.ticket;
                                                            //更新saveTicket的当前票号
                                                            saveTicket.currentTicketNo = ticketInfo.currentTicketNo;
                                                            myModel.Entry(saveTicket).State = System.Data.Entity.EntityState.Modified;
                                                            if (myModel.SaveChanges() < 1)
                                                            {
                                                                msg.Text = "PNR航段订座情况更新失败,出票失败";
                                                                return Json(msg, JsonRequestBehavior.AllowGet);
                                                            }
                                                        }
                                                    }
                                                    //6-更新当前用户的虚拟账户信息--- 虚拟账户扣除相应的交易余额
                                                    dbVirtualAccount.accountBalance = dbVirtualAccount.accountBalance - order.payMoney;
                                                    myModel.Entry(dbVirtualAccount).State = System.Data.Entity.EntityState.Modified;
                                                    if (myModel.SaveChanges() > 0)
                                                    {
                                                        //7-虚拟账户的交易记录-添加一条记录信息
                                                        B_TransactionRecord saveTransactionRecord = new B_TransactionRecord()
                                                        {
                                                            virtualAccountID = dbVirtualAccount.virtualAccountID,//虚拟账户ID
                                                            transactionMoney = order.payMoney,//交易金额
                                                            transactionType = 1,//类型:1 支出
                                                            transactionTime = dtNow,//交易时间
                                                            userID = loginUserID,//用户ID
                                                        };
                                                        myModel.B_TransactionRecord.Add(saveTransactionRecord);
                                                        if (myModel.SaveChanges() > 0)
                                                        {
                                                            //8提交事务
                                                            scope.Complete();
                                                            //返回成功提示
                                                            msg.State = true;
                                                            msg.Text = "出票成功";
                                                            msg.Object = saveOrderID;//把订单Id返回
                                                        }
                                                        else
                                                        {
                                                            msg.Text = "交易记录失败,出票失败";
                                                        }
                                                    }
                                                    else
                                                    {
                                                        msg.Text = "虚拟账户扣款失败,出票失败";
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                msg.Text = "订单保存失败";
                                            }
                                        }
                                        else
                                            {
                                                msg.Text = "您没有足够的电子客票,无法出票";
                                            }
                                        
                                    }
                                    else
                                    {
                                        msg.Text = "PNR下没有需要出票的航段信息,无法出票";
                                    }
                                }
                                else
                                {
                                    msg.Text = "PNR下没有需要出票的旅客,无法出票";
                                }
                            }
                            else
                            {
                                msg.Text = "账户余额不足";
                            }
                        }
                        else
                        {
                            if (dbPNR.PNRStatus == 2)
                            {
                                msg.Text = string.Format("PNR 【{0}】已经部分出票,不能再次出票;", dbPNR.PNRNo);
                            }
                            else if (dbPNR.PNRStatus == 3)
                            {
                                msg.Text = string.Format("PNR 【{0}】已经全部出票,不能再次出票操作;", dbPNR.PNRNo);
                            }
                            else//(dbPNR.PNRStatus == 0)
                            {
                                msg.Text = string.Format("PNR 【{0}】已经被取消,不能出票;", dbPNR.PNRNo);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        msg.Text = "出票失败";
                    }
                }
            }
            else
            {
                msg.Text = "数据异常,非法操作";
            }
            
            
            
            return Json(msg, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region PNR出票结果页面 

        public ActionResult IssuanceTicketResult(int orderID)
        {
            try
            {
                //查询订单信息
                OrderVo order = (from tabOrder in myModel.B_Order
                                 join tabPNR in myModel.B_PNR
                                 on tabOrder.PNRID equals tabPNR.PNRID
                                 where tabOrder.orderID == orderID
                                 select new OrderVo()
                                 {
                                     orderID = tabOrder.orderID,//订单ID
                                     orderNo = tabOrder.orderNo,//订单编号
                                     PNRNo = tabPNR.PNRNo,//PNR 编号
                                 }).Single();

                //查询该订单ID的电子客票信息
                List<ETicketVo> eTickets = (from tabETicket in myModel.B_ETicket
                                            join tabOrder in myModel.B_Order
                                              on tabETicket.oderID equals tabOrder.orderID
                                            join tabPNRPassenger in myModel.B_PNRPassenger
                                              on tabETicket.PNRPassengerID equals tabPNRPassenger.PNRPassengerID
                                            where tabOrder.orderID == orderID
                                            select new ETicketVo()
                                            {
                                                passengerNo = tabPNRPassenger.passengerNo.Value,//旅客编号
                                                passengerName = tabPNRPassenger.passengerName,//旅客姓名
                                                ticketPrice = tabOrder.totalPrice,//票价
                                                ticketNo = tabETicket.ticketNo,//电子客票号
                                            }).ToList();

                //传递参数到页面
                ViewBag.order = order;
                ViewBag.eTickets = eTickets;

                return View();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return RedirectToAction("Index");
            }
        }


        #endregion

        #region PNR查询中"添加航段"航段数为4时只显示联程按钮把前四段的设置为只读按钮

        public ActionResult FourSegment(int pnrID)
        {
            List<B_PNRSegment> dbPNRSegments = myModel.B_PNRSegment
                                                        .Where(o => o.PNRID == pnrID)
                                                        .ToList();
            ViewBag.Segment = dbPNRSegments;
            return View();
        }

        #endregion
    }
}