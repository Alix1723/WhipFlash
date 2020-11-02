using Commons.Music.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.ConstrainedExecution;

namespace MidiJunctionClient
{
    class Program
    {
        //A client to read MIDI inputs, send them to a light server, and (optionally) forwards them to an output MIDI device
        static void Main(string[] args)
        {

            Console.WriteLine("MIDI Message splitter client");
            var testmode = false;

            var remapNotes = true;

            //remap MIDI notes here (src,target)
            Dictionary<int, int> noteMapping = new Dictionary<int, int> 
            { 
                { 39, 38 }, //Snare Rim -> Snare
                { 28, 26 }, //Lcymbal -> Hihat 
                { 52, 51 }, //Ride bell -> Ride
                { 53, 51 }, //Mcymbal -> Ride
                { 59, 49 }, //xLCymbal -> RCymbal

            };

            string targetAddress = "127.0.0.1";
            int targetPort = 5005;

            if (args.Length > 0)
            {
                if (args[0] != null) { targetAddress = args[0]; }
                if (args[1] != null) { int.TryParse(args[1], out targetPort); }
            }

            TcpClient client = new TcpClient(targetAddress, targetPort);
            NetworkStream stream = client.GetStream();
            try
            {
                if (testmode)
                {
                    while (true)
                    {
                        var inputstr = Console.ReadLine();
                        TransmitMessage(inputstr, stream);
                        Console.WriteLine($"Sent message {inputstr}");
                    }
                }


                var access = MidiAccessManager.Default;
                IMidiInput input = null;
                IMidiOutput output = null;

                string chosenIdIn = "";
                string chosenIdOut = "";

                foreach (IMidiPortDetails portdetails in access.Inputs)
                {
                    Console.WriteLine($"Input: {portdetails.Name} / ID: {portdetails.Id}");
                }

                if (access.Inputs.Count() <= 0)
                {
                    throw new InvalidOperationException("No input devices detected!");
                }
                else if (access.Inputs.Count() == 1)
                {
                    //Default          
                    chosenIdIn = access.Inputs.First().Id;
                    Console.WriteLine("Using default input...");
                }
                else
                {
                    //Choose from list
                    Console.WriteLine("Enter input device ID to use:");
                    chosenIdIn = Console.ReadLine();
                }

                input = access.OpenInputAsync(chosenIdIn).Result;

                if (access.Outputs.Count() > 0)
                {
                    foreach (IMidiPortDetails portdetails in access.Outputs)
                    {
                        Console.WriteLine($"Output: {portdetails.Name} / ID: {portdetails.Id}");
                    }
                    Console.WriteLine("Enter output device ID to use:");
                    chosenIdOut = Console.ReadLine();
                }

                try
                {
                    output = access.OpenOutputAsync(chosenIdOut).Result;
                }
                catch
                {
                    Console.WriteLine("Couldn't open output device, ignoring...");
                }

                Console.WriteLine("Reading events...");
                input.MessageReceived += (object sender, MidiReceivedEventArgs e)
                   =>
                {
                    string[] outputs = new string[5] { "0", "0", "0", "0", "0" };

                    for (int i = 0; i < e.Length; i++)
                    {
                        outputs[i] = e.Data[i].ToString();
                    }
                    outputs[4] = e.Timestamp.ToString();
                    TransmitMessage($"{outputs[0]},{outputs[1]},{outputs[2]},{outputs[3]},{outputs[4]},", stream);
                    Console.WriteLine($"{outputs[0]},{outputs[1]},{outputs[2]},{outputs[3]},{outputs[4]}");
                    if (output != null)
                    {
                        if (remapNotes)
                        {
                            int noteNumberIn = e.Data[1];
                            byte noteNumberOut = 0;                       

                            if(noteMapping.ContainsKey(noteNumberIn))
                            {
                                Console.WriteLine($"Remapping note {noteNumberIn} to {noteMapping[noteNumberIn]}");

                                noteNumberOut = (byte)noteMapping[noteNumberIn];
                                e.Data[1] = noteNumberOut;
                            }
                        }

                        output.Send(e.Data, 0, e.Data.Length, e.Timestamp);
                    }
                };

                Console.ReadKey();
            }
            finally
            {
                TransmitMessage("disconnect", stream);
                stream.Close();
                client.Close();
            }
        }

        private static void TransmitMessage(string msg, NetworkStream targetStream)
        {
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(msg);
            targetStream.Write(data, 0, data.Length);
        }
    }
}
