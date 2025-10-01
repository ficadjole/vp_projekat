using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Events
{
    public class TransferEventArgs : EventArgs
    {
        public DateTime TimestampUtc { get; set; }
        public string Message { get; set; }
        public MetaZaglavlje Meta { get; set; }
        public string SessionID { get; set; }
        
    }
}
