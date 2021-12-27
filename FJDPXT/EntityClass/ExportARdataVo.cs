using FJDPXT.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FJDPXT.EntityClass
{
    public class ExportARdataVo : B_Order
    {
        /// <summary>
        /// 用户组号
        /// </summary>
        public string userGroup { get; set; }
        /// <summary>
        /// 用户工号
        /// </summary>
        public string jobNumber { get; set; }
        /// <summary>
        /// PNR
        /// </summary>
        public string PNR { get; set; }
        /// <summary>
        /// 支付日期
        /// </summary>        
        private string PayTimeStr;
        public string payTimeStr
        {
            get
            {
                return PayTimeStr;
            }
            set
            {
                try
                {
                    PayTimeStr = Convert.ToDateTime(value).ToString("yyyy-MM-dd hh:mm:ss");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    PayTimeStr = value;
                }
            }
        }
    }
}