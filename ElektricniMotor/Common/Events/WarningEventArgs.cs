using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Events
{
    public class WarningEventArgs : EventArgs
    {
        public MetaZaglavlje Sample { get; set; }
        public string Message { get; set; }
        public string WarningType { get; set; }
        public double CurValue { get; set; }
        public double ThresholdValue { get; set; }
        public DateTime Time { get; set; }
    }
}
