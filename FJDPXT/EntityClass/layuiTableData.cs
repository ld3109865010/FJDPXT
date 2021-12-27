using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FJDPXT.EntityClass
{
    //{ "code": 0,
    //  "msg": "",
    //  "count": 1000,
    //  "data": [{}, {}]
    public class LayuiTableData<T>
    {
        //状态
        public int code { get; set; }
        //消息
        public int msg { get; set; }
        //数据总条数
        public int count { get; set; }
        //数据
        public List<T> data { get; set; }
    }
}
