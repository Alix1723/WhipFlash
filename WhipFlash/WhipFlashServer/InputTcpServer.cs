using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace WhipFlashServer
{

    //Multi-threaded server paraphrased from
    //https://codinginfinite.com/multi-threaded-tcp-server-core-example-csharp/
    class InputTcpServer
    {
        TcpListener server = null;
        int inputBufferSize;
        Task listenTask;

        public InputTcpServer(string ip, int port, int buffersize = 512)
        {
            Console.WriteLine($"Started server @ {ip}:{port}");
            inputBufferSize = buffersize;
            IPAddress localAddr = IPAddress.Parse(ip);
            server = new TcpListener(localAddr, port);
            server.Start();
            listenTask = Task.Run(StartListener);
        }

        private async void StartListener()
        {
            try
            {
                while (true)
                {
                    Console.WriteLine("Waiting for a connection...");
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine($"Connected to {client.ToString()} ");
                    Thread t = new Thread(new ParameterizedThreadStart(HandleClient));
                    t.Start(client);
                    await Task.Delay(1);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
                server.Stop();
            }

        }

        public void HandleClient(Object obj)
        {
            TcpClient client = (TcpClient)obj;
            var stream = client.GetStream();
            string data = null;
            Byte[] bytes = new Byte[inputBufferSize];
            int i;
            try
            {
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    string hex = BitConverter.ToString(bytes);
                    data = Encoding.ASCII.GetString(bytes, 0, i);

                    if (data == "disconnect") 
                    {
                        //Gracefully disconnect
                        Console.WriteLine($"Client @ {client.Client.RemoteEndPoint} Disconnected");
                        stream.Close();
                        client.Close();                   
                    }
                    try
                    {
                        //Handle potentially multiple events in one message
                        var pieces = data.Split(",",StringSplitOptions.RemoveEmptyEntries);
                        for (i = 0; i < pieces.Length; i += 5)
                        {
                            var evnt = new MidiEvent(
                                pieces[i],
                                pieces[i + 1],
                                pieces[i + 2],
                                pieces[i + 3],
                                pieces[i + 4]);

                            Program.MidiMessageList.Enqueue(evnt);
                        }
                    }
                    catch (IndexOutOfRangeException)
                    {
                        Console.WriteLine("Warning: Not a well-formed (ints)(event,note,vel,data,timestamp) message!");
                        Console.WriteLine("Message:");
                        Console.WriteLine(data);
                    }
                    //Don't care about replies
                    //Byte[] reply = Encoding.ASCII.GetBytes(data.Length.ToString());
                    //stream.Write(reply, 0, reply.Length);
                }
            }
            catch (IOException) //se
            {
                Console.WriteLine("--Client dropped");
                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
                client.Close();
            }
        }

        public void StopListener()
        {
            server.Stop();
        }
    }
}
