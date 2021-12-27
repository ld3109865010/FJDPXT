using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FJDPXT.Model;
using FJDPXT.EntityClass;

namespace FJDPXT.Areas.SystemMaintenance.Controllers
{
    public class TicketNumMaintainController : Controller
    {
        // GET: SystemMaintenance/TicketNumMaintain
        //系统维护-->票号维护
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

        //查询用户及用户所在用户组的子用户组的用户 for table
        public ActionResult SelectTicket(LayuiTablePage layuiTablePage)
        {
            //查询出当前登录的用户
            S_User loginUser = myModel.S_User.Single(o => o.userID == loginUserID);
            //查询用户所在用户组ID
            int userGroupID = loginUser.userGroupID;
            //获取登录用户所在用户组的子用户组
            List<S_UserGroup> childUserGroup = getChildUserGroup(userGroupID).ToList();
            //获取出用户组的用户组ID
            List<int> userGroupIDs = new List<int>();
            foreach(S_UserGroup userGroup in childUserGroup)
            {
                userGroupIDs.Add(userGroup.userGroupID);
            }
            //获取childUserGroup包含的用户的ID
            List<int> userIDs = (from tabUser in myModel.S_User
                                 where userGroupIDs.Contains(tabUser.userGroupID)
                                 select tabUser.userID).ToList();
            //把当前登录的用户组ID添加到list
            userIDs.Add(loginUserID);

            var query = from tabTicket in myModel.S_Ticket
                        join tabUser in myModel.S_User on tabTicket.userID equals tabUser.userID
                        join tabUserGroup in myModel.S_UserGroup on tabUser.userGroupID equals tabUserGroup.userGroupID
                        orderby tabTicket.ticketID
                        where userIDs.Contains(tabUser.userID)
                        select new TickVo()
                        {
                            ticketID = tabTicket.ticketID,
                            ticketDate = tabTicket.ticketDate,
                            startTicketNo = tabTicket.startTicketNo,
                            endTicketNo = tabTicket.endTicketNo,
                            currentTicketNo = tabTicket.currentTicketNo,
                            userID = tabTicket.userID,
                            isEnable = tabTicket.isEnable,
                            //
                            jobNumber = tabUser.jobNumber,
                            userName = tabUser.userName,
                            userGroupNumber = tabUserGroup.userGroupNumber
                        };

            //获取总行数
            int totalRows = query.Count();
            //分页查询
            List<TickVo> listData = query
                .Skip(layuiTablePage.GetStartIndex())
                .Take(layuiTablePage.limit)
                .ToList();
            //准备layui table所需的数据
            LayuiTableData<TickVo> layuiTableData = new LayuiTableData<TickVo>()
            {
                count = totalRows,
                data = listData
            };
            return Json(layuiTableData,JsonRequestBehavior.AllowGet);
        }
        //根据用户组ID 递归查询子用户组
        private IEnumerable<S_UserGroup> getChildUserGroup(int superiorUserGroupID)
        {
            var query = from tabUserGroup in myModel.S_UserGroup
                        where tabUserGroup.superiorUserGroupID == superiorUserGroupID
                        select tabUserGroup;
            return query.ToList().Concat(query.ToList().SelectMany(o => getChildUserGroup(o.userGroupID)));
        }

        public ActionResult switchTitcketEnable(int ticketID,bool isEnable)
        {
            ReturnJson msg = new ReturnJson();

            try
            {
                //查询出需要启用/禁用的票号信息
                S_Ticket dbTicket = myModel.S_Ticket.Single(o => o.ticketID == ticketID);
                //修改isEnable
                dbTicket.isEnable = isEnable;
                //保存
                if (myModel.SaveChanges() > 0)
                {
                    msg.State = true;
                    msg.Text = (isEnable ? "启用" : "禁用") + "票号信息成功";
                }
                else
                {
                    msg.Text = (isEnable ? "启用" : "禁用") + "票号信息失败";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                msg.Text = "参数异常";
            }

            return Json(msg, JsonRequestBehavior.AllowGet);
        }
        public ActionResult DeleteTicket(int ticketID)
        {
            ReturnJson msg = new ReturnJson();
            try
            {
                //查询出需要启用/禁用的票号信息
                S_Ticket dbTicket = myModel.S_Ticket.Single(o => o.ticketID == ticketID);
                //判断该票号段的开始票号是否在使用中
                string startTicketNo= "E781-" + dbTicket.startTicketNo;
                int count = (from tabETicket in myModel.B_ETicket
                             where tabETicket.ticketNo == startTicketNo
                             select tabETicket).Count();
                if (count == 0)
                {
                    myModel.S_Ticket.Remove(dbTicket);
                    if (myModel.SaveChanges() > 0)
                    {
                        msg.State = true;
                        msg.Text = "票号信息删除成功";
                    }
                    else
                    {
                        msg.Text = "票号信息删除失败";
                    }
                }
                else
                {
                    msg.Text = "该票号段正在被使用,无法删除";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                msg.Text = "参数异常";
            }

            return Json(msg, JsonRequestBehavior.AllowGet);
        }
    }
}