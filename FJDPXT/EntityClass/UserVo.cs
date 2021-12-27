using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FJDPXT.Model;

namespace FJDPXT.EntityClass
{
    public class UserVo : S_User
    {
        public string userGroupNumber { get; set; }

        public string userType { get; set; } 

        public decimal accountBalance { get; set; }
    }
}