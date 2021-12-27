using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FJDPXT.Common;
using FJDPXT.EntityClass;
using FJDPXT.Model;


namespace FJDPXT.Controllers
{
    
    public class MainController : Controller
    {
        // GET: Main

        //实例化model
         FJDPXTEntities1 myModel = new FJDPXTEntities1();
       //登录页面
        public ActionResult Login()
        {
            string jobNumber = "";
            string password = "";
            bool rememberMe = false;

            //获取浏览器携带cookie  System系统   Request请求
            HttpCookie cookie = System.Web.HttpContext.Current.Request.Cookies["user"];
            if (cookie != null)
            {
                if (cookie["jobNumber"] != null)
                {
                    //给加密后的密码解码再返回给jobNumber
                    jobNumber = System.Web.HttpContext.Current.Server.UrlDecode(cookie["jobNumber"]);
                }
                if (cookie["password"] != null)
                {
                    password = System.Web.HttpContext.Current.Server.UrlDecode(cookie["password"]);
                }
                rememberMe = true;
            }

            //返回数据到页面
            ViewBag.jobNumber = jobNumber;
            ViewBag.password = password;
            ViewBag.rememberMe = rememberMe;
            return View();
        }
        //主页面
       
        //生成验证码和验证图片
        public ActionResult CreateValidImage()
        {
            //1-生成长度为5的随机字符串作为验证码
            string validCode = ValidCodeUtils.GetRandomCode(5);
            //2-根据生成的验证码字符串生成验证图片
            byte[] validImage = ValidCodeUtils.CreateImage(validCode);
            //3-将生成的验证码字符串保存到Session
            Session["validCode"] = validCode;
            //4-将验证图片返回到页面
            return File(validImage,"image/jpeg");
        }
        public ActionResult UserLogin(string jobNumber,string userPassword,
            string validCode, bool rememberMe)
        {
            //准备返回的数据
            ReturnJson msg = new ReturnJson();
            msg.State = false;//设置默认的状态 可以省略不写：bool类型的数据 默认值就是false

            //===1-检查用户输入的验证码是否正确
            //==1.1-从session中获取出保存的验证码字符串
            string sessionValidCode = "";
            if (Session["validCode"] != null)
            {
                sessionValidCode = Session["validCode"].ToString();
            }

            //==1.2-验证用户输入的验证码和session中的验证码是是否相同 忽略大小写
            validCode = validCode == null ? "" : validCode.Trim();
            //if (sessionValidCode.Equals(validCode, StringComparison.InvariantCultureIgnoreCase))
            //{
                //===2-验证用户的工号和密码是否匹配
                //==2.1-查询登录的用户数据 Linq
                try
                {
                    S_User dbUser = (from tabUser in myModel.S_User
                                     where tabUser.jobNumber == jobNumber
                                     select tabUser).Single();
                //==2.2-判断用户输入的密码和数据库中的密码是否相同 
                //=2.3-对用户输入的密码进行AES加密
                userPassword = AESEncryptHelper.Encrypt(userPassword);
                    //=2.4-将加密后的数据和数据库中查询的用户密码比较
                    if (userPassword == dbUser.userPassword)
                    {
                        int loginUserID = dbUser.userID;
                        List<ModuleVo> userModules = (from tabModule in myModel.S_Module
                                                  join tabP in myModel.S_Permission
                                                   on tabModule.moduleID equals tabP.moduleID
                                                  join tabUseType in myModel.S_UserType
                                                   on tabP.UserTypeID equals tabUseType.userTypeID
                                                  join tabUser in myModel.S_User
                                                   on tabUseType.userTypeID equals tabUser.userTypeID
                                                  where tabUser.userID == loginUserID
                                                  select new ModuleVo
                                                  {
                                                      moduleID = tabModule.moduleID,
                                                      moduleName = tabModule.moduleName,
                                                      moduleDescrible = tabModule.moduleDescrible,
                                                      moduleFarID = tabModule.moduleFarID,
                                                      blFun = tabModule.blFun,
                                                      parentModule = (from tabModuleF in myModel.S_Module
                                                                      where tabModuleF.moduleID == tabModule.moduleFarID
                                                                      select tabModuleF).FirstOrDefault()
                                                  }).ToList();
                        //===3-保存用户信息到session,用户ID,用户工号
                        Session["userID"] = dbUser.userID;
                        Session["jobNumber"] = dbUser.jobNumber;
                        Session["userModules"] = userModules;
                    //===4-处理记住我这个功能 使用cookie实现
                    //==4.1-判断是否勾选"记住我"
                    if (rememberMe == true)
                        {   //勾选
                            //实例cookie
                            HttpCookie cookie = new HttpCookie("user");
                            //保存数据到cookie
                            cookie["jobNumber"] = jobNumber;
                            cookie["Password"] = userPassword;
                            // 设置cookie的有效期 7天
                            cookie.Expires = DateTime.Now.AddDays(7);
                            //通过Response把cookie返回给浏览器
                            Response.Cookies.Add(cookie);
                        }
                        else
                        {
                            //未勾选 删除cookie
                            //实例cookie
                            HttpCookie cookie = new HttpCookie("user");
                            //设置cookie的有效期为昨天使其失效来达到remember=false的功能
                            cookie.Expires = DateTime.Now.AddDays(-1);
                            //通过Response把cookie返回给浏览器
                            Response.Cookies.Add(cookie);
                        }


                        msg.State = true;
                        msg.Text = "登录成功";
                    }
                    else
                    {
                        msg.Text = "登录失败";
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);//避免变量e没有使用的警告
                    msg.Text = "该用户不存在";
                }
            //}
            //else
            //{
            //     msg.Text = "验证码不正确,请重新输入";
            //    //msg.alert = "验证码不正确,请重新输入";
            //}





            //返回json格式的数据
            return Json(msg,JsonRequestBehavior.AllowGet);
        }
        public ActionResult Main()
        {
            try
            {
                int userID = Convert.ToInt32(Session["userID"].ToString());
                string jobNumber = Session["jobNumber"].ToString();

                //传递数据到页面
                //ViewData["jobNumber"] = jobNumber;
                ViewBag.jobNumber = jobNumber;


                return View();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                //重定向
                //重定向到登录页面
                //return Redirect("https://www.baidu.com");\
                //重定向有俩种方法
                //方法①return Redirect(Url.Content("~/Main/Login"));
                return RedirectToAction("Login");
            }

        }
        public ActionResult loginOut()
        {
            //清楚本次会话中所形成的数据
            Session.Clear();

            //return Json(true, JsonRequestBehavior.AllowGet);
            return null;
        }

        #region 查询模块权限

        public ActionResult SelectModularJurisdiction()
        {
            if (Session["userID"] != null)
            {
                int userId = Convert.ToInt32(Session["userID"]);
                //读取权限信息
                var tempModulars = from tabPermission in myModel.S_Permission
                                   join tabModule in myModel.S_Module
                                    on tabPermission.moduleID equals tabModule.moduleID
                                   join tabUser in myModel.S_User
                                    on tabPermission.UserTypeID equals tabUser.userTypeID
                                   where tabUser.userID == userId && tabModule.blFun == false
                                   select new
                                   {
                                       moduleID = tabModule.moduleID,//模块ID
                                       Name = tabModule.moduleDescrible.Trim()//模块描述
                                   };
                //外连接(左连接)
                var userModulars = (from tbSysModular in myModel.S_Module
                                    join tbTempModulars in tempModulars
                                    on tbSysModular.moduleID equals tbTempModulars.moduleID
                                    into temp
                                    select new
                                    {
                                        ModularID = tbSysModular.moduleID,//模块ID
                                        ModularName = tbSysModular.moduleDescrible.Trim(),//模块名称
                                        ID = temp.FirstOrDefault() != null ? temp.FirstOrDefault().moduleID : 0//有该模块的权限 ID>0 没有权限ID=0
                                    }).ToList();
                return Json(userModulars, JsonRequestBehavior.AllowGet);
            }
            else {
                return Json("", JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region 无权限页面

        public ActionResult NoPermission()
        {
            return View();
        }

        #endregion

    }
}