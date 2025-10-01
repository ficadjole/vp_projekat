using Common;
using Common.Events;
using System;
using System.Collections.Generic;
using System.Configuration;  
using System.Globalization; 
using System.Linq;

namespace Common.Analytics
{
    public class SpeedReading
    {
        public long RowIndex { get; set; }
        public double Speed { get; set; }          
        public DateTime Timestamp { get; set; }
    }

    public class SpeedAnalyzer
    {
        private readonly double _speedThreshold;   
        private readonly double _devRatio;         
        private readonly List<SpeedReading> _history = new List<SpeedReading>();

        private long _n;                           
        private double _mean;                      

        public event EventHandler<SpeedSpikedEventArgs> SpeedSpike;
        public event EventHandler<OutOfBandWarningEventArgs> OutOfBandWarning;

        public SpeedAnalyzer()
        {
            var cfg = ConfigRead.LoadFromConfig();
            _speedThreshold = cfg.Speed_threshold;
            _devRatio = (cfg.Deviation_threshold > 1.0) ? cfg.Deviation_threshold / 100.0 : cfg.Deviation_threshold;

            Console.WriteLine($"SpeedAnalyzer initialized: SpeedTh={_speedThreshold}, Dev={_devRatio:P0}");
        }

        public SpeedAnalyzer(double speedThreshold, double deviationPercent = 25.0)
        {
            _speedThreshold = speedThreshold;
            _devRatio = (deviationPercent > 1.0) ? deviationPercent / 100.0 : deviationPercent;

            Console.WriteLine($"SpeedAnalyzer initialized (custom): SpeedTh={_speedThreshold}, Dev={_devRatio:P0}");
        }

        public void AnalyzeSpeedChanges(List<MetaZaglavlje> samples)
        {
            if (samples == null || samples.Count < 2)
            {
                Console.WriteLine("Insufficient samples for speed analysis");
                return;
            }

            Console.WriteLine($"\n=== Speed Analysis — count={samples.Count}, Th={_speedThreshold}, Dev={_devRatio:P0} ===");
            _history.Clear();
            _n = 0; _mean = 0;

            for (int i = 0; i < samples.Count; i++)
            {
                var s = samples[i];
                double vCurr = s.Motor_speed;

                _n++;
                if (_n == 1) _mean = vCurr;
                else _mean += (vCurr - _mean) / _n;

                _history.Add(new SpeedReading
                {
                    RowIndex = i,
                    Speed = vCurr,
                    Timestamp = DateTime.Now
                });

                if (i == 0)
                {
                    Console.WriteLine($"Sample {i}: Speed={vCurr:F2} (baseline)");
                    continue;
                }

                var prev = samples[i - 1];
                double vPrev = prev.Motor_speed;

                double dV = vCurr - vPrev;
                double aV = Math.Abs(dV);

                Console.WriteLine($"Sample {i}: Speed={vCurr:F2}, ΔSpeed={dV:F3} (|Δ|={aV:F3})");
                if (aV > _speedThreshold)
                    RaiseSpeedSpike(prev, s, dV, aV, _speedThreshold);

                double minBand = (1.0 - _devRatio) * _mean;
                double maxBand = (1.0 + _devRatio) * _mean;

                if (vCurr < minBand)
                    RaiseOutOfBand(s, Directions.Below, vCurr, _mean, _devRatio);
                else if (vCurr > maxBand)
                    RaiseOutOfBand(s, Directions.Above, vCurr, _mean, _devRatio);
            }

            LogSummary();
        }

        public void AnalyzeSingleSample(MetaZaglavlje sample)
        {
            double vCurr = sample.Motor_speed;

            var prev = _history.LastOrDefault();
            _n++;
            if (_n == 1) _mean = vCurr;
            else _mean += (vCurr - _mean) / _n;

            _history.Add(new SpeedReading
            {
                RowIndex = _history.Count,
                Speed = vCurr,
                Timestamp = DateTime.Now
            });

            if (prev == null)
            {
                Console.WriteLine($"Real-time baseline: Speed={vCurr:F2}");
                return;
            }

            var prevSample = new MetaZaglavlje(
                profile_id: sample.Profile_Id,
                u_q: sample.U_q,
                u_d: sample.U_d,
                motor_speed: prev.Speed,   
                ambient: sample.Ambient,
                torque: sample.Torque
            );

            double dV = vCurr - prev.Speed;
            double aV = Math.Abs(dV);
            Console.WriteLine($"Real-time: Speed={vCurr:F2}, ΔSpeed={dV:F3}");

            if (aV > _speedThreshold)
                RaiseSpeedSpike(prevSample, sample, dV, aV, _speedThreshold);

            double minBand = (1.0 - _devRatio) * _mean;
            double maxBand = (1.0 + _devRatio) * _mean;
            if (vCurr < minBand) RaiseOutOfBand(sample, Directions.Below, vCurr, _mean, _devRatio);
            else if (vCurr > maxBand) RaiseOutOfBand(sample, Directions.Above, vCurr, _mean, _devRatio);
        }

        private void RaiseSpeedSpike(MetaZaglavlje prev, MetaZaglavlje curr,
                                     double delta, double absDelta, double threshold)
        {
            var dir = DetermineDirection(delta);

            var e = new SpeedSpikedEventArgs
            {
                Previous = prev,
                Current = curr,
                Delta = delta,
                AbsDelta = absDelta,
                Threshold = threshold,
                Direction = dir,
                TimeS = DateTime.Now
            };

            Console.WriteLine($"\n⚡ SPEED SPIKE DETECTED");
            Console.WriteLine($"ΔSpeed = {delta:F3} (|Δ| = {absDelta:F3}) > Th={threshold:F3}; Dir={dir} @ {e.TimeS:HH:mm:ss.fff}\n");

            SpeedSpike?.Invoke(this, e);
        }

        private void RaiseOutOfBand(MetaZaglavlje sample, Directions dir,
                                    double v, double mean, double ratio)
        {
            var e = new OutOfBandWarningEventArgs
            {
                Direction = dir,
                Speed = v,
                RunningMean = mean,
                DeviationRatio = ratio,
                Sample = sample,
                TimeS = DateTime.Now
            };

            Console.WriteLine($"[OOB] Speed={v:F2} vs mean={mean:F2} (±{ratio:P0}) → {dir} @ {e.TimeS:HH:mm:ss.fff}");
            OutOfBandWarning?.Invoke(this, e);
        }

        private static Directions DetermineDirection(double delta)
        {
            if (delta > 0) return Directions.Above;
            if (delta < 0) return Directions.Below;
            return Directions.Same;
        }

        private void LogSummary()
        {
            if (_history.Count < 2) return;

            var vVals = _history.Select(h => h.Speed).ToList();
            var dV = new List<double>();
            for (int i = 1; i < _history.Count; i++)
                dV.Add(Math.Abs(_history[i].Speed - _history[i - 1].Speed));

            Console.WriteLine($"\n=== Speed Analysis Summary ===");
            Console.WriteLine($"Speed: min={vVals.Min():F2}, max={vVals.Max():F2}");
            Console.WriteLine($"Avg |ΔSpeed|={dV.Average():F3}, Max |ΔSpeed|={dV.Max():F3}, spikes={dV.Count(x => x > _speedThreshold)} (Th={_speedThreshold:F3})\n");
        }

        public void ClearHistory()
        {
            _history.Clear();
            _n = 0; _mean = 0;
            Console.WriteLine("Speed analysis history cleared");
        }

        public List<SpeedReading> GetHistory() => _history.ToList();
    }
}
