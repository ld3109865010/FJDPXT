using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FJDPXT.EntityClass
{
    public class XmSelectVo
    {
        public string name { get; set; }
        public int value { get; set; }
        public bool selected { get; set; }//是否选择
        public bool disabled { get; set; }//是否禁用
    }
}