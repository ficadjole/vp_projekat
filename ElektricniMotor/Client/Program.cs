using Common;
using System;
using System.Linq;
using System.ServiceModel;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {

            ChannelFactory<IMotorServiceContract> factory = new ChannelFactory<IMotorServiceContract>("MotorServiceContractEndpoint");

            IMotorServiceContract proxy = factory.CreateChannel();

            FileReader reader = new FileReader(@"C:\Users\LaptopT460s\Desktop\vp_projekat\ElektricniMotor\Client\bin\Debug\measures_v2.csv");

            var (podaci, losiPodaci ) = reader.SamplesRead(@"C:\Users\LaptopT460s\Desktop\vp_projekat\ElektricniMotor\Client\bin\Debug\measures_v2.csv");

            if (podaci.Count() > 0)
            {
                try
                {
                    proxy.StartSession(podaci[0]);

                }
                catch (FaultException<ValidationFault> ex)
                {
                    Console.WriteLine($"Validation fault on session start: {ex.Detail.Message}");
                    return;
                }
                catch (FaultException<DataFormatFault> ex)
                {

                    Console.WriteLine($"Data format fault on session start: {ex.Detail.Message}");
                    return;

                }
                catch (CommunicationException ex)
                {
                    Console.WriteLine(ex.Message);
                }

                Console.WriteLine("-> Started transfering good data");

                foreach (MetaZaglavlje item in podaci)
                {
                    try
                    {
                        var response = proxy.PushSample(item);
                        Console.WriteLine($"[{podaci.IndexOf(item)}] Server response: {response}");
                    }
                    catch (FaultException<ValidationFault> ex)
                    {
                        Console.WriteLine($"Validation fault on session start: {ex.Detail.Message}");
                        return;
                    }
                    catch (FaultException<DataFormatFault> ex)
                    {

                        Console.WriteLine($"Data format fault on session start: {ex.Detail.Message}");
                        return;

                    }

                    System.Threading.Thread.Sleep(200);
                }


            }


            if(losiPodaci.Count() > 0)
            {
                Console.WriteLine("-> Started transfering bad data ");
                MetaZaglavlje otherList = new MetaZaglavlje(-1, 0.0, 0.0, 0.0, 0.0, 0.0);

                proxy.PushSample(otherList); //Ovo je flag da treba da krene da puni drugu listu tj fajl
                System.Threading.Thread.Sleep(200);

                foreach (MetaZaglavlje item in losiPodaci)
                {
                    try
                    {
                        var response = proxy.PushSample(item);
                        Console.WriteLine($"[{losiPodaci.IndexOf(item)}] Server response: {response}");
                    }
                    catch (FaultException<ValidationFault> ex)
                    {
                        Console.WriteLine($"Validation fault on session start: {ex.Detail.Message}");
                        return;
                    }
                    catch (FaultException<DataFormatFault> ex)
                    {

                        Console.WriteLine($"Data format fault on session start: {ex.Detail.Message}");
                        return;

                    }

                    System.Threading.Thread.Sleep(200);
                }
                
            }

            try
            {
                proxy.EndSession();
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine($"Validation fault on session start: {ex.Detail.Message}");
                return;
            }
            catch (FaultException<DataFormatFault> ex)
            {

                Console.WriteLine($"Data format fault on session start: {ex.Detail.Message}");
                return;

            }

            Console.WriteLine("Client tranfered all data");
            Console.ReadKey();

        }
    }
}
