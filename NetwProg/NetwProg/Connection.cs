using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace NetwProg
{
    class Connection
    {
        public StreamReader Read;
        public StreamWriter Write;
        public bool hasConnection = false;
        // Connection heeft 2 constructoren: deze constructor wordt gebruikt als wij CLIENT worden bij een andere SERVER
        public Connection(int port)
        {
            TcpClient client = new TcpClient("localhost", port);
            Read = new StreamReader(client.GetStream());
            Write = new StreamWriter(client.GetStream());
            Write.AutoFlush = true;

            // De server kan niet zien van welke poort wij client zijn, dit moeten we apart laten weten
            Write.WriteLine("Poort: " + Program.MijnPoort);

            // Start het reader-loopje
            new Thread(ReaderThread).Start();
            hasConnection = true;
        }

        // Deze constructor wordt gebruikt als wij SERVER zijn en een CLIENT maakt met ons verbinding
        public Connection(StreamReader read, StreamWriter write)
        {
            Read = read; Write = write;

            // Start het reader-loopje
            new Thread(ReaderThread).Start();
            hasConnection = true;
        }

        // LET OP: Nadat er verbinding is gelegd, kun je vergeten wie er client/server is (en dat kun je aan het Connection-object dus ook niet zien!)

        // Deze loop leest wat er binnenkomt en print dit
        public void ReaderThread()
        {
            try
            {
                while (true)
                {
                    string input = Read.ReadLine();
                    string[] splitInput = input.Split(' ');
                    switch (splitInput[0])
                    {
                        //Update ndis (neighbourport,v,distance)
                        case "UD":
                            {
                                Console.WriteLine("updatedndis");
                                Program.updateNdis(int.Parse(splitInput[1]), int.Parse(splitInput[2]), int.Parse(splitInput[3]));
                            }
                            break;
                        //Else print it to console 
                        default:
                            Console.WriteLine(input);
                            break;
                    }

                }
            }
            catch { Console.WriteLine("rip"); }// Verbinding is kennelijk verbroken
        }
    }
}

