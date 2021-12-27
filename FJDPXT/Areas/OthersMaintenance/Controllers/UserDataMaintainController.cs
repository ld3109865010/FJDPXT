using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FJDPXT.Model;
using FJDPXT.EntityClass;
using System.Text.RegularExpressions;
using FJDPXT.Common;
using System.Transactions;
using System.IO;
using System.Data;

namespace FJDPXT.Areas.OthersMaintenance.Controllers
{
    public class UserDataMaintainController : Controller
    {
        // GET: OthersMaintenance/UserDataMaintain
        // 其他-->用户资料维护
        //实例化model
        FJDPXTEntities1 myModel = new FJDPXTEntities1();

        private int loginUserID=0;

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

        public ActionResult UserList(string userGroup)
        {
            Session["userGroup"] = userGroup;

            return View();
        }
        public ActionResult SelectUserList(LayuiTablePage layuiTablePage)
        {
            //获取session中用户要查询的用户组
            string userGroup = "";
            if (Session["userGroup"] != null)
            {
                userGroup = Session["userGroup"].ToString();
            }
            var varQuery = from tabUser in myModel.S_User
                               //连接用户组表 通过用户组ID
                           join tabUserGroup in myModel.S_UserGroup
                           on tabUser.userGroupID equals tabUserGroup.userGroupID
                           //连接角色 通过角色ID
                           join tabUserType in myModel.S_UserType
                           on tabUser.userTypeID equals tabUserType.userTypeID
                           //连接虚拟账户表 通过用户ID
                           join tabVirtualAccount in myModel.S_VirtualAccount
                           on tabUser.userID equals tabVirtualAccount.userID
                           orderby tabUser.userID
                           select new UserVo() {
                               userID = tabUser.userID,
                               userTypeID = tabUser.userTypeID,
                               userGroupID = tabUser.userGroupID,
                               jobNumber = tabUser.jobNumber,
                               userName = tabUser.userName,
                               userEmail = tabUser.userEmail,
                               isEnable = tabUser.isEnable,
                               userPicture=tabUser.userPicture,//用户头像图片名称
                               //扩展
                               userGroupNumber = tabUserGroup.userGroupNumber,
                               userType = tabUserType.userType,
                               accountBalance = tabVirtualAccount.accountBalance.Value,
                           };
            

            //使用lambda表达式拼接查询条件用户组
            if (!string.IsNullOrEmpty(userGroup))
            {
                varQuery=varQuery.Where(o=>o.userGroupNumber==userGroup);
            }

            //数据总条数
            int totalRows =varQuery.Count();
            //分页查询数据
            List<UserVo> listData=varQuery
                                  .Skip(layuiTablePage.GetStartIndex())
                                  .Take(layuiTablePage.limit)
                                  .ToList();
            //layui table 所需的数据格式
            LayuiTableData<UserVo> layuiTableData=new LayuiTableData<UserVo>()
            {
                count = totalRows,
                data = listData,
            };
            return Json(layuiTableData, JsonRequestBehavior.AllowGet);
            
        }
        //启用/禁用用户
        public ActionResult SwitchUserEnable(int userID,bool isEnable)
        {
            ReturnJson msg = new ReturnJson();

            try
            {
                if (userID != 1)
                {
                    if (userID == loginUserID)
                    {
                        msg.Text = "不能修改自己当前的账户";
                    }
                    else {
                        try
                        {
                            //查询需要 启用/禁用的账户数据
                            S_User dbUser = myModel.S_User.Single(o => o.userID == userID);
                            //启用/禁用用户--本质 修改用户数据的isEnable字段
                            dbUser.isEnable = isEnable;
                            myModel.Entry(dbUser).State = System.Data.Entity.EntityState.Modified;
                            if (myModel.SaveChanges() > 0)
                            {
                                msg.State = true;
                                msg.Text = isEnable ? "启用" : "禁用" + "用户成功";
                            }
                            else
                            {
                                msg.Text = isEnable ? "启用" : "禁用" + "用户失败";
                            }
                        }
                        catch (Exception el)
                        {
                            Console.WriteLine(el);
                            msg.Text = "参数异常";
                        }
                    }
                    
                    
                    }
            else{
                    msg.Text = "超级账户不允许修改";
                }
                
            }
        catch (Exception e)
        {
            msg.Text = "未登录,无法操作";
            
            Console.WriteLine(e);
        }
            

            return Json(msg, JsonRequestBehavior.AllowGet);
        }

        #region UpDate修改部分
            public ActionResult SelectUsertTypeForSelect()
                {
                    List<SelectVo> list = (from tabUserType in myModel.S_UserType
                                           select new SelectVo()
                                           {
                                               id = tabUserType.userTypeID,
                                               text = tabUserType.userType
                                           }).ToList();

                    //System.Threading.Thread.Sleep(1000);//休眠一秒 模拟外网请求
                    return Json(list, JsonRequestBehavior.AllowGet);
                }


            public ActionResult SelectUserByID(int userID)
                {
                    try
                    {
                        UserVo user = (from tabUser in myModel.S_User
                                       join tabUserGroup in myModel.S_UserGroup
                                       on tabUser.userGroupID equals tabUserGroup.userGroupID
                                       join tabVirtualAccount in myModel.S_VirtualAccount
                                       on tabUser.userID equals tabVirtualAccount.userID
                                       where tabUser.userID == userID
                                       select new UserVo()
                                       {
                                           userID = tabUser.userID,//用户ID
                                           userGroupID = tabUser.userGroupID,//用户组ID
                                           userTypeID = tabUser.userTypeID,//用户角色ID
                                           jobNumber = tabUser.jobNumber,//工号
                                           userName = tabUser.userName,//用户名
                                           userEmail = tabUser.userEmail,//邮箱
                                           isEnable = tabUser.isEnable,//是否启用
                                           userPicture=tabUser.userPicture,//用户头像
                                           //扩展
                                           userGroupNumber = tabUserGroup.userGroupNumber,//
                                           accountBalance = tabVirtualAccount.accountBalance.Value
                                       }).Single();
                        return Json(user, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        return null;
                    }
                }


            public ActionResult UpdateUser(S_User user ,HttpPostedFileBase userPicture)
                {
                    ReturnJson msg = new ReturnJson();
                    //数据验证
                    if (user.userID > 0)
                    {
                        if (user.userID != 1)
                        {
                            //用户组用户角色
                            if (user.userGroupID > 0 && user.userTypeID > 0)
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
                                            if (!string.IsNullOrEmpty(user.userEmail) && 
                                                Regex.IsMatch(user.userEmail,"^([a-zA-Z]|[0-9])(\\w|\\-)+@[a-zA-Z0-9]+\\.([a-zA-Z]{2,4})$"))
                                            {
                                                if(loginUserID != 0)
                                                {
                                                    //判断工号是否已经存在(除去自身之外)
                                                    int oldCount = myModel.S_User.Count(o => o.jobNumber == user.jobNumber && o.userID != user.userID);
                                                    if (oldCount == 0)
                                                    {
                                                        //开启事务
                                                        using (TransactionScope scope = new TransactionScope())
                                                        {
                                                    try
                                                    {
                                                        //判断是否修改密码
                                                        if (!string.IsNullOrEmpty(user.userPassword))
                                                        {
                                                            user.userPassword = AESEncryptHelper.Encrypt(user.userPassword);
                                                        }
                                                        else
                                                        {
                                                            //在未修改密码的情况下回填原密码
                                                            //Linq表达式写法
                                                            string dbPassword = (from tabUser in myModel.S_User
                                                                                 where tabUser.userID == user.userID
                                                                                 select tabUser.userPassword).Single();
                                                            //lamdba表达式写法①
                                                            //string dbPassword = myModel.S_User.Single(o => o.userID == user.userID).userPassword;
                                                            //lamdba表达式写法②
                                                            //string dbPassword = myModel.S_User
                                                            //                   .Where(o => o.userID == user.userID)
                                                            //                   .Select(o => o.userPassword)
                                                            //                   .Single();
                                                            user.userPassword = dbPassword;
                                                        }
                                                        //=====图片处理
                                                        //查询出旧的图片名称
                                                        string oldPictute = (from tabUser in myModel.S_User
                                                                             where tabUser.userID == user.userID
                                                                             select tabUser.userPicture).Single();
                                                        //判断图片是否上传
                                                        if (userPicture != null && userPicture.ContentLength > 0)
                                                        {
                                                            //检查存放用户头像的目录是否存在
                                                            if (!System.IO.Directory.Exists(Server.MapPath("~/Document/userPicture/"))) {
                                                                //不存在就创建一个
                                                                System.IO.Directory.CreateDirectory(Server.MapPath("~/Document/userPicture/"));
                                                            }
                                                            //图片上传
                                                            //获取文件扩展名
                                                            string imgExtension = System.IO.Path.GetExtension(userPicture.FileName);
                                                            //拼接要保存的文件名称
                                                            string fileName = DateTime.Now.ToString("yyyyMMddHHmmssffff") + "_" + Guid.NewGuid() + imgExtension;
                                                            //拼接文件保存的路径
                                                            string filePath = Server.MapPath("~/Document/userPicture/") + fileName;
                                                            //保存上传的文件到硬盘
                                                            userPicture.SaveAs(filePath);

                                                            //文件名称保存到user对象
                                                            user.userPicture = fileName;

                                                            //判断是否有旧图片,有则删除
                                                            if (!string.IsNullOrEmpty(oldPictute)) {
                                                                string oldFilePath = Server.MapPath("~/Document/userPicture/") + oldPictute;
                                                                //判断文件是否存在
                                                                if (System.IO.File.Exists(oldFilePath)) {
                                                                    //删除文件
                                                                    System.IO.File.Delete(oldFilePath);
                                                                }
                                                            }
                                                        }
                                                        else {
                                                            //未上传
                                                            user.userPicture = oldPictute;
                                                        }




                                                                //修改用户
                                                                myModel.Entry(user).State = System.Data.Entity.EntityState.Modified;
                                                                if (myModel.SaveChanges() > 0)
                                                                {
                                                                    scope.Complete();
                                                                    msg.State = true;
                                                                    msg.Text = "修改成功";
                                                                }
                                                                else
                                                                {
                                                                    msg.Text = "修改失败";
                                                                }
                                                            }
                                                            catch (Exception e)
                                                            {
                                                                Console.WriteLine(e);
                                                                msg.Text = "数据异常";
                                                        
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
                                                    msg.Text = "您没有登录,无法修改";
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
                        }
                        else
                        {
                            msg.Text = "admin账号不允许被其他账号修改";
                        }
                    }
                    else
                    {
                        msg.Text="参数异常,修改失败";
                    }

                    return Json(msg, JsonRequestBehavior.AllowGet);
                }


                #endregion

        #region Delete删除部分

                public ActionResult DeleteUser(int userID)
                {
                    ReturnJson msg = new ReturnJson();

                    try
                    {
                        if (userID != 1)
                        {
                            if (userID != loginUserID)
                            {
                                //判断用户是否在使用中
                                int countPNR = myModel.B_PNR.Count(o => o.operatorID == userID);
                                int countOrder = myModel.B_Order.Count(o => o.operatorID == userID);
                                int countETicket = myModel.B_ETicket.Count(o => o.operatorID == userID);
                                int countFlightChange = myModel.B_FlightChange.Count(o => o.operatorID == userID);
                                int countTicketRefund = myModel.B_TicketRefund.Count(o => o.operatorID == userID);
                                int countETicketChange = myModel.B_ETicketChange.Count(o => o.operatorID == userID);
                                int countETicketInvalid = myModel.B_ETicketInvalid.Count(o => o.operatorID == userID);

                                if(countPNR+ countOrder+ countETicket+ countFlightChange+ countTicketRefund+ countETicketChange+ countETicketInvalid == 0)
                                {
                                    using(TransactionScope scope=new TransactionScope())
                                    {
                                        //查询需要删除的用户
                                        S_User dbUser = myModel.S_User.Single(o => o.userID == userID);
                                        myModel.S_User.Remove(dbUser);
                                        if (myModel.SaveChanges() > 0)
                                        {
                                            //删除用户的图片
                                            string oldPicture = dbUser.userPicture;
                                            //判断是否含有图片,有就删除
                                            if (!string.IsNullOrEmpty(oldPicture)) {
                                                string oldFilePath = Server.MapPath("~/Document/userPicture/") + oldPicture;
                                                //判断文件是否存在
                                                if (System.IO.File.Exists(oldFilePath)) {
                                                    //删除文件
                                                    System.IO.File.Delete(oldFilePath);
                                                }
                                            }
                                            //查询删除用户对应的虚拟账户
                                            S_VirtualAccount dbVirtualAccount = myModel.S_VirtualAccount.Single(o => o.userID == userID);
                                            myModel.S_VirtualAccount.Remove(dbVirtualAccount);
                                            if (myModel.SaveChanges() > 0)
                                            {
                                                scope.Complete();
                                                msg.State = true;
                                                msg.Text = "删除成功";
                                            }
                                            else
                                            {
                                                msg.Text = "删除失败";
                                            }
                                        }
                                        else
                                        {
                                            msg.Text = "删除失败";
                                        }

                                
                                    }
                                }
                                else
                                {
                                    msg.Text = "该用户正在使用中,无法删除";
                                }
                            }
                            else
                            {
                                msg.Text = "不允许删除自身用户";
                            }
                        }
                        else
                        {
                            msg.Text = "无法操作admin账户数据";
                        }
                    }
                    catch (Exception e)
                    {

                        Console.WriteLine(e);
                    }



                    return Json(msg, JsonRequestBehavior.AllowGet);
                }

        #endregion

        #region 导入

        /// <summary>
        /// 导入模板下载
        /// </summary>
        /// <returns></returns>
        public ActionResult DownImportTemplate()
        {
            //获取模板文件的路径
            string templateFilePath = Server.MapPath("~/Document/用户导入模板.xls");
            //判断模板是否存在
            if (System.IO.File.Exists(templateFilePath))
            {
                //获取文件名称
                string fileName = System.IO.Path.GetFileName(templateFilePath);

                //以流的形式返回文件
                return File(new  System.IO.FileStream(templateFilePath, System.IO.FileMode.Open),
                    "application/octet-stream", fileName);
            }
            else {
                return Content("导入模板不存在，请联系网站管理人员");
            }
        }

        /// <summary>
        /// 导入Excel表格
        /// </summary>
        /// <param name="excelFile"></param>
        /// <returns></returns>
        public ActionResult ImportExcel(HttpPostedFileBase excelFile)
        {
            ReturnJson msg = new ReturnJson();
            try
            {
                //判断文件后缀--扩展名
                string fileExtension = System.IO.Path.GetExtension(excelFile.FileName);
                if (".xls".Equals(fileExtension, StringComparison.CurrentCultureIgnoreCase))
                {
                    //转换成二进制数组
                    //声明一个和文件大小一致的二进制数组
                    byte[] fileBytes = new byte[excelFile.ContentLength];
                    //将上传的文件转成二进制数组  
                    excelFile.InputStream.Read(fileBytes, 0, fileBytes.Length);//将上传的excelFile读到fileBytes(二进制数组)中
                    //将二进制数组转为内存流     excelFile中包含的上传流和NPOI一起使用的时候可能会出现被占用的情况 所以要声明另外一个流
                    MemoryStream excelMemoryStream = new MemoryStream(fileBytes);
                    //将内存流转为工作薄
                    NPOI.SS.UserModel.IWorkbook workbook = new NPOI.HSSF.UserModel.HSSFWorkbook(excelMemoryStream);

                    //判断是否存在工作表
                    if (workbook.NumberOfSheets > 0)
                    {
                        //获取出第一个工作表
                        NPOI.SS.UserModel.ISheet sheet = workbook.GetSheetAt(0);
                        //PhysicalNumberOfRows 是物理行的行数 ，不包含空行
                        //判断工作表是否存在行
                        if (sheet.PhysicalNumberOfRows > 0)
                        {
                            //============将数据保存到DataTable中
                            //定义DataTable
                            DataTable dtExcel = new DataTable();

                            //获取Excel中的标题行，设置dataTable的列名 第二行为标题行，索引为1
                            NPOI.SS.UserModel.IRow rowHeader = sheet.GetRow(1);
                            /*
                              FirstCellNum：获取某行第一个单元格下标
                              LastCellNum：获取某行的列数 ！！！！！
                              FirstRowNum：获取第一(个实际)行的下标
                              LastRowNum：获取最后一(个实际)行的下标
                            */
                            //获得表格的列数
                            int cellCount = rowHeader.LastCellNum;
                            //获取表格的行数
                            int rowCount = sheet.LastRowNum + 1;
                            //创建DataTable中的列
                            for (int i = rowHeader.FirstCellNum; i < cellCount; i++)
                            {
                                //通过遍历中的每一个单元格,获取标题行的各个单元格的数据
                                DataColumn dtCol = new DataColumn(rowHeader.GetCell(i).StringCellValue.Trim());
                                //把列添加到DataTable中
                                dtExcel.Columns.Add(dtCol);
                            }

                            //读取Excel中的数据
                            //(sheet.FirstRowNum+2) 第一行是说明行 第二行是标题行，第三行开始才是数据
                            for (int i = sheet.FirstRowNum + 2; i < rowCount; i++)
                            {
                                //获取行
                                NPOI.SS.UserModel.IRow row = sheet.GetRow(i);
                                //DataTable中创建一行
                                DataRow dtRow = dtExcel.NewRow();
                                //遍历行中列获取数据
                                if (row != null)
                                {
                                    for (int j = row.FirstCellNum; j < cellCount; j++)
                                    {
                                        if (row.GetCell(j) != null)
                                        {
                                            dtRow[j] = row.GetCell(j).ToString();
                                        }
                                    }

                                }
                                //将一行的数据添加到Datatable中
                                dtExcel.Rows.Add(dtRow);
                            }
                            //移除掉DataTable中的空行
                            removeEmptyRow(dtExcel);
                            //===将dataTable中的数据转换为List<S_User>
                            //存放所有的用户 包括数据库和添加的 --用于判断工号是否重复
                            List<S_User> allUsers = (from tabUser in myModel.S_User
                                                     select tabUser).ToList();
                            //查询出所有用户组 和用户角色
                            List<S_UserGroup> userGroups = (from tabUserGroup in myModel.S_UserGroup
                                                            select tabUserGroup).ToList();
                            List<S_UserType> userTypes = (from tabUserType in myModel.S_UserType
                                                          select tabUserType).ToList();
                            //定义存放容器
                            List<S_User> saveUsers = new List<S_User>();
                            //遍历datatable中的数据
                            for (int i = 0; i < dtExcel.Rows.Count; i++)
                            {
                                try
                                {
                                    DataRow dr = dtExcel.Rows[i];
                                    //创建一个S_User实例保存一条用户数据
                                    S_User addUser = new S_User();

                                    //1-用户组号 根据用户组号查询用户组ID
                                    string userGroupNumber = dr["用户组号"].ToString().Trim();
                                    addUser.userGroupID = userGroups.Single(o => o.userGroupNumber == userGroupNumber).userGroupID;
                                    //2-角色
                                    string userType = dr["角色"].ToString().Trim();
                                    addUser.userTypeID = userTypes.Single(o => o.userType == userType).userTypeID;
                                    //3-工号
                                    string jobNumber = dr["工号"].ToString().Trim();
                                    int oldCount = allUsers.Count(o => o.jobNumber == jobNumber);
                                    if (oldCount > 0)
                                    {
                                        msg.Text = "第" + (i + 1) + "条数据的工号:[" + jobNumber + "]重复，请检查";
                                        return Json(msg, JsonRequestBehavior.AllowGet);
                                    }
                                    addUser.jobNumber = jobNumber;
                                    //4-姓名
                                    string userName = dr["姓名"].ToString().Trim();
                                    if (string.IsNullOrEmpty(userName))
                                    {
                                        msg.Text = "第" + (i + 1) + "条数据的 姓名 未填写，请检查";
                                        return Json(msg, JsonRequestBehavior.AllowGet);
                                    }
                                    addUser.userName = userName;
                                    //4-余额
                                    string amount = dr["余额"].ToString().Trim();
                                    if (string.IsNullOrEmpty(amount) || !Regex.IsMatch(amount, "^\\+?[0-9]{1,6}(\\.[0-9]{1,2})?$"))
                                    {
                                        msg.Text = "第" + (i + 1) + "条数据的余额不正确，请检查";
                                        return Json(msg, JsonRequestBehavior.AllowGet);
                                    }
                                    addUser.amount = Convert.ToDecimal(amount);
                                    //5-密码
                                    string userPassword = dr["密码"].ToString().Trim();
                                    addUser.userPassword = AESEncryptHelper.Encrypt(userPassword);
                                    //6-email
                                    string email = dr["email"].ToString().Trim();
                                    if (string.IsNullOrEmpty(email) ||
                                        !Regex.IsMatch(email, "^([a-zA-Z]|[0-9])(\\w|\\-)+@[a-zA-Z0-9]+\\.([a-zA-Z]{2,4})$"))
                                    {

                                        msg.Text = "第" + (i + 1) + "条数据的邮箱格式不正确，请检查";
                                        return Json(msg, JsonRequestBehavior.AllowGet);
                                    }
                                    addUser.userEmail = email;
                                    //7-是否开通
                                    string strEnable = dr["是否开通"].ToString().Trim();
                                    if (strEnable != "是" && strEnable != "否")
                                    {
                                        msg.Text = "第" + (i + 1) + "条数据的 是否开通列 值只能填写“是”或者“否”，请检查";
                                        return Json(msg, JsonRequestBehavior.AllowGet);
                                    }
                                    addUser.isEnable = strEnable == "是";

                                    //添加到要保存的列表saveUsers
                                    saveUsers.Add(addUser);
                                    //添加到用于查重的列表allUsers
                                    allUsers.Add(addUser);
                                }
                                catch (Exception e)
                                {
                                    Console.Write(e);
                                    msg.Text = "第" + (i + 1) + "条数据不正确，请检查";
                                    return Json(msg, JsonRequestBehavior.AllowGet);
                                }
                            }
                            //===========保存数据
                            if (saveUsers.Count > 0)
                            {
                                using (TransactionScope scope = new TransactionScope())
                                {
                                    try
                                    {
                                        foreach (S_User saveUser in saveUsers)
                                        {
                                            //保存用户数据
                                            myModel.S_User.Add(saveUser);
                                            myModel.SaveChanges();
                                            //获取保存后的用户ID
                                            int tUserId = saveUser.userID;
                                            //===再保存 虚拟账户数据
                                            S_VirtualAccount virtualAccount = new S_VirtualAccount();
                                            virtualAccount.userID = tUserId;//设置用户ID
                                            virtualAccount.accountBalance = saveUser.amount;//设置账户余额;
                                            //virtualAccount.account = string.Format("XNZH{0:000000000}", userID);//设置虚拟账号
                                            virtualAccount.account = string.Format("XNZH{0:000000000}", tUserId);//设置虚拟账户

                                            myModel.S_VirtualAccount.Add(virtualAccount);
                                            myModel.SaveChanges();
                                        }
                                        //提交事务
                                        scope.Complete();
                                        msg.State = true;
                                        msg.Text= "数据导入成功，成功导入" + saveUsers.Count() + "条用户数据";
                                    }
                                    catch (Exception e)
                                    {
                                        Console.Write(e);
                                        msg.Text = "数据导入保存失败";
                                        return Json(msg, JsonRequestBehavior.AllowGet);
                                    }
                                }
                            }
                            else
                            {
                                msg.Text = "导入失败,请检查是否填写数据！";
                            }
                        }
                        else
                        {
                            msg.Text = "导入失败,请检查是第一个工作表中是否存在数据！";
                        }

                    }
                    else
                    {
                        msg.Text = "上传的Excel文件中不存在工作表，请检查";
                    }
                }
                else {
                    msg.Text = "请上传Excel类型文件(.xls)";
                }



            }
            catch (Exception e)
            {
                Console.Write(e);
                msg.Text = "数据异常";
            }
            return Json(msg, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 删除空行
        /// </summary>
        /// <param name="dt"></param>
        private void removeEmptyRow(DataTable dt)
        {
            //存放需要移除的DataRow
            List<DataRow> removeList = new List<DataRow>();
            //遍历所有的行
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                bool rowDataIsEmpty = true;//标识是否是空行-默认为空行
                //遍历DataRow的所有列
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    //判断数据是否为空
                    if (!string.IsNullOrEmpty(dt.Rows[i][j].ToString().Trim()))
                    {
                        rowDataIsEmpty = false;
                    }
                }
                //如果是空行，添加到removeList
                if (rowDataIsEmpty)
                {
                    removeList.Add(dt.Rows[i]);
                }
            }
            //移除掉空行
            for (int i = 0; i < removeList.Count; i++)
            {
                dt.Rows.Remove(removeList[i]);
            }
        }

        #endregion

        #region 导出用户数据1

        public ActionResult ExportLKDATA()
        {
            try
            {
                #region 查询需要导出的数据

                var varQuery = from tabUser in myModel.S_User
                                   //连接用户组表 通过用户组ID
                               join tabUserGroup in myModel.S_UserGroup
                               on tabUser.userGroupID equals tabUserGroup.userGroupID
                               //连接角色 通过角色ID
                               join tabUserType in myModel.S_UserType
                               on tabUser.userTypeID equals tabUserType.userTypeID
                               //连接虚拟账户表 通过用户ID
                               join tabVirtualAccount in myModel.S_VirtualAccount
                               on tabUser.userID equals tabVirtualAccount.userID
                               orderby tabUser.userID
                               select new UserVo()
                               {
                                   userID = tabUser.userID,
                                   userTypeID = tabUser.userTypeID,
                                   userGroupID = tabUser.userGroupID,
                                   jobNumber = tabUser.jobNumber,
                                   userName = tabUser.userName,
                                   userEmail = tabUser.userEmail,
                                   isEnable = tabUser.isEnable,
                                   userPicture = tabUser.userPicture,//用户头像图片名称
                                                             //扩展
                                   userGroupNumber = tabUserGroup.userGroupNumber,
                                   userType = tabUserType.userType,
                                   accountBalance = tabVirtualAccount.accountBalance.Value,
                               };
                List<UserVo> list = varQuery.ToList();

                #endregion

                #region NPOI导出Excel

                //1-创建工作薄
                NPOI.SS.UserModel.IWorkbook workbook = new NPOI.HSSF.UserModel.HSSFWorkbook();
                //2-创建工作表
                //NPOI.SS.UserModel.ISheet sheet = workbook.CreateSheet("用户信息");
                NPOI.SS.UserModel.ISheet sheet = workbook.CreateSheet();
                workbook.SetSheetName(0, "用户信息");//修改工作表名称
                //3-创建标题行
                NPOI.SS.UserModel.IRow rowTitle = sheet.CreateRow(0);
                rowTitle.HeightInPoints = 35;//设置行高
                //3.1创建单元格
                NPOI.SS.UserModel.ICell cell0 = rowTitle.CreateCell(0);
                //单元格设置值
                cell0.SetCellValue("用户信息");
                //3.2合并单元格
                sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(0, 0, 0, 6));/*firstRow,lastRow,firstCol,lastCol*/
                //3.3设置单元格样式
                NPOI.SS.UserModel.ICellStyle cellStyle_Title = workbook.CreateCellStyle();//声明单元格样式
                cellStyle_Title.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;//水平居中
                cellStyle_Title.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
                NPOI.SS.UserModel.IFont font_Title = workbook.CreateFont();//声明字体
                font_Title.Color = NPOI.HSSF.Util.HSSFColor.Blue.Index;//设置字体颜色
                font_Title.Boldweight = (short)NPOI.SS.UserModel.FontBoldWeight.Bold;//加粗体
                font_Title.FontHeightInPoints = 18;//设置字体大小
                cellStyle_Title.SetFont(font_Title);//设置单元格字体

                cell0.CellStyle = cellStyle_Title;//设置单元格样式

                //4-设置表头行
                NPOI.SS.UserModel.IRow rowHeader = sheet.CreateRow(1);///给sheet添加第一行的表头行
                rowHeader.HeightInPoints = 22;//设置行高
                                              //创建单元格,并设置值
                rowHeader.CreateCell(0).SetCellValue("序号");
                rowHeader.CreateCell(1).SetCellValue("用户组");
                rowHeader.CreateCell(2).SetCellValue("角色");
                rowHeader.CreateCell(3).SetCellValue("工号");
                rowHeader.CreateCell(4).SetCellValue("用户名");
                rowHeader.CreateCell(5).SetCellValue("邮箱");
                rowHeader.CreateCell(6).SetCellValue("余额");
                //rowHeader.CreateCell(7).SetCellValue("头像");
                //rowHeader.CreateCell(8).SetCellValue("是否启用");
                //rowHeader.CreateCell(9).SetCellValue("操作");
                //创建表头的样式
                NPOI.SS.UserModel.ICellStyle cellStyle_header = workbook.CreateCellStyle();
                cellStyle_header.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;//水平居中
                cellStyle_header.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;//垂直居中
                //设置背景颜色
                cellStyle_header.FillPattern = NPOI.SS.UserModel.FillPattern.SolidForeground;
                cellStyle_header.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Aqua.Index;
                //设置边框实线
                cellStyle_header.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
                cellStyle_header.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
                cellStyle_header.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
                cellStyle_header.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
                //循坏给单元格附加样式
                for (int i = 0; i < rowHeader.Cells.Count; i++)
                {
                    rowHeader.GetCell(i).CellStyle = cellStyle_header;
                }

                //5.遍历查询到的数据,设置表格数据
                //5.1-创建数据内部部分 单元格样式
                NPOI.SS.UserModel.ICellStyle cellstyle_value = workbook.CreateCellStyle();//声明样式
                cellstyle_value.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;//水平居中
                cellstyle_value.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;//垂直居中
                //设置边框实线
                cellstyle_value.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
                cellstyle_value.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
                cellstyle_value.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
                cellstyle_value.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
                //5.2-遍历数据,创建数据部分行列
                for (int i = 0; i < list.Count; i++)
                {
                    string AccountBalance = list[i].accountBalance.ToString();
                    NPOI.SS.UserModel.IRow row = sheet.CreateRow(2 + i);
                    row.Height = 22 * 20;//设置行高
                                         //创建单元格 并赋值
                    row.CreateCell(0).SetCellValue(i + 1);
                    row.CreateCell(1).SetCellValue(list[i].userGroupNumber);
                    row.CreateCell(2).SetCellValue(list[i].userType);
                    row.CreateCell(3).SetCellValue(list[i].jobNumber);
                    row.CreateCell(4).SetCellValue(list[i].userName);
                    row.CreateCell(5).SetCellValue(list[i].userEmail);
                    row.CreateCell(6).SetCellValue(AccountBalance);
                    //循坏给每一个单元格设置样式
                    for (int j = 0; j < row.Cells.Count; j++)
                    {
                        row.GetCell(j).CellStyle = cellstyle_value;
                    }
                }

                //6-设置列宽为自适应
                for (int i = 0; i < sheet.GetRow(1).Cells.Count; i++)
                {
                    sheet.AutoSizeColumn(i);
                    sheet.SetColumnWidth(i, sheet.GetColumnWidth(i) * 17 / 10);
                }
                //7-把创建好的Excel输出到浏览器
                string fileName = "用户信息" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-ffff") + ".xls";
                //把Excel转化为流输出
                MemoryStream BookStream = new MemoryStream();//定义流
                workbook.Write(BookStream);//将工作薄写入流
                BookStream.Seek(0, SeekOrigin.Begin);//输出之前调用Seek(偏移量,游标位置)
                return File(BookStream, "application/vnd.ms-excel", fileName);
                #endregion
            }
            catch (Exception e)
            {
                Console.Write(e);
                return Content("数据导出异常");
            }
            
        }

        #endregion

        #region 导出用户数据2

        
        public ActionResult ExportUserDATA()
        {
            try
            {
                #region 查询出需要导出的数据
                var varQuery = from tabUser in myModel.S_User
                                   //连接用户组表 通过用户组ID
                               join tabUserGroup in myModel.S_UserGroup
                               on tabUser.userGroupID equals tabUserGroup.userGroupID
                               //连接角色 通过角色ID
                               join tabUserType in myModel.S_UserType
                               on tabUser.userTypeID equals tabUserType.userTypeID
                               //连接虚拟账户表 通过用户ID
                               join tabVirtualAccount in myModel.S_VirtualAccount
                               on tabUser.userID equals tabVirtualAccount.userID
                               orderby tabUser.userID
                               select new UserVo()
                               {
                                   userID = tabUser.userID,
                                   userTypeID = tabUser.userTypeID,
                                   userGroupID = tabUser.userGroupID,
                                   jobNumber = tabUser.jobNumber,
                                   userName = tabUser.userName,
                                   userEmail = tabUser.userEmail,
                                   isEnable = tabUser.isEnable,
                                   userPicture = tabUser.userPicture,//用户头像图片名称
                                                             //扩展
                                   userGroupNumber = tabUserGroup.userGroupNumber,
                                   userType = tabUserType.userType,
                                   accountBalance = tabVirtualAccount.accountBalance.Value,
                               };
                List<UserVo> list = varQuery.ToList();

                #endregion

                #region 根据已有模板导入数据

                //1-检查模板文件是否存在
                // Server.MapPath 将相对的路径转为实际的物理路径
                string templatePath = Server.MapPath("~/Document/USERDataTemplate.xls");
                if (!System.IO.File.Exists(templatePath))
                {
                    //如果不存在,就返回失败信息
                    return Content("导出失败,请联系网站管理人员");
                }
                //==2-使用NPOI打开模板Excel
                //=2.1-使用文件打开模板文件
                FileStream templateStream = System.IO.File.Open(templatePath, FileMode.Open);
                //=2.2-使用NPOI打开模板Excel 得到一个工作簿
                NPOI.HSSF.UserModel.HSSFWorkbook excelBookTemplate = new NPOI.HSSF.UserModel.HSSFWorkbook(templateStream);
                //==3-打开模板所在第一个工作表
                NPOI.SS.UserModel.ISheet sheet = excelBookTemplate.GetSheetAt(0);
                //构建单元格样式
                NPOI.SS.UserModel.ICellStyle style = excelBookTemplate.CreateCellStyle();
                //==4-往模板中填充数据
                style.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                style.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
                //设置实线边框
                style.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
                style.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
                style.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
                style.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;

                //填充数据
                int index = 2;
                for (int i = 0; i <list.Count; i++)
                {
                    NPOI.SS.UserModel.IRow row = sheet.CreateRow(index);
                    row.HeightInPoints = 22;
                    //设置单元格数据
                    row.CreateCell(0).SetCellValue(i + 1);
                    row.CreateCell(1).SetCellValue(list[i].userGroupNumber);
                    row.CreateCell(2).SetCellValue(list[i].userType);
                    row.CreateCell(3).SetCellValue(list[i].jobNumber);
                    row.CreateCell(4).SetCellValue(list[i].userName);
                    row.CreateCell(5).SetCellValue(list[i].userEmail);
                    row.CreateCell(6).SetCellValue(list[i].accountBalance.ToString());
                    for (int j = 0; j < row.Cells.Count; j++)
                    {
                        row.GetCell(j).CellStyle = style;
                    }
                    index++;
                }
                //设置列宽为自适应
                for (int i = 0; i < sheet.GetRow(1).Cells.Count; i++)
                {
                    sheet.AutoSizeColumn(i);
                    sheet.SetColumnWidth(i, sheet.GetColumnWidth(i) * 17 / 10);
                }
                //以流的方式返回
                string fileName = "用户信息" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-ffff") + ".xls";
                //把Excel转化为流,输出
                MemoryStream BookStream = new MemoryStream();//定义内存流
                excelBookTemplate.Write(BookStream);//将工作薄写入流中
                BookStream.Seek(0, SeekOrigin.Begin);
                return File(BookStream, "application/vnd.ms-excel",fileName);
                
                #endregion
            }
            catch (Exception e)
            {
                Console.Write(e);
                return Content("数据导出异常");
            }
        }

        #endregion
    }
}