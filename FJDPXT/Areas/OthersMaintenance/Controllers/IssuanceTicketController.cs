using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FJDPXT.Model;
using FJDPXT.EntityClass;

namespace FJDPXT.Areas.OthersMaintenance.Controllers
{
    public class IssuanceTicketController : Controller
    {
        // GET: OthersMaintenance/IssuanceTicket
        //其他-->票证下发
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
            Session.Remove("nextStartTicketNo");
            Session.Remove("groupUserIDList");
            return View(); 
        }

        public ActionResult SelectUser()
        {
            try
            {
                //查询出用户所在用户组ID
                int loginUserGroupID = (from tabUser in myModel.S_User
                                        where tabUser.userID == loginUserID
                                        select tabUser.userGroupID).Single();
                //@*查询用户所在用户组ID*@(loginUserGroupID)所有子用户组
                List<S_UserGroup> childGroups = GetChildUserGroup(loginUserGroupID).ToList();
                //查询出用户组的用户组ID List
                List<int> listUsergroupID = (from tabUserGroup in childGroups
                                             select tabUserGroup.userGroupID).ToList();
                //根据用户组ID list查询出对应用户
                List<SelectVo> list = (from tabUser in myModel.S_User
                                           //条件:被listUsergroupID包含的userGroupID
                                       where listUsergroupID.Contains(tabUser.userGroupID)
                                       select new SelectVo()
                                       {
                                           id = tabUser.userID,
                                           text = tabUser.jobNumber + " - " + tabUser.userName
                                       }).ToList();
                //查询出当前用户
                List<SelectVo> loginUser = (from tabUser in myModel.S_User
                                            where tabUser.userID == loginUserID
                                            select new SelectVo()
                                            {
                                                id = tabUser.userID,
                                                text = tabUser.jobNumber + " - " + tabUser.userName
                                            }).ToList();
                //把当前用户添加到列表中  Add添加一个   AddRange添加多个
                list.AddRange(loginUser);
                //对list进行排序
                list = list.OrderBy(o => o.id).ToList();
                //将用户列表保存到session中,供列表查询时使用
                List<int> groupUserIDList = new List<int>();
                foreach (SelectVo vo in list)
                {
                    groupUserIDList.Add(vo.id);
                }
                Session["groupUserIDList"] = groupUserIDList;
                return Json(list, JsonRequestBehavior.AllowGet);
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return null;
        }
        private IEnumerable<S_UserGroup> GetChildUserGroup(int superiorUserGroupID)
        {
            var query = from tabUserGroup in myModel.S_UserGroup
                        where tabUserGroup.superiorUserGroupID == superiorUserGroupID
                        select tabUserGroup;
            return query.ToList().Concat(query.ToList().SelectMany(o=>GetChildUserGroup(o.userGroupID)));
            
        }
        //获取下发开始的票号
        public ActionResult GetStartTicketNo()
        {
            try
            {
                //获取最后一个票号段
                S_Ticket lastTicket = (from tabTicket in myModel.S_Ticket
                                       orderby tabTicket.ticketID descending/*倒序*/
                                       select tabTicket).FirstOrDefault();
                int nextStartTicketNo = 0;
                if (lastTicket != null)
                {
                    string strEndTicketNo = lastTicket.endTicketNo;
                    int endTickNo = Convert.ToInt32(strEndTicketNo);
                    //下发的开始票号
                    nextStartTicketNo = endTickNo + 1;
                    //本次开始的票号  //PadLeft 返回一个指定长度字符串，如果原字符串长度不足，就在左边填充指定的字符
                    string strStartTicketNo = "E781-" + nextStartTicketNo.ToString().PadLeft(10, '0');
                    //将开始票号放入session,供下发票证时使用   
                    Session["nextStartTicketNo"] = nextStartTicketNo;
                    return Json(strStartTicketNo, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        public ActionResult SelectTicket(LayuiTablePage layuiTablePage, int UserID)
        {
            var query = from tabTicket in myModel.S_Ticket
                        join tabUser in myModel.S_User
                        on tabTicket.userID equals tabUser.userID
                        join tabUserGroup in myModel.S_UserGroup
                        on tabUser.userGroupID equals tabUserGroup.userGroupID
                        orderby tabTicket.ticketID
                        select new TickVo()
                        {
                            ticketID = tabTicket.ticketID,
                            ticketDate = tabTicket.ticketDate,
                            startTicketNo = tabTicket.startTicketNo,
                            endTicketNo = tabTicket.endTicketNo,
                            currentTicketNo = tabTicket.currentTicketNo,
                            userID = tabTicket.userID,
                            //扩展
                            jobNumber = tabUser.jobNumber,
                            userName = tabUser.userName,
                            userGroupNumber = tabUserGroup.userGroupNumber
                        };


            //userID为0时 查询下拉框中所有的用户
            if (UserID == 0)
            {
                List<int> groupUserIDList = Session["groupUserIDList"] as List<int>;
                query = query.Where(o => groupUserIDList.Contains(o.userID.Value));
            }
            else
            {
                //不为0时查询指定的用户
                query = query.Where(o => o.userID == UserID);
            }


            //总行数
            int totalRows = query.Count();
            //分页查询
            List<TickVo> listData = query
                .Skip(layuiTablePage.GetStartIndex())
                .Take(layuiTablePage.limit)
                .ToList();

            LayuiTableData<TickVo> layuiTableData = new LayuiTableData<TickVo>()
            {
                count = totalRows,
                data = listData
            };


            return Json(layuiTableData,JsonRequestBehavior.AllowGet);

        }
        public ActionResult InsertTicket(int userID ,int votes)
        {
            ReturnJson msg = new ReturnJson();

            if (userID > 0)
            {
                if (votes > 0)
                {
                    try
                    {
                        //获取出开始票号
                        int startTicketNo = Convert.ToInt32(Session["nextStartTicketNo"].ToString());
                        S_Ticket saveTicket = new S_Ticket();
                        //用户ID
                        saveTicket.userID = userID;
                        //下发日期
                        saveTicket.ticketDate = DateTime.Now;
                        //开始票号
                        saveTicket.startTicketNo = startTicketNo.ToString().PadLeft(10, '0');
                        //当前票号(还未使用,当前票号就是开始票号)
                        saveTicket.currentTicketNo = saveTicket.startTicketNo;
                        //结束票号=开始票号+下发数量-1
                        saveTicket.endTicketNo = (startTicketNo + votes - 1).ToString().PadLeft(10, '0');
                        //是否启用
                        saveTicket.isEnable = true;

                        //保存到数据库
                        myModel.S_Ticket.Add(saveTicket);
                        if (myModel.SaveChanges() > 0)
                        {
                            Session.Remove("nextStartTicketNo");
                            msg.State = true;
                            msg.Text = "票证下发成功";
                        }
                        else
                        {
                            msg.Text = "票证下发成功";
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        msg.Text = "下发票号出现异常";
                    }
                }
                else
                {
                    msg.Text = "请输入要下发的票数(正整数)";
                }
            }
            else
            {
                msg.Text = "请选择下发用户";
            }


            return Json(msg, JsonRequestBehavior.AllowGet);
        }


    }
}