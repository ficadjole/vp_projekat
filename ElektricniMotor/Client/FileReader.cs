using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class FileReader : IDisposable
    {

        private StreamReader reader;
        private bool disposed = false;

        public FileReader(string filePath)
        {
            reader = new StreamReader(filePath);
        }

        ~FileReader() { Dispose(false); }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if(reader != null)
                {
                    reader.Dispose();
                }

                disposed = true;
            }
        }

        public (List<MetaZaglavlje> podaci, List<MetaZaglavlje> losiPodaci) SamplesRead(string filePath)
        {
            List<MetaZaglavlje> podaci = new List<MetaZaglavlje>();
            List<MetaZaglavlje> losiPodaci = new List<MetaZaglavlje>();

            int nmbrRow = 0;

            string fileLine;

            //ovo nam ovde preksace header i krecemo dalje na podatke
            reader.ReadLine();

            using(StreamReader reader = new StreamReader(filePath))
            {
                while ((fileLine = reader.ReadLine()) != null) {

                    string[] parts = fileLine.Split(',');

                    double u_q = double.Parse(parts[0]);
                    double u_d = double.Parse(parts[3]);
                    double motor_speed = double.Parse(parts[5]);
                    double ambient = double.Parse(parts[10]);
                    double torque = double.Parse(parts[11]);
                    int profil_id = Int32.Parse(parts[12]);
                    MetaZaglavlje podaciFile = new MetaZaglavlje(profil_id, u_q, u_d, motor_speed, ambient, torque);

                    if (nmbrRow++ <= 100)
                    {
                        podaci.Add(podaciFile);
                    }
                    else
                    {
                        losiPodaci.Add(podaciFile);
                    }

                        nmbrRow++;
                }
            }

            return (podaci, losiPodaci);
        }
    }
}
