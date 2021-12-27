using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FJDPXT.Model;

namespace FJDPXT.EntityClass
{
    public class ModuleVo : S_Module
    {
        /// <summary>
        /// 父模块
        /// </summary>
        public S_Module parentModule { get; set; }
    }
}