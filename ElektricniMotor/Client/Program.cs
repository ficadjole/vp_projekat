using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {

            ChannelFactory<IMotorServiceContract> factory = new ChannelFactory<IMotorServiceContract>("MotorServiceContractEndpoint");

            IMotorServiceContract proxy = factory.CreateChannel();

            FileReader reader = new FileReader(@"C:\Users\Filip\Desktop\measures_v2.csv");

            var (samples, rejecects) = reader.SamplesRead(@"C:\Users\Filip\Desktop\measures_v2.csv");

        }
    }
}
