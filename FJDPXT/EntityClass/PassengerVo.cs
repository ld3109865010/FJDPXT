using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FJDPXT.Model;

namespace FJDPXT.EntityClass
{
    public class PassengerVo : B_PNRPassenger
    {
        /// <summary>
        /// 常旅客号
        /// </summary>
        public string frequentPassengerNo { get; set; }

        /// <summary>
        /// 旅客类型
        /// </summary>
        public string passengerType { get; set; }

        /// <summary>
        /// 证件类型
        /// </summary>

        public string certificatesType { get; set; }


        public string contactName { get; set; }

        public string contactPhone { get; set; }

        public DateTime createTime { get; set; }
    }
}