using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FJDPXT.EntityClass
{
    // layui table的分页信息
    public class LayuiTablePage
    {
        public int page { get; set; }

        public int limit { get; set; }


        // 获取要跳过的数据条数(同时也是要查询的数据的开始索引)
        public int GetStartIndex()
        {
            return (page - 1) * limit;
        }

        // 分页数据结束位置的索引
        public int GetEndIndex()
        {
            return page * limit - 1;
        }
    }
}