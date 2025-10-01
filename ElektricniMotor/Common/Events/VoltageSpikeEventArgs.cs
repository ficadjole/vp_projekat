using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Events
{
    public class VoltageSpikeEventArgs : EventArgs
    {
        public string U { get; set; }   
        public Directions Direction { get; set; }
        public double Delta { get; set; }
        public double AbsDelta { get; set; }
        public double Threshold { get; set; }
        public MetaZaglavlje Current { get; set; }
        public MetaZaglavlje Previous { get; set; }
        public DateTime TimeV { get; set; }
    }
}
