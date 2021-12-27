using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FJDPXT.Model;
using FJDPXT.EntityClass;
using FJDPXT.Common;
using System.Transactions;
using System.Text.RegularExpressions;

namespace FJDPXT.Areas.OthersMaintenance.Controllers
{
    
    public class OpenOffNumberController : Controller
    {
        // GET: OthersMaintenance/OpenOffNumber
        //其他-->开通工号
        FJDPXTEntities1 myModel = new FJDPXTEntities1();    
        public ActionResult Index()
        {
            try
            {
                int useID = Convert.ToInt32(Session["userID"].ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Redirect(Url.Content("~/Main/Login"));
            }
            return View();
        }
        //用户组
        public ActionResult SelectUserGroupForSelect()
        {
            List<SelectVo> list = (from tabUserGroup in myModel.S_UserGroup
                                      select new SelectVo()
                                      {
                                          id = tabUserGroup.userGroupID,
                                          text=tabUserGroup.userGroupNumber
                                      }).ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        //用户角色
        public ActionResult SelectUserTypeForSelect()
        {
            List<SelectVo> list = (from tabUserType in myModel.S_UserType
                                      select new SelectVo() {
                                          id=tabUserType.userTypeID,
                                          text=tabUserType.userType
                                      }).ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public ActionResult InsertUser(S_User user,HttpPostedFileBase userPicture)
        {
            ReturnJson msg = new ReturnJson();

            //数据验证
            //用户组用户角色
            if( user.userGroupID>0 && user.userTypeID > 0)
            {
                //工号                         
                if (!string.IsNullOrEmpty(user.jobNumber) && Regex.IsMatch(user.jobNumber, "^[A-Za-z0-9]{3,10}$"))
                {
                    

                    //用户姓名
                    if (!string.IsNullOrEmpty(user.userName))
                    {
                        //余额
                        if (user.amount != null && user.amount >= 0)
                        {
                            //邮箱
                            if (!string.IsNullOrEmpty(user.userEmail) && Regex.IsMatch(user.userEmail,
                                "^([a-zA-Z]|[0-9])(\\w|\\-)+@[a-zA-Z0-9]+\\.([a-zA-Z]{2,4})$"))
                            {
                                int oldCount = myModel.S_User.Count(o => o.jobNumber == user.jobNumber);
                                if (oldCount == 0)
                                {
                                    using (TransactionScope scope = new TransactionScope())
                                    {
                                        try
                                        {
                                            //保存用户头像
                                            //==检查存放用户头像的目录是否存在
                                            if (!System.IO.Directory.Exists(Server.MapPath("~/Document/userPicture/")))
                                            {
                                                System.IO.Directory.CreateDirectory(Server.MapPath("~/Document/userPicture/"));
                                            }
                                            //判断是否上传了图片
                                            if (userPicture != null && userPicture.ContentLength > 0)
                                            {
                                                //获取文件的扩展名称
                                                string imgExtension = System.IO.Path.GetExtension(userPicture.FileName);
                                                //拼接要保存的文件名称
                                                string fileName = DateTime.Now.ToString("yyyyMMddHHmmssffff") + "_" + Guid.NewGuid() + imgExtension;
                                                //拼接文件保存的路径 (完整路径)
                                                string filePath = Server.MapPath("~/Document/userPicture/") + fileName;

                                                //保存上传的文件到硬盘 (保存完整的路径)
                                                userPicture.SaveAs(filePath);

                                                //文件名称保存到user对象 (存文件名称 在其他电脑中可以用相对路径Server.MapPath来获取 兼容性更好)
                                                user.userPicture = fileName;
                                            }
                                            else {
                                                msg.Text = "图片上传失败";
                                            }



                                            //==先保存 用户数据
                                            //-对用户密码进行加密
                                            user.userPassword = AESEncryptHelper.Encrypt(user.userPassword);
                                            myModel.S_User.Add(user);
                                            if (myModel.SaveChanges() > 0)
                                            {
                                                //--用户数据保存成功
                                                //-获取保存后用户的ID -- LinQ新增数据后会将新增后数据的主键ID设置到保存所使用的对象中                                                                    
                                                int userID = user.userID;

                                                //===再保存 虚拟账户数据
                                                S_VirtualAccount virtualAccount = new S_VirtualAccount();
                                                virtualAccount.userID = userID;//设置用户ID
                                                virtualAccount.accountBalance = user.amount;//设置账户余额
                                                virtualAccount.account = string.Format("XNZH{0:000000000}", userID);//设置虚拟账户
                                                myModel.S_VirtualAccount.Add(virtualAccount);

                                                if (myModel.SaveChanges() > 0)
                                                {
                                                    scope.Complete();
                                                    msg.State = true;
                                                    msg.Text = "保存成功";
                                                }
                                                else
                                                {
                                                    msg.Text = "保存失败";
                                                }
                                            }
                                            else
                                            {
                                                msg.Text = "保存失败";
                                            }
                                        }
                                        catch (Exception e)
                                        {

                                            msg.Text = "数据保存异常";
                                            Console.WriteLine(e);
                                        }
                                    }
                                }
                                else
                                {
                                    msg.Text = "该工号已存在,请重新输入";
                                }
                            }
                            else
                            {
                                msg.Text = "请规范填写邮箱";
                            }
                        }
                        else
                        {
                            msg.Text = "请输入余额";
                        }

                    }
                    else
                    {
                        msg.Text = "请输入姓名";
                    }
                }
                else
                {
                    msg.Text = "工号请填写规范";
                    
                }
            }
            else
            {
                msg.Text = "请选择用户组ID或者用户角色ID";
            }
         return Json(msg,JsonRequestBehavior.AllowGet);
        }









    }
}