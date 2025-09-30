using Common;
using ElektricniMotor.Events;
using System;
using System.Collections.Generic;
using System.Configuration;   // može ostati
using System.Globalization;   // može ostati
using System.Linq;

namespace ElektricniMotor.Analytics
{
    public class VoltageReading
    {
        public long RowIndex { get; set; }
        public double Uq { get; set; }
        public double Ud { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class VoltAnalyzer
    {
        private readonly double uqThreshold;
        private readonly double udThreshold;

        private readonly List<VoltageReading> voltageHistory = new List<VoltageReading>();

        public event EventHandler<VoltageSpikeEventArgs> VoltageSpike;



        public VoltAnalyzer()
        {
            var cfg = ConfigRead.LoadFromConfig();
            uqThreshold = cfg.Uq_threshold;
            udThreshold = cfg.Ud_threshold;

            Console.WriteLine($"VoltageAnalyzer initialized: UqTh={uqThreshold}, UdTh={udThreshold}");
        }

        public VoltAnalyzer(double uqThreshold, double udThreshold)
        {
            this.uqThreshold = uqThreshold;
            this.udThreshold = udThreshold;

            Console.WriteLine($"VoltageAnalyzer initialized (custom): UqTh={uqThreshold}, UdTh={udThreshold}");
        }

        public void AnalyzeVoltageChanges(List<MetaZaglavlje> samples)
        {
            if (samples == null || samples.Count < 2)
            {
                Console.WriteLine("Insufficient samples for voltage analysis");
                return;
            }

            Console.WriteLine($"\n=== Voltage Analysis (Uq/ Ud) — count={samples.Count}, UqTh={uqThreshold}, UdTh={udThreshold} ===");

            voltageHistory.Clear();

            for (int i = 0; i < samples.Count; i++)
            {
                var s = samples[i];

                var reading = new VoltageReading
                {
                    RowIndex = i,
                    Uq = s.U_q,
                    Ud = s.U_d,
                    Timestamp = DateTime.Now
                };
                voltageHistory.Add(reading);

                if (i == 0)
                {
                    Console.WriteLine($"Sample {i}: Uq={s.U_q:F4}, Ud={s.U_d:F4} (baseline)");
                    continue;
                }

                var prev = samples[i - 1];

                // ΔUq
                double dUq = s.U_q - prev.U_q;
                double aUq = Math.Abs(dUq);
                Console.WriteLine($"Sample {i}: Uq={s.U_q:F4}, ΔUq={dUq:F6} (|Δ|={aUq:F6})");
                if (aUq > uqThreshold) RaiseSpike(prev, s, "Uq", dUq, aUq, uqThreshold);

                // ΔUd
                double dUd = s.U_d - prev.U_d;
                double aUd = Math.Abs(dUd);
                Console.WriteLine($"           Ud={s.U_d:F4}, ΔUd={dUd:F6} (|Δ|={aUd:F6})");
                if (aUd > udThreshold) RaiseSpike(prev, s, "Ud", dUd, aUd, udThreshold);
            }

            LogSummary(samples);
        }

        // Real-time analiza (jedan sample po pozivu)
        public void AnalyzeSingleSample(MetaZaglavlje sample)
        {
            var previous = voltageHistory.LastOrDefault();

            var reading = new VoltageReading
            {
                RowIndex = voltageHistory.Count,
                Uq = sample.U_q,
                Ud = sample.U_d,
                Timestamp = DateTime.Now
            };
            voltageHistory.Add(reading);

            if (previous == null)
            {
                Console.WriteLine($"Real-time baseline: Uq={sample.U_q:F4}, Ud={sample.U_d:F4}");
                return;
            }

            var prevSample = new MetaZaglavlje(
                profile_id: sample.Profile_Id,
                u_q: previous.Uq,
                u_d: previous.Ud,
                motor_speed: sample.Motor_speed,
                ambient: sample.Ambient,
                torque: sample.Torque
            );

            // ΔUq
            double dUq = sample.U_q - previous.Uq;
            double aUq = Math.Abs(dUq);
            Console.WriteLine($"Real-time: Uq={sample.U_q:F4}, ΔUq={dUq:F6}");
            if (aUq > uqThreshold) RaiseSpike(prevSample, sample, "Uq", dUq, aUq, uqThreshold);

            // ΔUd
            double dUd = sample.U_d - previous.Ud;
            double aUd = Math.Abs(dUd);
            Console.WriteLine($"           Ud={sample.U_d:F4}, ΔUd={dUd:F6}");
            if (aUd > udThreshold) RaiseSpike(prevSample, sample, "Ud", dUd, aUd, udThreshold);
        }

        private void RaiseSpike(MetaZaglavlje prev, MetaZaglavlje curr, string component,
                                double delta, double absDelta, double threshold)
        {
            var dir = DetermineSpikeDirection(delta);

            var e = new VoltageSpikeEventArgs
            {
                Previous = prev,
                Current = curr,
                U = component,      
                Delta = delta,
                AbsDelta = absDelta,
                Threshold = threshold,
                Direction = dir,
                TimeV = DateTime.Now
            };

            Console.WriteLine($"\n ==============VOLTAGE SPIKE — {component}==========");
            Console.WriteLine($"From: {component}prev={(component == "Uq" ? prev.U_q : prev.U_d):F4}  → {component}curr={(component == "Uq" ? curr.U_q : curr.U_d):F4}");
            Console.WriteLine($"Δ{component} = {delta:F6} (|Δ| = {absDelta:F6}) > Th={threshold:F6}");
            Console.WriteLine($"Direction: {dir}  @ {e.TimeV:HH:mm:ss.fff}\n");

            VoltageSpike?.Invoke(this, e);
        }

        private static Directions DetermineSpikeDirection(double delta)
        {
            if (delta > 0) return Directions.Above;
            if (delta < 0) return Directions.Below;
            return Directions.Same;
        }

        private void LogSummary(List<MetaZaglavlje> samples)
        {
            if (samples == null || samples.Count < 2) return;

            var uq = samples.Select(s => s.U_q).ToList();
            var ud = samples.Select(s => s.U_d).ToList();

            var dUq = new List<double>();
            var dUd = new List<double>();

            for (int i = 1; i < samples.Count; i++)
            {
                dUq.Add(Math.Abs(samples[i].U_q - samples[i - 1].U_q));
                dUd.Add(Math.Abs(samples[i].U_d - samples[i - 1].U_d));
            }

            Console.WriteLine($"\n=== Voltage Analysis Summary (Uq / Ud) ===");
            Console.WriteLine($"Uq: min={uq.Min():F4}, max={uq.Max():F4}, avg|Δ|={dUq.Average():F6}, max|Δ|={dUq.Max():F6}, spikes={dUq.Count(x => x > uqThreshold)} (Th={uqThreshold:F6})");
            Console.WriteLine($"Ud: min={ud.Min():F4}, max={ud.Max():F4}, avg|Δ|={dUd.Average():F6}, max|Δ|={dUd.Max():F6}, spikes={dUd.Count(x => x > udThreshold)} (Th={udThreshold:F6})\n");
        }

        public void ClearHistory()
        {
            voltageHistory.Clear();
            Console.WriteLine("Voltage analysis history cleared");
        }

        public List<VoltageReading> GetVoltageHistory()
        {
            return voltageHistory.ToList();
        }
    }
}
