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
    
    public partial class B_ETicketInvalid
    {
        public int ETicketInvalidID { get; set; }
        public Nullable<int> ETicketID { get; set; }
        public Nullable<decimal> agencyFee { get; set; }
        public Nullable<decimal> actualRefunfMoney { get; set; }
        public string InvalidCode { get; set; }
        public Nullable<int> operatorID { get; set; }
        public Nullable<System.DateTime> invalidTime { get; set; }
    }
}
