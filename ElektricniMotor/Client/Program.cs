using Common;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;

namespace Client
{
    public class Program
    {
        static IMotorServiceContract Reconnect(ChannelFactory<IMotorServiceContract> factory, IMotorServiceContract current)
        {
            try { ((IClientChannel)current)?.Abort(); } catch { }
            var fresh = factory.CreateChannel();
            ((IClientChannel)fresh).Open();
            WriteInfo(" Reconnected to server.");
            return fresh;
        }

        static void Main(string[] args)
        {
            Title("PMSM Motor Monitoring – Client");

            var factory = new ChannelFactory<IMotorServiceContract>("MotorServiceContractEndpoint");
            var proxy = factory.CreateChannel();

            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string csvRel = ConfigurationManager.AppSettings["CsvPath"] ?? @"Data\measures_v2.csv";
            string csvPath = Path.GetFullPath(Path.IsPathRooted(csvRel) ? csvRel : Path.Combine(exeDir, csvRel));

            int delayMs = 0;
            int.TryParse(ConfigurationManager.AppSettings["SampleDelayMs"], out delayMs);
            delayMs = Math.Max(0, delayMs);

            Directory.CreateDirectory(Path.GetDirectoryName(csvPath) ?? exeDir);
            WriteInfo($" CSV path: {csvPath}");
            WriteInfo($"  Delay per sample: {delayMs} ms");

            if (!File.Exists(csvPath))
            {
                WriteWarn("CSV NIJE pronađen. Proveri App.config (CsvPath) i svojstva fajla (Content/Copy).");
                Pause(); return;
            }

            using (var reader = new FileReader(csvPath))
            {
                var (podaci, losiPodaci) = reader.SamplesRead(csvPath);
                WriteInfo($" Parsed GOOD samples: {podaci.Count}");
                WriteInfo($"  Parsed BAD samples:  {losiPodaci.Count}");

                if (podaci.Count > 0)
                {
                    try
                    {
                        proxy.StartSession(podaci[0]);
                        WriteSuccess($"▶ Session started (seed from sample[0])");
                    }
                    catch (FaultException<ValidationFault> ex) { WriteErr($"Validation fault on start: {ex.Detail.Message}"); return; }
                    catch (FaultException<DataFormatFault> ex) { WriteErr($"Data format fault on start: {ex.Detail.Message}"); return; }
                    catch (CommunicationException ex) { WriteErr(ex.Message); }

                    Section(" Sending GOOD samples");
                    var swTotal = Stopwatch.StartNew();
                    int ack = 0, valFaults = 0, fmtFaults = 0, commReconnects = 0;

                    for (int i = 0; i < podaci.Count; i++)
                    {
                        var s = podaci[i];
                        try
                        {
                            var resp = proxy.PushSample(s);
                            ack++;
                            Console.WriteLine($"  [{i + 1}/{podaci.Count}]  ACK  | speed={s.Motor_speed:F3} | uq={s.U_q:F3} | ud={s.U_d:F3} | amb={s.Ambient:F2} | τ={s.Torque:F3}  →  {resp}");
                        }
                        catch (FaultException<ValidationFault> ex)
                        {
                            valFaults++;
                            Console.WriteLine($"  [{i + 1}/{podaci.Count}]  Validation fault: {ex.Detail.Message}");
                        }
                        catch (FaultException<DataFormatFault> ex)
                        {
                            fmtFaults++;
                            Console.WriteLine($"  [{i + 1}/{podaci.Count}]  Data format fault: {ex.Detail.Message}");
                        }
                        catch (TimeoutException ex)
                        {
                            commReconnects++;
                            WriteWarn("Timeout – trying reconnect: " + ex.Message);
                            proxy = Reconnect(factory, proxy);
                            i--;
                        }
                        catch (CommunicationException ex)
                        {
                            commReconnects++;
                            WriteWarn("Comm fault – trying reconnect: " + ex.Message);
                            proxy = Reconnect(factory, proxy);
                            i--; 
                        }

                        if (delayMs > 0) System.Threading.Thread.Sleep(delayMs);
                    }

                    swTotal.Stop();
                    Summary("GOOD summary", ("ACK", ack), ("Validation faults", valFaults), ("Data faults", fmtFaults), ("Reconnects", commReconnects), ("Elapsed", swTotal.Elapsed.ToString(@"mm\:ss\.fff")));

                    if (losiPodaci.Count > 0)
                    {
                        Section(" Sending BAD samples");
                        proxy.PushSample(new MetaZaglavlje(-1, 0, 0, 0, 0, 0));
                        if (delayMs > 0) System.Threading.Thread.Sleep(delayMs);

                        ack = 0; valFaults = 0; fmtFaults = 0; commReconnects = 0;
                        swTotal.Restart();

                        for (int i = 0; i < losiPodaci.Count; i++)
                        {
                            var s = losiPodaci[i];
                            try
                            {
                                var resp = proxy.PushSample(s);
                                ack++;
                                Console.WriteLine($"  [bad {i + 1}/{losiPodaci.Count}]  ACK  | speed={s.Motor_speed:F3} | uq={s.U_q:F3} | ud={s.U_d:F3} | amb={s.Ambient:F2} | τ={s.Torque:F3}  →  {resp}");
                            }
                            catch (FaultException<ValidationFault> ex)
                            {
                                valFaults++;
                                Console.WriteLine($"  [bad {i + 1}/{losiPodaci.Count}]  Validation fault: {ex.Detail.Message}");
                            }
                            catch (FaultException<DataFormatFault> ex)
                            {
                                fmtFaults++;
                                Console.WriteLine($"  [bad {i + 1}/{losiPodaci.Count}]  Data format fault: {ex.Detail.Message}");
                            }
                            catch (TimeoutException ex)
                            {
                                commReconnects++;
                                WriteWarn("Timeout – trying reconnect: " + ex.Message);
                                proxy = Reconnect(factory, proxy);
                                i--;
                            }
                            catch (CommunicationException ex)
                            {
                                commReconnects++;
                                WriteWarn("Comm fault – trying reconnect: " + ex.Message);
                                proxy = Reconnect(factory, proxy);
                                i--;
                            }

                            if (delayMs > 0) System.Threading.Thread.Sleep(delayMs);
                        }

                        swTotal.Stop();
                        Summary("BAD summary", ("ACK", ack), ("Validation faults", valFaults), ("Data faults", fmtFaults), ("Reconnects", commReconnects), ("Elapsed", swTotal.Elapsed.ToString(@"mm\:ss\.fff")));
                    }

                    // END
                    try
                    {
                        var endMsg = proxy.EndSession();
                        WriteSuccess($"  Session ended → {endMsg}");
                    }
                    catch (FaultException<ValidationFault> ex) { WriteErr($"Validation fault on end: {ex.Detail.Message}"); }
                    catch (FaultException<DataFormatFault> ex) { WriteErr($"Data format fault on end: {ex.Detail.Message}"); }
                }
            }

            WriteSuccess("✓ Client finished sending all data.");
            Pause();
        }

        static void Title(string text)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(new string('═', text.Length));
            Console.WriteLine(text);
            Console.WriteLine(new string('═', text.Length));
            Console.ResetColor();
        }

        static void Section(string text)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"\n{text}");
            Console.ResetColor();
        }

        static void WriteInfo(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        static void WriteWarn(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine( msg);
            Console.ResetColor();
        }

        static void WriteErr(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine( msg);
            Console.ResetColor();
        }

        static void WriteSuccess(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        static void Summary(string title, params (string label, object value)[] rows)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"\n— {title} —");
            Console.ResetColor();
            foreach (var r in rows)
                Console.WriteLine($"   • {r.label}: {r.value}");
        }

        static void Pause()
        {
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
