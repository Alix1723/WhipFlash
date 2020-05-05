using System;
using System.Collections.Concurrent;
using System.Drawing;

namespace LightFxServer
{
    class Program
    {
        public static ConcurrentQueue<MidiEvent> MidiMessageList = new ConcurrentQueue<MidiEvent>();        

        static void Main(string[] args)
        {          
            bool debug = true;
            Console.WriteLine("Light FX Server");
            string targetAddress = "192.168.1.23";//"127.0.0.1";
            int targetPort = 5005;

            bool running = true;

            if (args.Length > 0) 
            {
                if (args[0] != null) { targetAddress = args[0]; }
                if (args[1] != null) { int.TryParse(args[1], out targetPort); }
            }

            //Inputs over TCP
            //Todo: gracefully shut down, instruct comnnected clients when it happens
            InputTcpServer ls = new InputTcpServer(targetAddress,targetPort);

            //Deal with messages
            LightControl control = new LightControl();
            MidiEvent curEvent;
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
                        if (debug) { Console.WriteLine($"MIDI Event: {curEvent.ToString()}"); }
                        if (curEvent.EventType == 999) { running = false; } //special event to close server 
                        control.ProcessEvent(curEvent);                        
                    }
                }
            }

            ls.StopListener();
        }
    }
}

/*
 * 
*/