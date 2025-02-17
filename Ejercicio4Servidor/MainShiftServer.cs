using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ejercicio4Servidor
{
    internal class MainShiftServer
    {
        static void Main(string[] args)
        {
            ShiftServer server = new ShiftServer();
            server.LoadWaitQueue("waitQueue.txt");
            server.Init();
        }
    }
}
