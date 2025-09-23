using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class MetaZaglavlje
    {
        int profile_id;
        double u_q;
        double u_d;
        double motor_speed;
        double ambient;
        double torque;

        public MetaZaglavlje() : this(1, 0.0, 0.0, 0.0, 0.0, 0.0) { }

        public MetaZaglavlje(int profile_id, double u_q, double u_d, double motor_speed, double ambient, double torque)
        {
            this.profile_id = profile_id;
            this.u_q = u_q;
            this.u_d = u_d;
            this.motor_speed = motor_speed;
            this.ambient = ambient;
            this.torque = torque;
        }
        [DataMember]
        public int Profile_Id { get => profile_id; set => profile_id = value; }
        [DataMember]
        public double U_q { get => u_q; set => u_q = value; }
        [DataMember]
        public double U_d { get => u_d; set => u_d = value; }
        [DataMember]
        public double Motor_speed { get => motor_speed; set => motor_speed = value; }
        [DataMember]
        public double Ambient { get => ambient; set => ambient = value; }
        [DataMember]
        public double Torque { get => torque; set => torque = value; }

        public override string ToString()
        {
            return $"Profile_ID: {Profile_Id} U_q: {U_q} U_d: {U_d} Motor_speed: {Motor_speed} Ambient: {Ambient} Torque: {Torque}";
        }
    }
}
