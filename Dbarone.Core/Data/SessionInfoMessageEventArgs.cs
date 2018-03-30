using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dbarone.Data
{
    public enum InfoMessageType
    {
        INFO,
        ERROR
    }
    
    public class SessionInfoMessageEventArgs
    {
        public int Class { get; set; }
        public int LineNumber { get; set; }
        public string Message { get; set; }
        public int Number { get; set; }
        public string Procedure { get; set; }
        public string Server { get; set; }
        public string Source { get; set; }
        public int State { get; set; }

        public InfoMessageType GetInfoMessageType()
        {
            if (Class <= 10)
                return InfoMessageType.INFO;
            else
                return InfoMessageType.ERROR;
        }
    }

    public delegate void SessionInfoMessageEventHandler(object sender, SessionInfoMessageEventArgs e);

}
