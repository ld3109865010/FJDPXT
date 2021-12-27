using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FJDPXT.Areas.LD.Controllers
{
    public class DefaultController : Controller
    {
        // GET: LD/Default

        public ActionResult Index()
        {
            return View();
            int item = 0;
            for (int a = 0; a < 100; a++)
            {
                for (int b = 0; b < 100; b++)
                {
                    for (int c = 0; c < 100; c++)
                    {
                        if (920 * a == 68 * b + 9 * c + 1)
                        {

                            Console.Write(a);
                            Console.Write(b);
                            Console.Write(c);
                            item++;
                        }
                    }
                }
            }
        }
    }
}


