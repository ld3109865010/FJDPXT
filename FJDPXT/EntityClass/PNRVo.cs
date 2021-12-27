using FJDPXT.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FJDPXT.EntityClass
{
    public class PNRVo : B_PNR
    {
        /// <summary>
        /// PNR下的旅客信息 列表
        /// </summary>
        public List<B_PNRPassenger> Passengers { get; set; }

        /// <summary>
        /// 航班号
        /// </summary>
        public string flightCode { get; set; }


        public DateTime? flightDate { get; set; }


        /// <summary>
        /// PNR状态
        /// </summary>
        public string strPNRStatus
        {
            get
            {
                switch (this.PNRStatus.Value)
                {
                    case 0:
                        return "已经取消";
                    case 1:
                        return "已经订座";
                    case 2:
                        return "部分出票";
                    case 3:
                        return "全部出票";
                    default:
                        return "";
                }
            }
        }


    }
}