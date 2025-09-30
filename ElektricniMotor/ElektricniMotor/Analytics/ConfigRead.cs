using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElektricniMotor.Analytics
{
    public class ConfigRead
    {
        public double Uq_threshold { get; set; } = 50.0;
        public double Ud_threshold { get; set; } = 50.0;
        public double Speed_threshold { get; set; } = 200.0;
        public double Deviation_threshold { get; set; } = 25.0;

        public static ConfigRead LoadFromConfig()
        {
            var cfg = new ConfigRead();
            try
            {
   
                cfg.Uq_threshold = double.Parse(ConfigurationManager.AppSettings["Uq_threshold"] ?? "50.0");
                cfg.Ud_threshold = double.Parse(ConfigurationManager.AppSettings["Ud_threshold"] ?? "50.0");
                cfg.Speed_threshold = double.Parse(ConfigurationManager.AppSettings["Speed_threshold"] ?? "200.0");
                cfg.Deviation_threshold = double.Parse(ConfigurationManager.AppSettings["Deviation_threshold"] ??  ConfigurationManager.AppSettings["Deviation_threshold"] ??    "25.0");
                Console.WriteLine( $"Config: Uq={cfg.Uq_threshold}, Ud={cfg.Ud_threshold}, Speed={cfg.Speed_threshold}, Dev%={cfg.Deviation_threshold}%");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: analytics config load failed, using defaults. {ex.Message}");
            }
            return cfg;
        }
    }
}
