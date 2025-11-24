using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Envir
{
    /// <summary>
    /// lyo 2025-11-6 08:44:37
    /// </summary>
    public class ManagerDelegate
    {
        public delegate void SysLogMsgDelegate(string msg);
        public static event SysLogMsgDelegate? SysLogMsg;
        public static void PublishSysLogMsg(string msg)
        {
            SysLogMsg?.Invoke(msg);
        }

        public delegate void ChatLogMsgDelegate(string msg);
        public static event ChatLogMsgDelegate? ChatLogMsg;
        public static void PublishChatLogMsg(string msg)
        {
            ChatLogMsg?.Invoke(msg);
        }
    }
}
