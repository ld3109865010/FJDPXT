using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FJDPXT.EntityClass;
using FJDPXT.Model;

namespace FJDPXT.Areas.StatisticalManagement
{
    public class SalesReportStatisticsController : Controller
    {
        // GET: StatisticalManagement/SalesReportStatistics
        //统计功能-->销售报告统计
        //实例化myModel

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

        FJDPXTEntities1 myModel = new FJDPXTEntities1();

        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 查询销售报告统计数据
        /// </summary>
        /// <param name="layuiTablePage">分页实体模型</param>
        /// <param name="userGroup">用户组</param>
        /// <param name="departureCity">出发城市</param>
        /// <param name="arrivalCity">到达城市</param>
        /// <returns></returns>
        public ActionResult SelectSalesReport(LayuiTablePage layuiTablePage, string userGroup, string departureCity, string arrivalCity)
        {
            var list= (from tbETicket in myModel.B_ETicket
                       join tbUser in myModel.S_User on tbETicket.operatorID equals tbUser.userID
                       join tbUserGroup in myModel.S_UserGroup on tbUser.userGroupID equals tbUserGroup.userGroupID
                       join tbCabinType in myModel.S_CabinType on tbETicket.cabinTypeID equals tbCabinType.cabinTypeID
                       join tbPNRPassenger in myModel.B_PNRPassenger on tbETicket.PNRPassengerID equals tbPNRPassenger.PNRPassengerID
                       join tbCertificatesType in myModel.S_CertificatesType on tbPNRPassenger.certificatesTypeID equals tbCertificatesType.certificatesTypeID
                       join tbFlight in myModel.S_Flight on tbETicket.flightID equals tbFlight.flightID
                       join tbOrange in myModel.S_Airport on tbFlight.orangeID equals tbOrange.airportID
                       join tbDestination in myModel.S_Airport on tbFlight.destinationID equals tbDestination.airportID
                       select new SalesReportStatisticsVo
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
                       }).ToList();

            //条件筛选
            if (!string.IsNullOrEmpty(userGroup))
            {
                list = list.Where(m => m.userGroupNumber == userGroup).ToList();
            }
            if (!string.IsNullOrEmpty(departureCity))
            {
                list = list.Where(m => m.orangeCityName.Contains(departureCity)).ToList();
            }
            if (!string.IsNullOrEmpty(arrivalCity))
            {
                list = list.Where(m => m.destinationCityName.Contains(arrivalCity)).ToList();
            }
            //分页查询机场数据
            List<SalesReportStatisticsVo> listPayment = list.OrderBy(m => m.flightDate)
                                                        .Skip(layuiTablePage.GetStartIndex())
                                                        .Take(layuiTablePage.limit)
                                                        .ToList();
            //机场数据总条数
            int TotalRow = list.Count;
            LayuiTableData<SalesReportStatisticsVo> layuiTableData = new LayuiTableData<SalesReportStatisticsVo>()
            {
                data = listPayment,
                count = TotalRow
            };

            return Json(layuiTableData, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ExportSalesData(string userGroup,string departureCity,string arrivalCity)
        {
            try
            {






                return null;
            }
            catch (Exception e)
            {
                Console.Write(e);
                return Json("数据异常", JsonRequestBehavior.AllowGet);
            }


        }
    }
}