using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FJDPXT.Model;

namespace FJDPXT.EntityClass
{
    public class TickVo:S_Ticket
    {
        public string strStartTicketNo
        {
            get
            {
                return "E781-" + this.startTicketNo;
            }
        }
        public string strEndTicketNo
        {
            get
            {
                return "E781-" + this.endTicketNo;
            }
        }
        public string strCurrentTicketNo
        {
            get
            {
                return "E781-" + this.currentTicketNo;
            }
        }

        public string strTicketDate
        {
            get
            {
                return this.ticketDate.Value.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }


        public string userName { get; set; }

        public string jobNumber { get; set; }

        public string userGroupNumber { get; set; }
    }
}