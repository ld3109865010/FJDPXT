//------------------------------------------------------------------------------
// <auto-generated>
//     此代码已从模板生成。
//
//     手动更改此文件可能导致应用程序出现意外的行为。
//     如果重新生成代码，将覆盖对此文件的手动更改。
// </auto-generated>
//------------------------------------------------------------------------------

namespace FJDPXT.Model
{
    using System;
    using System.Collections.Generic;
    
    public partial class S_User
    {
        public int userID { get; set; }
        public int userTypeID { get; set; }
        public string userName { get; set; }
        public int userGroupID { get; set; }
        public string jobNumber { get; set; }
        public string userPassword { get; set; }
        public string userEmail { get; set; }
        public Nullable<decimal> amount { get; set; }
        public Nullable<bool> isEnable { get; set; }
        public string userPicture { get; set; }
    }
}
