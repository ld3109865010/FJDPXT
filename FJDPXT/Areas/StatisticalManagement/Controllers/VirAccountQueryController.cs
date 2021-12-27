using FJDPXT.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FJDPXT.EntityClass;

namespace FJDPXT.Areas.StatisticalManagement.Controllers
{
    public class VirAccountQueryController : Controller
    {
        // GET: StatisticalManagement/VirAccountQuery
        //统计功能-->运输数据查询统计
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
        //实例化myModel
        FJDPXTEntities1 myModel = new FJDPXTEntities1();
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult SelectVirAccount(LayuiTablePage layuiTablePage, string UserGroup, string StartTime, string EndTime)
        {
            var list = (from tbETicket in myModel.B_ETicket
                        join tbPNRPassenger in myModel.B_PNRPassenger on tbETicket.PNRPassengerID equals tbPNRPassenger.PNRPassengerID
                        join tbCertificatesType in myModel.S_CertificatesType on tbPNRPassenger.certificatesTypeID equals tbCertificatesType.certificatesTypeID
                        join tbCabinType in myModel.S_CabinType on tbETicket.cabinTypeID equals tbCabinType.cabinTypeID
                        join tbUser in myModel.S_User on tbETicket.operatorID equals tbUser.userID
                        join tbUserGroup in myModel.S_UserGroup on tbUser.userGroupID equals tbUserGroup.userGroupID
                        join tbFlight in myModel.S_Flight on tbETicket.flightID equals tbFlight.flightID
                        join tbOrange in myModel.S_Airport on tbFlight.orangeID equals tbOrange.airportID
                        join tbDestination in myModel.S_Airport on tbFlight.destinationID equals tbDestination.airportID
                        select new PaymentStatisticsVo
                        {
                            passengerName = tbPNRPassenger.passengerName,
                            userGroupNumber = tbUserGroup.userGroupNumber,
                            flightCode = tbFlight.flightCode,
                            flightDate = tbFlight.flightDate.ToString(),
                            strDepartureTime = tbFlight.departureTime.ToString(),
                            orangeCityName = tbOrange.cityName,
                            destinationCityName = tbDestination.cityName,
                            cabinTypeCode = tbCabinType.cabinTypeCode,
                            certificatesType = tbCertificatesType.certificatesType,
                            certificatesCode = tbPNRPassenger.certificatesCode,
                            strOperatingTtime = tbETicket.tickingTime.ToString(),
                            operTime = tbETicket.tickingTime
                        }).ToList();
            if (!string.IsNullOrEmpty(UserGroup))
            {
                list = list.Where(m => m.userGroupNumber == UserGroup).ToList();
            }
            if (!string.IsNullOrEmpty(StartTime))
            {
                var startTime = Convert.ToDateTime(StartTime);
                list = list.Where(m => m.operTime >= startTime).ToList();
            }
            if (!string.IsNullOrEmpty(EndTime))
            {
                var endTime = Convert.ToDateTime(EndTime);
                list = list.Where(m => m.operTime <= endTime).ToList();
            }
            //分页查询机场数据
            List<PaymentStatisticsVo> listPayment = list.OrderBy(m => m.operTime)
                                                    .Skip(layuiTablePage.GetStartIndex())
                                                    .Take(layuiTablePage.limit)
                                                    .ToList();
            //计算总条数
            int TotalRow = list.Count();
            LayuiTableData<PaymentStatisticsVo> layuiTableData = new LayuiTableData<PaymentStatisticsVo>
            {
                count = TotalRow,
                data = listPayment
            };
            return Json(layuiTableData, JsonRequestBehavior.AllowGet);
        }
    }
}