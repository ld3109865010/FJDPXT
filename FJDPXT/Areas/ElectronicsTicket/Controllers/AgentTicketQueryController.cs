using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FJDPXT.EntityClass;
using FJDPXT.Model;

namespace FJDPXT.Areas.ElectronicsTicket.Controllers
{
    public class AgentTicketQueryController : Controller
    {
        // GET: ElectronicsTicket/AgentTicketQuery
        //电子票证-->代理人票证查询
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
            //查询所有的城市机场信息
            List<S_Airport> airports = (from tbAirport in myModel.S_Airport
                                        select tbAirport).ToList();
            //查询所有的证件类型
            List<S_CertificatesType> CertificatesTypes = (from tbCertificatesType in myModel.S_CertificatesType
                                                          select tbCertificatesType).ToList();

            //传递到页面
            ViewBag.airports = airports;
            ViewBag.CertificatesTypes = CertificatesTypes;

            return View();
        }

        
    }
}