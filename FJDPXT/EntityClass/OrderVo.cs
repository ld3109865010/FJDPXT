using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FJDPXT.Model;

namespace FJDPXT.EntityClass
{
    public class OrderVo : B_Order
    {
        /// <summary>
        /// PNR编号
        /// </summary>
        public string PNRNo { get; set; }
        /// <summary>
        /// 用户组ID
        /// </summary>
        public int userGroupID { get; set; }

        /// <summary>
        /// 用户组编号
        /// </summary>
        public string userGroupNumber { get; set; }

        /// <summary>
        /// 工号
        /// </summary>
        public string jobNumber { get; set; }

        /// <summary>
        /// 虚拟账户账号
        /// </summary>
        public string account { get; set; }
    }
}