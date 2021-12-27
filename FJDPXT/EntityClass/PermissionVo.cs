using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FJDPXT.Model;

namespace FJDPXT.EntityClass
{
    public class PermissionVo : S_Permission
    {
        /// <summary>
        /// 角色名称
        /// </summary>
        public string userType { get; set; }
        /// <summary>
        /// 角色描述
        /// </summary>
        public string description { get; set; }
    }
}