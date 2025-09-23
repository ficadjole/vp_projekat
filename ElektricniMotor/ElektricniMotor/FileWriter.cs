using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElektricniMotor
{
    public class FileWriter : IDisposable
    {
        private StreamWriter writerGood;
        private StreamWriter writerBad;
        private bool disposed = false;
        private bool badData = false;

        public FileWriter() {

            if (File.Exists("measurements_session.csv"))
            {
                File.Delete("measurements_session.csv");
            }

            if (File.Exists("rejects.csv"))
            {
                File.Delete("rejects.csv");
            }


            writerGood = new StreamWriter("measurements_session.csv", true);
            writerBad = new StreamWriter("rejects.csv", true);

            //odmah pisem zaglavlje
            writerGood.WriteLine("profile_id,u_d,ambient,motor_speed,torque");

            writerBad.WriteLine("profile_id,u_d,ambient,motor_speed,torque");
        }

        public void SampleWrite(MetaZaglavlje metaZaglavlje)
        {
            string newLine = metaZaglavlje.ToString();
            
            //ovo je onaj koji mi govori da sam presao na drugog
            if(metaZaglavlje.Profile_Id == -1)
            {
                badData = true;
                return;
            }

            if (!badData)
            {
                writerGood.WriteLine(newLine);
                writerGood.Flush();
            }
            else
            {
                writerBad.WriteLine(newLine);
                writerBad.Flush();
            }

        }

        ~FileWriter()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {

            if (!disposed) {

                if (disposing)
                {
                    if (writerGood != null)
                    {
                        writerGood.Flush();
                        writerGood.Dispose();
                    }

                    if(writerBad != null)
                    {
                        writerBad.Flush();
                        writerBad.Dispose();
                    }
                }

                disposed = true;
            }

        }
    }
}
