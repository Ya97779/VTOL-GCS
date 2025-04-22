using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMI
{
    public class Error_Status
    {
        public Dictionary<int, string> StatusCodeDic = new Dictionary<int, string>
        {
               { 0, "待机，空闲" },
               { 16, "待机，设备单独控制" },
               { 32, "待机，各设备自动自检" },
               { 48, "待机，修改配置参数" },
               { 64, "待机，读取外部 nash" },
               { 1, "起动准备就绪" },
               { 114, "起动点火，电点火预热" },
               { 130, "起动点火，电机带转" },
               { 146, "起动点火，开起动油阀" },
               { 162, "起动点火，开主油阀" },
               { 178, "起动点火，关起动油阀" },
               { 194, "起动点火，关电机" },
               { 210, "起动点火，开环加速" },
               { 226, "起动点火，闭环加速" },
               { 3, "发动机正常运行" },
               { 13, "停车冷却" },
               { 15, "急停，无法跳出!只能重新上电" },
               { 4, "发动机处于仿真状态" },


        };

        public Dictionary<int, string> ErrorsCodeDic = new Dictionary<int, string>
        {
               { 1, "起动超时 未到开辅油阀转速" },
               { 2, "起动超时 未到辅油引燃温度" },
               { 3, "起动超时 未到开主油阀转速" },
               { 4, "起动超时 未到主油引燃温度" },
               { 5, "起动超时 未到停电机转速" },
               { 6, "起动过程超温" },
               { 7, "起动超时 未到关辅油阀转速" },
               { 8, "起动故障，电点火电流低" },
               { 9, "起动过程电流过大" },
               { 12, "错误，转速丢失" },
               { 13, "冷却过程电流过大" },
               { 21, "电池超压" },
               { 22, "电池欠压" },
               { 33, "错误，通信丢失" },



        };



        // 根据错误码获取错误信息
        public string GetStatus(Dictionary<int, string> stateDict, int statusCode)
        {
            // 使用 ContainsKey 和索引器作为 TryGetValue 的替代方案  
            if (stateDict.ContainsKey(statusCode))
            {
                return stateDict[statusCode];
            }
            else
            {
                return "Unknown status";
            }
        }

        
    }
}
