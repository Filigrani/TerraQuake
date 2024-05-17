using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraQuake
{
    internal class Logger
    {
        static FileStream ostrm;
        static StreamWriter writer;
        static bool Ready = false;
        static bool DisableLogger = true;
        public static void StartLogger()
        {
            try
            {
                ostrm = new FileStream("./log.txt", FileMode.OpenOrCreate, FileAccess.Write);
                writer = new StreamWriter(ostrm);
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot open log.txt for writing");
                Console.WriteLine(e.Message);
                return;
            }
            Console.SetOut(writer);
            Ready = true;
        }
        
        public static void Log(string Message)
        {
            if (DisableLogger)
            {
                return;
            }
            
            if (!Ready)
            {
                StartLogger();
            }
            Console.WriteLine(Message);
        }

        public static void SaveLogFile()
        {
            writer.Close();
            ostrm.Close();
        }
    }
}
