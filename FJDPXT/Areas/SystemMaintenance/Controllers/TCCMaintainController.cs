using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FJDPXT.Model;
using FJDPXT.EntityClass;
using System.Text.RegularExpressions;

namespace FJDPXT.Areas.SystemMaintenance.Controllers
{
    public class TCCMaintainController : Controller
    {
        //实例化model
        FJDPXTEntities1 myModel = new FJDPXTEntities1();
        // GET: SystemMaintenance/TCCMaintain
        // 系统维护 -> 三字码维护
        public ActionResult Index()
        {
            try
            {
                int userID = Convert.ToInt32(Session["userID"].ToString());
                return View();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                //重定向
                //重定向到登录页面
                //重定向有俩种方法
                //return Redirect("https://www.baidu.com");外网
                return Redirect(Url.Content("~/Main/Login"));//内网
                //return View();
            }
        }

        public ActionResult SelectAirport(LayuiTablePage layuiTablePage)
        {
            //<>泛型                  from:要的内容   in内容的路径         select选择
            List<S_Airport> listData = (from tabAirport in myModel.S_Airport
                                        orderby tabAirport.airportCode
                                        select tabAirport)
                                        .Skip(layuiTablePage.GetStartIndex())
                                        .Take(layuiTablePage.limit)
                                        .ToList();

            //查询数据的总条数 lambda表达式
            int totalRows = myModel.S_Airport.Count();
            //totalRows = (from tabAirport in myModel.S_Airport
            //            select tabAirport).Count();



            LayuiTableData<S_Airport> layuiTableData = new LayuiTableData<S_Airport>() {
                count = totalRows,//总条数
                data = listData,  //数据
            };
            //layuiTableData.count = totalRows;//总条数
            //layuiTableData.data = listData;//数据
            return Json(layuiTableData, JsonRequestBehavior.AllowGet);
            
        }

        #region Insert新增类
         public ActionResult InserAirport(S_Airport airport)
        {
            ReturnJson msg = new ReturnJson();

            //===1-验证数据
            //==1.1-验证三字码
            if(airport.airportCode!=null && airport.airportCode.Length == 3 
                && Regex.IsMatch(airport.airportCode,"^[A-Z]{3}$"))//^代表从 $代表到 从A到Z重复三遍
            {
                //验证机场名称
                if (!string.IsNullOrEmpty(airport.airportName)
                    && Regex.IsMatch(airport.airportName, "^[\u4e00-\u9fa5A-Za-z]+$"))
                {
                    //验证城市名称
                    if (!string.IsNullOrEmpty(airport.cityName))
                    {
                        //判断新增数据是否已存在
                        int oldAirportCount = (from tabAirport in myModel.S_Airport
                                               where tabAirport.airportCode == airport.airportCode
                                               || tabAirport.airportName == airport.airportName
                                               select tabAirport).Count();
                        if (oldAirportCount == 0) {
                            //新增数据到数据库
                            myModel.S_Airport.Add(airport);
                            if (myModel.SaveChanges() > 0)
                            {
                                msg.Text = "新增成功";
                                msg.State = true;
                            }
                            else
                            {
                                msg.Text = "新增失败";
                            }   
                        }
                        else
                        {
                            msg.Text = "已经存在该三字码或者机场名称";
                        }  
                    }
                    else
                    {
                        msg.Text = ("请填写城市名称");
                    }
                }
                else
                {
                    msg.Text = ("请填写机场名称");
                }
            }
            else
            {
                msg.Text = "请填写规范";//保证三字码的位数为3
            }
            return Json(msg, JsonRequestBehavior.AllowGet);
        }





        #endregion

        #region UpDate修改类
        //根据主键(ID)查询机场信息
        public ActionResult SelectAirportByID(int airportID)
        {
            try
            {
                S_Airport airport = (from tabAirport in myModel.S_Airport
                                     where tabAirport.airportID == airportID
                                     select tabAirport).Single();
                return Json(airport, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }
        public ActionResult UpdateAirport(S_Airport airport)
        {
             ReturnJson msg = new ReturnJson();
            if (airport.airportID > 0)
            {
                if (airport.airportCode != null && airport.airportCode.Length == 3)
                {
                    //验证机场名称
                    if (!string.IsNullOrEmpty(airport.airportName))
                    {
                        //验证城市名称
                        if (!string.IsNullOrEmpty(airport.cityName))
                        {
                            //判断新增数据是否已存在
                            int oldOtherAirportCount = (from tabAirport in myModel.S_Airport
                                                   where tabAirport.airportID!=airport.airportID
                                                   && (tabAirport.airportCode == airport.airportCode
                                                   || tabAirport.airportName == airport.airportName)
                                                   select tabAirport).Count();
                            if (oldOtherAirportCount == 0)
                            {
                                //===3- 保存修改后的数据到数据库
                                //3.1-标记该条数据被修改
                                myModel.Entry(airport).State = System.Data.Entity.EntityState.Modified;
                                //3.2保存修改
                                if (myModel.SaveChanges() > 0)
                                {
                                    msg.State = true;
                                    msg.Text = "修改成功";
                                    
                                }
                                else
                                {
                                    msg.Text = "修改失败";
                                }
                            }
                            else
                            {
                                msg.Text = "已经存在该三字码或者机场名称";
                            }
                        }
                        else
                        {
                            msg.Text = ("请填写城市名称");
                        }
                    }
                    else
                    {
                        msg.Text = ("请填写机场名称");
                    }
                }
                else
                {
                    msg.Text = "三字码请填写规范";//保证三字码的位数为3
                }
            }
            else
            {
                msg.Text = "参数异常";
            }


            return Json(msg, JsonRequestBehavior.AllowGet);
        }









        #endregion

        #region Delete删除类
        public ActionResult DeleteAirportByID(int airportID)
        {
            ReturnJson msg = new ReturnJson();

            //===1-检查被删除机场的ID是否大于0
            if (airportID > 0)
            {
                try
                {
                    //==2-先查询出需要被删除的机场 
                    //方法①Linq表达式
                    S_Airport dbAirport = (from tabAirport in myModel.S_Airport
                                       where tabAirport.airportID == airportID
                                       select tabAirport).Single();
                    //方法②Lambda表达式
                    //S_Airport dbAirport = myModel.S_Airport.Single(
                    //       tabAirport => tabAirport.airportID == airportID);
                    //==3-检查被删除机场是否在(航班表)使用中
                    int useAirportCount = (from tabFlight in myModel.S_Flight
                                           where tabFlight.orangeID == airportID//起飞机场ID
                                           || tabFlight.destinationID == airportID//到达机场ID
                                           select tabFlight).Count();
                    if (useAirportCount == 0)
                    {
                        //该机场没有被使用
                        //删除数据
                        myModel.S_Airport.Remove(dbAirport);
                        //保存删除到数据库
                        if (myModel.SaveChanges() > 0)
                        {
                            msg.State = true;
                            msg.Text = "删除成功";
                        }
                    }
                    else
                    {
                        //该机场正在被使用
                        msg.Text = "该机场正在被使用,无法删除";
                    }
                }
                catch (Exception e)
                {

                    Console.WriteLine(e);
                    msg.Text = "机场数据不存在,无法删除";
                }
                
            }
            else
            {
                msg.Text = "参数异常";
            }



            return Json(msg, JsonRequestBehavior.AllowGet);
        }







        #endregion
    }
}