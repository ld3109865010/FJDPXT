using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FJDPXT.Model;

namespace FJDPXT.Areas.LD
{
    public class LDController : Controller
    {
        // GET: LD/LD

        FJDPXTEntities1 myModel = new FJDPXTEntities1();
        public ActionResult Index()
        {
            try
            {
                //var studentInFor = from tbStudent in myModel.S_Student
                //var studentInFor=myModel.S_Student.single(o=>o.)
            }
            catch (Exception e)
            {
                Console.Write(e);

            }




            return View();
        }
        
    }
}