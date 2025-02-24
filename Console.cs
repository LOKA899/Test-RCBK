using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lok_wss
{
    public static class CustomConsole
    {

        public static void WriteLine(string message, ConsoleColor colour)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = colour;
            Console.WriteLine(message);
  
        }
    }
}
