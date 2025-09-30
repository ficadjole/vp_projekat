using Common;
using System;

namespace ElektricniMotor.Events
{
    public class MetaEventArgs : EventArgs
    {
        public string SessionID { get; set; }
        public int Index { get; set; }            
        public MetaZaglavlje Sample { get; set; }
        public DateTime TimestampUtc { get; set; }
    }
}
