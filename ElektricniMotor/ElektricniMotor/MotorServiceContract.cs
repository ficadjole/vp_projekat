using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ElektricniMotor
{
    public class MotorServiceContract : IMotorServiceContract
    {
        private bool aktivnaSesija = false;

        public string EndSession()
        {
            aktivnaSesija = false;

            return "ACK: session was succesful";
        }

        public string PushSample(MetaZaglavlje metaZaglavlje)
        {
            return "ACK: metaZaglavlje was sent";
        }

        public string StartSession(MetaZaglavlje metaZaglavlje)
        {
            aktivnaSesija = true;

            return "ACK: session is started";
        }

        public bool Validacija(MetaZaglavlje metaZaglavlje)
        {
            if (metaZaglavlje == null)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("metaZaglavlje is required"));

            if (metaZaglavlje.Profile_Id <= 0)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Profile_Id must be greater than 0!"));

            if (metaZaglavlje.U_q < -1000 || metaZaglavlje.U_q > 1000)
                throw new FaultException<ValidationFault>(
                    new ValidationFault($"U_q {metaZaglavlje.U_q} out of range (-1000,1000)"));

            if (metaZaglavlje.U_d < -1000 || metaZaglavlje.U_d > 1000)
                throw new FaultException<ValidationFault>(
                    new ValidationFault($"U_d {metaZaglavlje.U_d} out of range (-1000,1000)"));

            if (metaZaglavlje.Motor_speed < 0)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Motor_speed must be >= 0"));

            if (metaZaglavlje.Ambient < -50 || metaZaglavlje.Ambient > 150)
                throw new FaultException<ValidationFault>(
                    new ValidationFault($"Ambient {metaZaglavlje.Ambient} out of range (-50,150)"));

            if (metaZaglavlje.Torque < -1000 || metaZaglavlje.Torque > 1000)
                throw new FaultException<ValidationFault>(
                    new ValidationFault($"Torque {metaZaglavlje.Torque} out of range (-1000,1000)"));



            return true;
        }
    }
}
