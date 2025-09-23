using System;
using System.ServiceModel;

namespace ElektricniMotor
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceHost svc = new ServiceHost(typeof(ElektricniMotor.MotorServiceContract));

            svc.Open();

            Console.WriteLine("Server is started, press key to close it!");
            Console.ReadKey();

            svc.Close();
        }
    }
}
