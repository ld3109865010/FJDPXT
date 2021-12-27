using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FJDPXT.Model;
using FJDPXT.EntityClass;

namespace FJDPXT.Areas.PNR.Controllers
{
    public class StrokeDisplayController : Controller
    {
        // GET: PNR/StrokeDisplay
        FJDPXTEntities1 myModel = new FJDPXTEntities1();
        public ActionResult StrokeDisplay()
        {
            return View();
        }
        public ActionResult SelectPNRIDByNo(string PNRNo)
        {
            ReturnJson rtMsg = new ReturnJson();

            try
            {
                //将输入的PNR编号中的字母全转化为大写
                PNRNo = PNRNo.Trim().ToUpper();
                //查询PNR状态,PNR取消以及未出票的PNR不能打印
                B_PNR pnr = myModel.B_PNR.Single(p => p.PNRNo == PNRNo);
                if (pnr.PNRStatus == 2 || pnr.PNRStatus == 3)
                {
                    rtMsg.State = true;
                    rtMsg.Object = pnr.PNRID;
                }
                else if (pnr.PNRStatus == 0)
                {
                    rtMsg.Text = "该PNR已经被取消！无法打印行程单";
                }
                else
                {
                    rtMsg.Text = "该PNR还未出票！无法打印行程单";
                }
            }
            catch (Exception e)
            {
                Console.Write(e);
                rtMsg.Text = "输入的PNRNo不存在，请检查！";
            }

            return Json(rtMsg, JsonRequestBehavior.AllowGet);

        }
        /// <summary>
        /// 行程显示页面
        /// </summary>
        /// <param name="PNRID"></param>
        /// <returns></returns>
        public ActionResult checkFlight(int PNRID)
        {
            try
            {
                //查询PNR
                B_PNR pnr = myModel.B_PNR.Single(p => p.PNRID == PNRID);
                //查询旅客列表
                List<B_PNRPassenger> listPassenger = (from tbpassenger in myModel.B_PNRPassenger
                                                      where tbpassenger.PNRID == PNRID
                                                      select tbpassenger).ToList();
                //将数据传递到页面
                ViewBag.pnr = pnr;
                ViewBag.listPassenger = listPassenger;
                return View();
            }
            catch (Exception e)
            {
                Console.Write(e);
                return RedirectToAction("StrokeDisplay");
            }
        }

        /// <summary>
        /// 根据旅客ID查询对应票号信息
        /// </summary>
        /// <param name="PNRPassengerID"></param>
        /// <returns></returns>
        public ActionResult selectPassengerETicket(int PNRPassengerID)
        {
            //查询对应的客票信息
            List<PNRTicketingVo> listPNRTicketing=(from tbPNRTicketing in myModel.B_PNRTicketing
                                                   join tbETicketing in myModel.B_ETicket on tbPNRTicketing.ETicketID equals tbETicketing.ETicketID
                                                   join tbPNRPassenger in myModel.B_PNRPassenger on tbPNRTicketing.PNRPassengerID equals tbPNRPassenger.PNRPassengerID
                                                   join tbSegment in myModel.B_PNRSegment on tbPNRTicketing.PNRSegmentID equals tbSegment.PNRSegmentID
                                                   join tbFlight in myModel.S_Flight on tbSegment.flightID equals tbFlight.flightID
                                                   join tbOrange in myModel.S_Airport on tbFlight.orangeID equals tbOrange.airportID
                                                   join tbDestination in myModel.S_Airport on tbFlight.destinationID equals tbDestination.airportID
                                                   join tbCabinTpe in myModel.S_CabinType on tbSegment.cabinTypeID equals tbCabinTpe.cabinTypeID
                                                   where tbPNRPassenger.PNRPassengerID == PNRPassengerID
                                                   select new PNRTicketingVo
                                                   {
                                                       ticketNo = tbETicketing.ticketNo,//票号
                                                       cabinTypeCode = tbCabinTpe.cabinTypeCode,//舱位等级
                                                       flightCode = tbFlight.flightCode,//航班编号
                                                       flightDate = tbFlight.flightDate.Value,//航班日期
                                                       orangePinyin = tbOrange.pinyinName.ToUpper(),//起飞城市拼音
                                                       orangeCity = tbOrange.cityName,//起飞城市
                                                       orangeCode = tbOrange.airportCode,//起飞机场编码
                                                       departureTime = tbFlight.departureTime.Value,//起飞时间
                                                       destinationPinyin = tbDestination.pinyinName.ToUpper(),//达到城市拼音
                                                       destinationCity = tbDestination.cityName,//到达城市
                                                       destinationCode = tbDestination.airportCode,//到达机场编码
                                                       arrivalTime = tbFlight.arrivalTime.Value//到达时间
                                                   }).ToList();
            
            return Json(listPNRTicketing, JsonRequestBehavior.AllowGet);
        }
    }
}