using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElektricniMotor.Events
{
    public class OutOfBandWarningEventArgs : EventArgs
    {
        public Directions Direction { get; set; }
        public double Speed { get; set; }
        public double RunningMean { get; set; }
        public double DeviationRatio { get; set; }
        public MetaZaglavlje Sample { get; set; }
        public DateTime TimeS { get; set; }
    }
}
