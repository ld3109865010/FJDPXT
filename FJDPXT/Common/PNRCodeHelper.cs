using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FJDPXT.Model;

namespace FJDPXT.Common
{
    public class PNRCodeHelper
    {
        //大写字母和数字
        private static char[] chars = new[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
            'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z','0',
            '1', '2', '3', '4', '5', '6', '7', '8', '9'};

        //存放Model的实例
        private static FJDPXTEntities1 myModel;

        /// <summary>
        /// 生成一个没有使用的PNR编号
        /// </summary>
        /// <returns></returns>
        public static string CreatePNR()
        {
            //一种懒加载 在需要使用的时候才去加载
            if (myModel==null)
            {
                myModel = new FJDPXTEntities1();
            }

            string strPnrNo = "";
            bool isExist = false;//存放生成的PNR是否存在在数据库
            do
            {
                //随机生成一个6位的PNR编号
                strPnrNo = CreatePNR6();
                //判断随机生成的PNR编号是否存在
                int count = myModel.B_PNR.Count(o => o.PNRNo.Trim() == strPnrNo);
                isExist = count > 0;
            } while (isExist);

            return strPnrNo;
        }


        /// <summary>
        /// 生成6位的随机字符串
        /// </summary>
        /// <returns></returns>
        private static string CreatePNR6()
        {
            string strPnr = "";
            //循环6次,每次随机从 大写字母数字中抽取一个
            Random random = new Random();
            for (int i = 0; i < 6; i++)
            {
                strPnr += chars[random.Next(0, chars.Length)];
            }

            return strPnr;
        }

    }
}