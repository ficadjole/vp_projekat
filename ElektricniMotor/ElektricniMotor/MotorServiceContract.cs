using Common;
using System;
using System.ServiceModel;

namespace ElektricniMotor
{
    public class MotorServiceContract : IMotorServiceContract
    {
        private bool aktivnaSesija = false;

        private FileWriter writer;

        public string EndSession()
        {
            if (!aktivnaSesija) return "NACK: No active session";

            aktivnaSesija = false;

            writer.Dispose();
            writer = null;

            return "ACK: session is completed";
        }

        public string PushSample(MetaZaglavlje metaZaglavlje)
        {
            if (!aktivnaSesija) return "NACK: Session is not active";

            if (metaZaglavlje == null) { throw new FaultException<DataFormatFault>(new DataFormatFault("metaZaglavlje is null")); }

            Console.WriteLine("->Transmission in progress");

            writer.SampleWrite(metaZaglavlje);

            return "ACK: metaZaglavlje was recived";
        }

        public string StartSession(MetaZaglavlje metaZaglavlje)
        {

            if(metaZaglavlje == null) { throw new FaultException<DataFormatFault>(new DataFormatFault("metaZaglavlje is null")); }


            new Validacija().ValidacijaEx(metaZaglavlje);


            writer = new FileWriter();

            aktivnaSesija = true;

            return "ACK: session is started";
        }


    }
}
