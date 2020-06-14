using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;

namespace WhipFlashServer
{
    class Program
    {
        public static ConcurrentQueue<MidiEvent> MidiMessageList = new ConcurrentQueue<MidiEvent>();

        private static string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");

        static void Main(string[] args)
        {
            Console.WriteLine("Light FX Server");
            string targetAddress = "127.0.0.1";
            int targetPort = 5005;
            bool debug = false;
            bool running = true;
            string targetFilepath = "";

            if (args.Length > 0)
            {
                if (args[0] == "testmode") { debug = true; }
                else
                {
                    if (args[0] != null) { targetAddress = args[0]; }
                    if (args[1] != null) { int.TryParse(args[1], out targetPort); }
                }
            }
            
            if(Directory.Exists(configPath))
            {
                foreach (string fname in Directory.GetFiles(configPath))
                {
                    if(fname.EndsWith(".xml"))
                    {
                        //todo: multiple files, just load the first one for now
                        targetFilepath = fname;
                    }
                }
            }
            else
                {
                throw new InvalidOperationException("No config available!");
            }


            LightControl control = new LightControl(isDebug: debug, inputConfig: LightsConfiguration.LoadConfigFromFile(targetFilepath));

            //Inputs over TCP
            //Todo: gracefully shut down, instruct connected clients when it happens
            InputTcpServer listenServer = new InputTcpServer(targetAddress,targetPort);

            //Deal with messages            
            MidiEvent curEvent;
            try
            {
                while (running)
                {
                    while (MidiMessageList.TryDequeue(out curEvent))
                    {
                        if (curEvent == null)
                        {
                            Console.WriteLine("Null event?");
                        }
                        else
                        {
                            //Todo: extra special event to perform a program-change?
                            if (debug) { Console.WriteLine($"MIDI Event: {curEvent.ToString()}"); }
                            if (curEvent.EventType == 999) { running = false; } //special event to close server 
                            control.ProcessEvent(curEvent);
                        }
                    }
                }
            }
            finally
            {
                listenServer.StopListener();
            }
        }
    }
}

/*
 * 
*/