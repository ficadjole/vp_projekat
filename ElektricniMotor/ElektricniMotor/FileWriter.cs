using Common;
using System;
using System.Collections.Generic;
using System.Configuration;
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

            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var goodRel = ConfigurationManager.AppSettings["GoodCsv"] ?? @"Data\measurements_session.csv";
            var badRel = ConfigurationManager.AppSettings["BadCsv"] ?? @"Data\rejects.csv";

            var goodPath = Path.GetFullPath(Path.IsPathRooted(goodRel) ? goodRel : Path.Combine(exeDir, goodRel));
            var badPath = Path.GetFullPath(Path.IsPathRooted(badRel) ? badRel : Path.Combine(exeDir, badRel));

            Directory.CreateDirectory(Path.GetDirectoryName(goodPath) ?? exeDir);
            Directory.CreateDirectory(Path.GetDirectoryName(badPath) ?? exeDir);

            if (File.Exists(goodPath)) File.Delete(goodPath);
            if (File.Exists(badPath)) File.Delete(badPath);


            writerGood = new StreamWriter("measurements_session.csv", true);
            writerBad = new StreamWriter("rejects.csv", true);

            writerGood.WriteLine("profile_id,u_d,ambient,motor_speed,torque");

            writerBad.WriteLine("profile_id,u_d,ambient,motor_speed,torque");
        }

        public void SampleWrite(MetaZaglavlje metaZaglavlje)
        {
            string newLine = metaZaglavlje.ToString();
            
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
