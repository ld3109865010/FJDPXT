using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FJDPXT.Model;

namespace FJDPXT.EntityClass
{
    public class ETicketVo : B_ETicket
    {
        /// <summary>
        /// PNR旅客编号
        /// </summary>
        public int passengerNo { get; set; }

        /// <summary>
        /// 旅客姓名
        /// </summary>
        public string passengerName { get; set; }




        /// <summary>
        /// 票联状态
        /// </summary>
        public string eTicketStatus { get; set; }
        public string EnglishName { get; set; }
        /// <summary>
        /// 起飞日期
        /// </summary>
        private string FlightDate;
        public string flightDateStr
        {
            get
            {
                return FlightDate;
            }
            set
            {
                try
                {
                    FlightDate = Convert.ToDateTime(value).ToString("yyyy-MM-dd");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    FlightDate = value;
                }
            }
        }
        public Nullable<System.DateTime> flightDate { get; set; }

        /// <summary>
        ///起飞时间
        /// </summary>
        private string DepartureTime;
        public string departureTime
        {
            get
            {
                return DepartureTime;
            }
            set
            {
                try
                {
                    DepartureTime = Convert.ToDateTime(value).ToString("HH:mm");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    DepartureTime = value;
                }
            }
        }

        /// <summary>
        /// 出发城市Id
        /// </summary>
        public int orangeId { get; set; }
        /// <summary>
        /// 城市编码
        /// </summary>
        public string orangeCode { get; set; }
        /// <summary>
        /// 出发城市名称
        /// </summary>
        public string orangeName { get; set; }

        /// <summary>
        /// 到达城市-目的地Id
        /// </summary>
        public int destinationId { get; set; }
        /// <summary>
        /// 到达城市编码
        /// </summary>
        public string destinationCode { get; set; }
        /// <summary>
        /// 到达城市名称
        /// </summary>
        public string destinationName { get; set; }

        /// <summary>
        /// 舱位
        /// </summary>
        public string cabinTypeName { get; set; }
        /// <summary>
        /// 证件类型+号码
        /// </summary>
        public string certificate { get; set; }


        /// <summary>
        /// 发票状态
        /// </summary>
        public string invoiceStatus { get; set; }

        /// <summary>
        ///票价基础= 基础价格
        /// </summary>
        public Nullable<decimal> basisPrice { get; set; }

        /// <summary>
        /// 操作人
        /// </summary>
        public string userName { get; set; }


        /// <summary>
        ///操作时间
        /// </summary>
        private string OperatingTtime;
        public string peratingTtimes
        {
            get
            {
                return OperatingTtime;
            }
            set
            {
                try
                {
                    OperatingTtime = Convert.ToDateTime(value).ToString("yyyy-MM-dd HH:mm:ss");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    OperatingTtime = value;
                }
            }
        }
        public int PNRID { get; set; }

        /// <summary>
        /// PNR编号
        /// </summary>
        public string PNRNo { get; set; }

        /// <summary>
        ///出票日期
        /// </summary>
        private string ticketingTime;
        public string TicketingTime
        {
            get
            {
                return ticketingTime;
            }
            set
            {
                try
                {
                    ticketingTime = Convert.ToDateTime(value).ToString("yyyy-MM-dd");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    ticketingTime = value;
                }
            }
        }
        /// <summary>
        /// 工号
        /// </summary>
        public string JobNumber { get; set; }
        /// <summary>
        /// 旅客类型
        /// </summary>
        public string passengerType { get; set; }
        
        public string flightCode { get; set; }
        /// <summary>
        /// 证件类型ID
        /// </summary>
        public Nullable<int> certificatesTypeID { get; set; }
        /// <summary>
        /// 证件类型+号码
        /// </summary>
        public string certificatesType { get; set; }
        public string certificatesCode { get; set; }
        /// <summary>
        /// 航空公司记录编号
        /// </summary>
        public Nullable<int> segmentNo { get; set; }
        /// <summary>
        /// 定座记录编号
        /// </summary>
        public Nullable<int> seatNum { get; set; }

        public int PNRSegmentID { get; set; }
    }
}