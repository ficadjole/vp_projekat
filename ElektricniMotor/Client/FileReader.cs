using Common;
using System;
using System.Collections.Generic;
using System.IO;

namespace Client
{
    public class FileReader : IDisposable
    {

        private StreamReader reader;
        private bool disposed = false;
        private Validacija validacija;

        public FileReader(string filePath)
        {
            validacija = new Validacija();
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
                if (reader != null)
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



            using (StreamReader reader = new StreamReader(filePath))
            {
                reader.ReadLine();

                while ((fileLine = reader.ReadLine()) != null)
                {

                    string[] parts = fileLine.Split(',');

                    double u_q = double.Parse(parts[0]);
                    double u_d = double.Parse(parts[3]);
                    double motor_speed = double.Parse(parts[5]);
                    double ambient = double.Parse(parts[10]);
                    double torque = double.Parse(parts[11]);
                    int profil_id = Int32.Parse(parts[12]);
                    MetaZaglavlje podaciFile = new MetaZaglavlje(profil_id, u_q, u_d, motor_speed, ambient, torque);

                    bool isValid = validacija.PokusajValidacija(podaciFile);

                    if (nmbrRow < 100 && isValid == true)
                    {
                        podaci.Add(podaciFile);
                        nmbrRow++;
                    }
                    else
                    {
                        losiPodaci.Add(podaciFile);

                    }

                    
                }
            }

            return (podaci, losiPodaci);
        }
    }
}
