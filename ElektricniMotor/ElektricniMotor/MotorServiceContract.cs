using Common;
using ElektricniMotor.Analytics;
using ElektricniMotor.Events;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace ElektricniMotor
{
    public class MotorServiceContract : IMotorServiceContract
    {
        private bool aktivnaSesija = false;
        private string sessionId;
        private int sampleIndex;
        private MetaZaglavlje last;
        private double runningMeanSpeed;
        private int runningCount;
        private bool badMode;
        private FileWriter writer;
        private readonly List<MetaZaglavlje> sessionMeta = new List<MetaZaglavlje>();
        private readonly ConfigRead conRead = ConfigRead.LoadFromConfig();

        public delegate void TransferEventHandler(object sender, TransferEventArgs e);
        public delegate void WarningEventHandler(object sender, WarningEventArgs e);

        public event EventHandler<TransferEventArgs> OnTransferStarted;
        public event EventHandler<MetaEventArgs> OnSampleReceived;
        public event EventHandler<TransferEventArgs> OnTransferCompleted;
        public event EventHandler<WarningEventArgs> OnWarningRaised;
        public event EventHandler<VoltageSpikeEventArgs> OnVoltageSpiked;
        public event EventHandler<SpeedSpikedEventArgs> OnSpeedSpiked;

        public MotorServiceContract()
        {
            OnTransferStarted += (_, e) => Console.WriteLine($"[SRV] Start: {e.SessionID}");
            OnSampleReceived += (_, e) => Console.WriteLine($"[SRV] Sample RX");
            OnTransferCompleted += (_, e) => Console.WriteLine($"[SRV] End: {e.SessionID} | {e?.TimestampUtc:HH:mm:ss} {e?.Message}");
            OnWarningRaised += (_, e) => Console.WriteLine($"[SRV][WARN] {e.WarningType} - {e.Message}");
        }

        private static double DevFraction(double percent) => percent > 1.0 ? percent / 100.0 : percent;

        public string EndSession()
        {
            if (!aktivnaSesija)
                return "NACK: No active session";

            aktivnaSesija = false;

            writer?.Dispose();
            writer = null;

            OnTransferCompleted?.Invoke(this, new TransferEventArgs
            {
                SessionID = sessionId,
                TimestampUtc = DateTime.UtcNow,
                Message = $"Completed, samples={sampleIndex}",
                Meta = sessionMeta.Count > 0 ? sessionMeta[0] : null
            });

            sessionId = null;
            return "ACK: session is completed";
        }

        public string PushSample(MetaZaglavlje metaZaglavlje)
        {
            try
            {
                if (!aktivnaSesija)
                    return "NACK: Session is not active";

                if (metaZaglavlje == null)
                    throw new FaultException<DataFormatFault>(new DataFormatFault("metaZaglavlje is null"));

                Console.WriteLine("->Transmission in progress");

                if (metaZaglavlje.Profile_Id == -1)
                {
                    badMode = true;
                    writer.SampleWrite(metaZaglavlje);
                    return "ACK: flag received";
                }

                writer.SampleWrite(metaZaglavlje);

                if (sampleIndex == 0)
                {
                    runningMeanSpeed = metaZaglavlje.Motor_speed;
                    last = metaZaglavlje;
                    sampleIndex = 1;
                    runningCount = 1;

                    OnSampleReceived?.Invoke(this, new MetaEventArgs
                    {
                        TimestampUtc = DateTime.UtcNow
                    });

                    return "ACK: sample accepted";
                }

                if (!badMode)
                {
                    if (last != null)
                    {
                        // ΔUq
                        var dUq = metaZaglavlje.U_q - last.U_q;
                        var aUq = Math.Abs(dUq);
                        if (aUq > conRead.Uq_threshold)
                        {
                            OnVoltageSpiked?.Invoke(this, new VoltageSpikeEventArgs
                            {
                                U = "U_q",
                                Direction = dUq < 0 ? Directions.Below : Directions.Above,
                                Delta = dUq,
                                AbsDelta = aUq,
                                Threshold = conRead.Uq_threshold,
                                Current = metaZaglavlje,
                                Previous = last,
                                TimeV = DateTime.UtcNow
                            });
                        }

                        // ΔUd
                        var dUd = metaZaglavlje.U_d - last.U_d;
                        var aUd = Math.Abs(dUd);
                        if (aUd > conRead.Ud_threshold)
                        {
                            OnVoltageSpiked?.Invoke(this, new VoltageSpikeEventArgs
                            {
                                U = "U_d",
                                Direction = dUd < 0 ? Directions.Below : Directions.Above,
                                Delta = dUd,
                                AbsDelta = aUd,
                                Threshold = conRead.Ud_threshold,
                                Current = metaZaglavlje,
                                Previous = last,
                                TimeV = DateTime.UtcNow
                            });
                        }

                        // ΔSpeed
                        var dV = metaZaglavlje.Motor_speed - last.Motor_speed;
                        var aV = Math.Abs(dV);
                        if (aV > conRead.Speed_threshold)
                        {
                            OnSpeedSpiked?.Invoke(this, new SpeedSpikedEventArgs
                            {
                                Direction = dV < 0 ? Directions.Below : Directions.Above,
                                Delta = dV,
                                AbsDelta = aV,
                                Threshold = conRead.Speed_threshold,
                                Current = metaZaglavlje,
                                Previous = last,
                                TimeS = DateTime.UtcNow
                            });
                        }
                    }

                    if (last != null)
                    {
                        var dSpeed = metaZaglavlje.Motor_speed - last.Motor_speed;
                        if (Math.Abs(dSpeed) > conRead.Speed_threshold)
                            OnWarningRaised?.Invoke(this, new WarningEventArgs
                            {
                                WarningType = "SpeedSpike",
                                Message = dSpeed > 0 ? "iznad očekivanog" : "ispod očekivanog",
                                Time = DateTime.Now,
                                Sample = metaZaglavlje,
                                CurValue = metaZaglavlje.Motor_speed,
                                ThresholdValue = conRead.Speed_threshold
                            });
                    }

                    runningMeanSpeed = (runningCount == 0)
                        ? metaZaglavlje.Motor_speed
                        : (runningMeanSpeed * runningCount + metaZaglavlje.Motor_speed) / (runningCount + 1);
                    runningCount++;

                    var dev = DevFraction(conRead.Deviation_threshold); 
                    var low = (1.0 - dev) * runningMeanSpeed;
                    var high = (1.0 + dev) * runningMeanSpeed;

                    if (metaZaglavlje.Motor_speed < low || metaZaglavlje.Motor_speed > high)
                        OnWarningRaised?.Invoke(this, new WarningEventArgs
                        {
                            WarningType = "OutOfBandWarning",
                            Message = metaZaglavlje.Motor_speed < low ? "ispod očekivane vrednosti" : "iznad očekivane vrednosti",
                            Time = DateTime.Now,
                            Sample = metaZaglavlje,
                            CurValue = metaZaglavlje.Motor_speed,
                            ThresholdValue = runningMeanSpeed
                        });
                }

                sampleIndex++;
                OnSampleReceived?.Invoke(this, new MetaEventArgs
                {
                    TimestampUtc = DateTime.UtcNow
                });

                last = metaZaglavlje;

                return "ACK: metaZaglavlje was received";
            }
            catch (FaultException) { throw; }
            catch (Exception ex)
            {
                Console.WriteLine("[SRV][UNHANDLED] " + ex);
                throw new FaultException<DataFormatFault>(new DataFormatFault("Server error: " + ex.Message));
            }
        }

        public string StartSession(MetaZaglavlje metaZaglavlje)
        {
            if (metaZaglavlje == null)
                throw new FaultException<DataFormatFault>(new DataFormatFault("metaZaglavlje is null"));

            new Validacija().ValidacijaEx(metaZaglavlje);

            aktivnaSesija = true;
            sessionId = Guid.NewGuid().ToString();
            sampleIndex = 0;
            badMode = false;
            last = null;
            runningMeanSpeed = 0;
            runningCount = 0;

            writer?.Dispose();
            writer = new FileWriter();

            sessionMeta.Clear();
            sessionMeta.Add(metaZaglavlje);

            OnTransferStarted?.Invoke(this, new TransferEventArgs
            {
                SessionID = sessionId,
                TimestampUtc = DateTime.UtcNow,
                Message = "Session started",
                Meta = metaZaglavlje
            });

            return "ACK: session is started";
        }
    }
}
