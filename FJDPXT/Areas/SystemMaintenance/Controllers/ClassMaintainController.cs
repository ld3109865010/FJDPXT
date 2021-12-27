using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FJDPXT.Model;
using FJDPXT.EntityClass;
using System.Text.RegularExpressions;

namespace FJDPXT.Areas.SystemMaintenance
{
    public class ClassMaintainController : Controller
    {
        // GET: SystemMaintenance/ClassMaintain
        //系统维护->舱位等级维护

        //实例化model
        FJDPXTEntities1 myModel = new FJDPXTEntities1();
        public ActionResult Index()
        {
            try
            {
                int userID = Convert.ToInt32(Session["userID"].ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Redirect(Url.Content("~/Main/Login"));
                
            }
            return View();
        }

        #region 分页查询部分
        public ActionResult SelectCabinType(LayuiTablePage LayuiTablePage)
        {
            List<S_CabinType> listData = (from tabCabinType in myModel.S_CabinType
                                          orderby tabCabinType.cabinTypeID
                                          select tabCabinType)
                                         .Skip(LayuiTablePage.GetStartIndex())
                                         .Take(LayuiTablePage.limit)
                                         .ToList();

            //查询总条数
            int totalRows = (from tabCabinType in myModel.S_CabinType
                             select tabCabinType).Count();
            //组装layui table所需的数据格式
            LayuiTableData<S_CabinType> layuiTableData = new LayuiTableData<S_CabinType>
            {
                count = totalRows,//当前总条数
                data = listData,//当前页的数据

            };
            return Json(layuiTableData, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Insert新增部分
        public ActionResult InsertCabinType(S_CabinType cabinType)
        {
            ReturnJson msg = new ReturnJson();

            if (!string.IsNullOrEmpty(cabinType.cabinTypeCode) && cabinType.cabinTypeCode.Length == 1
                && Regex.IsMatch(cabinType.cabinTypeCode, "^[A-Z]{1}$"))
            {
                if (!string.IsNullOrEmpty(cabinType.cabinTypeName))
                {
                    if (cabinType.basisPrice != null && cabinType.basisPrice > 0 && cabinType.basisPrice < 999999)
                    {
                        if (cabinType.discountRate != null && cabinType.discountRate > 0)
                        {
                            //检查舱位编码或者舱位名称是否存在
                            int oldCount = myModel.S_CabinType.Count(o => o.cabinTypeCode == cabinType.cabinTypeCode);
                            if (oldCount == 0)
                            {
                                myModel.S_CabinType.Add(cabinType);
                                if (myModel.SaveChanges() > 0)
                                {
                                    msg.State = true;
                                    msg.Text = "新增成功";
                                }
                                else
                                {
                                    msg.Text = "新增失败";
                                }
                            }
                            else
                            {
                                msg.Text = "需要新增的舱位编码已经存在";
                            }
                        }

                        else
                        {
                            msg.Text = "请规范填写折扣率";
                        }
                    }
                    else
                    {
                        msg.Text = "请规范填写基础价格";
                    }
                }
                else
                {
                    msg.Text = "请规范填写舱位名称";
                }
            }
            else
            {
                msg.Text = "请规范填写舱位编码";
            }



            myModel.S_CabinType.Add(cabinType);

            return Json(msg, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region UpDate修改部分

        //public ActionResult SelectCabinTypeByID(int cabinTypeID)
        //{
        //    try
        //    {
        //        //根据ID查询舱位等级数据
        //        S_CabinType dbCabinType = myModel.S_CabinType.Single(o => o.cabinTypeID == cabinTypeID);

        //        return Json(dbCabinType, JsonRequestBehavior.AllowGet);
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //        return null;

        //    }
        //}
        public ActionResult SelectCabinTypeByID(int cabinTypeID)
        {
            try
            {
                //根据ID查询舱位等级数据
                S_CabinType dbCabinType = myModel.S_CabinType.Single(o => o.cabinTypeID == cabinTypeID);
                
                return Json(dbCabinType, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;

            }
        }
        public ActionResult UpdateCabinType(S_CabinType cabinType)
        {
            ReturnJson msg = new ReturnJson();
            //验证数据
            //主键ID 大于0
            
            if (cabinType.cabinTypeID > 0)
            { 
                
                //舱位编号
                if (!string.IsNullOrEmpty(cabinType.cabinTypeCode) && cabinType.cabinTypeCode.Length == 1
                && Regex.IsMatch(cabinType.cabinTypeCode, "^[A-Z]{1}$"))
                {
                    //舱位名称
                    if (!string.IsNullOrEmpty(cabinType.cabinTypeName))
                    {
                        //基础价格
                        if (cabinType.basisPrice != null && cabinType.basisPrice > 0 && cabinType.basisPrice < 999999)
                        {
                            //折扣率
                            if (cabinType.discountRate != null && cabinType.discountRate > 0)
                            {
                                int otherCount = myModel.S_CabinType
                                .Count(o => o.cabinTypeID != cabinType.cabinTypeID
                                       &&o.cabinTypeCode == cabinType.cabinTypeCode);

                                if (otherCount == 0)
                                {
                                    myModel.Entry(cabinType).State = System.Data.Entity.EntityState.Modified;
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
                                    msg.Text = "要修改的舱位编码已经存在";
                                }
                            }
                            else
                            {
                                msg.Text = "请规范填写折扣率";
                            }
                        }

                        else
                        {
                            msg.Text = "请规范填写基础价格";
                        }
                    }
                    else
                    {
                        msg.Text = "请规范填写舱位名称";
                    }
                }
                else
                {
                    msg.Text = "请规范填写舱位编码";
                }
            }
            else
            {
                msg.Text = "参数异常,请稍后再试";
            }




            return Json(msg, JsonRequestBehavior.AllowGet);
        }

    








        #endregion

        #region Delete删除部分
        public ActionResult DeleteCabinTypeByID(int cabinTypeID)
        {
            ReturnJson msg = new ReturnJson();
            if (cabinTypeID > 0)
            {
                //判断指定删除数据是否在使用中(与其他表是否有关联)
                //pnr航段表
                int useInPNRSegment = myModel.B_PNRSegment.Count(o => o.cabinTypeID == cabinTypeID);
                //航班舱位表
                int useInFlightCabin = myModel.S_FlightCabin.Count(o => o.cabinTypeID == cabinTypeID);
                if(useInFlightCabin==0 && useInPNRSegment == 0)
                {
                    try
                    {
                        //查询被指定删除的数据
                        S_CabinType dbCabinType = myModel.S_CabinType.Single(o => o.cabinTypeID == cabinTypeID);
                        //删除数据
                        myModel.S_CabinType.Remove(dbCabinType);
                        if (myModel.SaveChanges() > 0)
                        {
                            msg.State = true;
                            msg.Text = "删除成功";
                        }
                        else
                        {
                            msg.Text = "删除失败";
                        }
                    }
                    catch (Exception e)
                    {

                        Console.WriteLine(e);
                        msg.Text = "数据异常,删除失败";
                    }
                }
                else
                {
                    msg.Text = "该数据正在被使用";
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