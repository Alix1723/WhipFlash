using Commons.Music.Midi;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;

namespace MidiJunctionClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("MIDI Message splitter client");

            bool testMode = true;

            string targetAddress = "192.168.1.23";//"127.0.0.1";
            int targetPort = 5005;

            if (args.Length > 0)
            {
                if (args[0] != null) { targetAddress = args[0]; }
                if (args[1] != null) { int.TryParse(args[1], out targetPort); }
            }

            if (!testMode)
            {
                TcpClient client = new TcpClient(targetAddress, targetPort);
                NetworkStream stream = client.GetStream();
            }

            var access = MidiAccessManager.Default;
            IMidiInput input;
            IMidiOutput output;

            string chosenIdIn = "";
            string chosenIdOut = "";

            foreach (IMidiPortDetails portdetails in access.Inputs)
            {
                Console.WriteLine($"Input: {portdetails.Name} / ID: {portdetails.Id}");
            }

            if(access.Inputs.Count()<=0)
            {
                throw new InvalidOperationException("No input devices detected!");
            }
            else if(access.Inputs.Count() == 1)
            {
                //Default          
                chosenIdIn = access.Inputs.First().Id;
            }
            else
            {
                //Choose from list
                Console.WriteLine("Enter device ID to use:");
                chosenIdIn = Console.ReadLine();
            }

            input = access.OpenInputAsync(chosenIdIn).Result;

            foreach (IMidiPortDetails portdetails in access.Outputs)
            {
                Console.WriteLine($"Output: {portdetails.Name} / ID: {portdetails.Id}");
            }
            Console.WriteLine("Enter output device ID to use:");
            chosenIdOut = Console.ReadLine();

            output = access.OpenOutputAsync(chosenIdOut).Result;

            Console.WriteLine("Reading events...");
            input.MessageReceived += (object sender, MidiReceivedEventArgs e)
               =>
            {
                if (!testMode)
                {
                    string[] outputs = new string[5] { "0", "0", "0", "0", "0" };

                    for (int i = 0; i < e.Length; i++)
                    {
                        outputs[i] = e.Data[i].ToString();
                    }
                    outputs[4] = e.Timestamp.ToString();
                    TransmitMessage($"{outputs[0]},{outputs[1]},{outputs[2]},{outputs[3]},{outputs[4]}", stream);
                }

                output.Send(e.Data, 0, e.Data.Length, e.Timestamp);
            };


            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            input.CloseAsync();
        }

        private static void TransmitMessage(string msg, NetworkStream targetStream)
        {
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(msg);
            targetStream.Write(data, 0, data.Length);
        }
    }
}
