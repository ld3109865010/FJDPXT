using System.Web.Mvc;

namespace FJDPXT.Areas.LD
{
    public class LDAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "LD";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "LD_default",
                "LD/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}