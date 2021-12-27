using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FJDPXT.Model;
using FJDPXT.EntityClass;
using FJDPXT.Common;

namespace FJDPXT.Areas.OthersMaintenance.Controllers
{
    public class ModifyPasswordController : Controller
    {
        // GET: OthersMaintenance/ModifyPassword
        //其他-->密码修改
        FJDPXTEntities1 myModel = new FJDPXTEntities1();

        private int loginUserID = 0;

        protected override/*重写*/ void OnActionExecuting/*在执行Action方法前执行该方法*/(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            try
            {
                loginUserID = Convert.ToInt32(Session["userID"].ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                filterContext.Result = Redirect(Url.Content("~/Main/Login"));//重定向到登录界面
            }
        }
        public ActionResult Index()
        {
            return View();
        }

        //检查用户输入的原密码
        public ActionResult CheckedOldPassword(string oldPassword)
        {
            ReturnJson msg = new ReturnJson();

            try
            {
                //根据登录的用户ID查询用户数据
                S_User dbUser = myModel.S_User.Single(o => o.userID == loginUserID);
                //将用户输入的原密码加密后和数据库的密码对比
                oldPassword = AESEncryptHelper.Encrypt(oldPassword);
                if (oldPassword == dbUser.userPassword)
                {
                    msg.State = true;
                    msg.Text = "密码正确";
                }
                else
                {
                    msg.Text = "密码错误";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                msg.Text = "密码错误";
            }


            return Json(msg, JsonRequestBehavior.AllowGet);

        }
        //修改密码
        public ActionResult UpdatePassword(string oldPassword, string newPassword)
        {
            ReturnJson msg = new ReturnJson();

            try
            {
                //根据登录的用户ID查询用户数据
                S_User dbUser = myModel.S_User.Single(o => o.userID == loginUserID);
                //将用户输入的原密码加密后和数据库的密码对比
                oldPassword = AESEncryptHelper.Encrypt(oldPassword);
                if (oldPassword == dbUser.userPassword)
                {
                    //修改密码 对新密码加密 并赋值给用户对象
                    dbUser.userPassword = AESEncryptHelper.Encrypt(newPassword);
                    myModel.Entry(dbUser).State = System.Data.Entity.EntityState.Modified;
                    if (myModel.SaveChanges() > 0)
                    {
                        msg.State = true;
                        msg.Text = "修改成功";

                        //清空session数据 强制重新登录
                        Session.Clear();
                    }
                    else
                    {
                        msg.Text = "修改失败";
                    }
                }
                else
                {
                    msg.Text = "原密码错误";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                msg.Text = "密码错误";
            }

            return Json(msg, JsonRequestBehavior.AllowGet);
        }







    }
}