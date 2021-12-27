using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FJDPXT.Model;
using FJDPXT.EntityClass;

namespace FJDPXT.Areas.StatisticalManagement.Controllers
{
    public class PaymentStatisticsController : Controller
    {
        // GET: StatisticalManagement/PaymentStatistics
        //统计功能-->支付统计
        //实例化myModel
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

        public ActionResult SelectPaymentStatistics(LayuiTablePage layuiTablePage,string UserGroup,string StartTime,string EndTime)
        {
            var list = (from tbEticket in myModel.B_ETicket
                        join tbPassenger in myModel.B_PNRPassenger on tbEticket.PNRPassengerID equals tbPassenger.PNRPassengerID
                        join tbUser in myModel.S_User on tbEticket.operatorID equals tbUser.userID
                        join tbUserGroup in myModel.S_UserGroup on tbUser.userGroupID equals tbUserGroup.userGroupID
                        join tbFlight in myModel.S_Flight on tbEticket.flightID equals tbFlight.flightID
                        join tbCabinType in myModel.S_CabinType on tbEticket.cabinTypeID equals tbCabinType.cabinTypeID
                        join orangeCity in myModel.S_Airport on tbFlight.orangeID equals orangeCity.airportID
                        join arriveCity in myModel.S_Airport on tbFlight.destinationID equals arriveCity.airportID
                        join tbCertificatesType in myModel.S_CertificatesType on tbPassenger.certificatesTypeID equals tbCertificatesType.certificatesTypeID
                        select new PaymentStatisticsVo
                        {
                            passengerName = tbPassenger.passengerName,
                            userGroupNumber = tbUserGroup.userGroupNumber,
                            flightCode = tbFlight.flightCode,
                            flightDate = tbFlight.flightDate.ToString(),
                            strDepartureTime = tbFlight.departureTime.ToString(),
                            orangeCityName = orangeCity.cityName,
                            destinationCityName = arriveCity.cityName,
                            cabinTypeCode = tbCabinType.cabinTypeCode,
                            certificatesType = tbCertificatesType.certificatesType,
                            certificatesCode = tbPassenger.certificatesCode,
                            strOperatingTtime = tbEticket.tickingTime.ToString(),
                            operTime = tbEticket.tickingTime
                        }).ToList();
            //条件筛选
            if (!string.IsNullOrEmpty(UserGroup))
            {
                list = list.Where(m => m.userGroupNumber.Equals(UserGroup)).ToList();
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
            //计算总条数
            int TotalRow = list.Count();
            //分页
            List<PaymentStatisticsVo> listPayment =list.OrderBy(m=>m.operTime)
                                                  .Skip(layuiTablePage.GetStartIndex())
                                                  .Take(layuiTablePage.limit)
                                                  .ToList();

            LayuiTableData<PaymentStatisticsVo> layuiTableData = new LayuiTableData<PaymentStatisticsVo>
            {
                data = listPayment,
                count = TotalRow
            };

            return Json(layuiTableData, JsonRequestBehavior.AllowGet);
        }
    }
}