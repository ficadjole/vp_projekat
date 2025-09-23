using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class Validacija
    {

        public void ValidacijaEx(MetaZaglavlje metaZaglavlje)
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

        }

        public bool PokusajValidacija(MetaZaglavlje metaZaglavlje)
        {
            if (metaZaglavlje == null) return false;

            if (metaZaglavlje.Profile_Id <= 0) return false;

            if (metaZaglavlje.U_q < -1000 || metaZaglavlje.U_q > 1000) return false;

            if (metaZaglavlje.U_d < -1000 || metaZaglavlje.U_d > 1000) return false;

            if (metaZaglavlje.Motor_speed < 0) return false;

            if (metaZaglavlje.Ambient < -50 || metaZaglavlje.Ambient > 150) return false;

            if (metaZaglavlje.Torque < -1000 || metaZaglavlje.Torque > 1000) return false;

            return true;
        }
    }
}
