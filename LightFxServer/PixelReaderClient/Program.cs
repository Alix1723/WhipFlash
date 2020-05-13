using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace PixelReaderClient
{
    //Read the screen of Clone Hero and output combo, SP, etc
    class Program
    {
        //NB: This is specifically for Clone Hero PTB v0.24.0.1555-master
        //if it breaks it's probably because CH updated
        static Point monitorOffset = new Point(0, 0); //deal with multi     
        static int updatesPerSecond = 60;
        static bool isFourLaneHighway = false; //drums = true, guitar/anything else = false

        static void Main(string[] args)
        {
            string targetAddress = "192.168.1.23";//"127.0.0.1";
            int targetPort = 5005;

            if (args.Length > 0)
            {
                if (args[0] != null) { targetAddress = args[0]; }
                if (args[1] != null) { int.TryParse(args[1], out targetPort); }
            }

            TcpClient client = new TcpClient(targetAddress, targetPort);
            NetworkStream stream = client.GetStream();

            Console.WriteLine("Reading for pixels from Clone Hero...");
            Point p_HighwayLeft_Drums = new Point(538, 1079);
            Point p_HighwayRight_Drums = new Point(1382, 1079);
            Point p_MultiplierStart_Drums = new Point(1256, 664);
            Point p_MultiplierEnd_Drums = new Point(1258, 666);

            Point p_HighwayLeft_Guitar = new Point(508, 1079); 
            Point p_HighwayRight_Guitar = new Point(1412, 1079);
            Point p_MultiplierStart_Guitar = new Point(1272, 664); 
            Point p_MultiplierEnd_Guitar = new Point(1274, 666);

            //Point p_StarPowerBarStart = new Point(585, 1060);
            //Point p_StarPowerBarEnd = new Point(1200, 720);
            //Point p_StarPowerHighwayA = new Point(585, 1060);
            //Point p_StarPowerHighwayB = new Point(615, 990);


            List<Point> ComboMeter = new List<Point>();

            ComboMeter.Add(new Point(1192, 680));
            ComboMeter.Add(new Point(1187, 671));
            ComboMeter.Add(new Point(1183, 663));
            ComboMeter.Add(new Point(1179, 655));
            ComboMeter.Add(new Point(1174, 647));
            ComboMeter.Add(new Point(1170, 639));
            ComboMeter.Add(new Point(1166, 632));
            ComboMeter.Add(new Point(1161, 625));
            ComboMeter.Add(new Point(1158, 618));
            ComboMeter.Add(new Point(1154, 612));

            Color c_EmptyComboColor = Color.FromArgb(255, 1, 1, 1); //Black
            Color c_MultiplierColourTwo = Color.FromArgb(255, 241, 200, 0); //Yellow
            Color c_MultiplierColourFour = Color.FromArgb(255, 158, 239, 159); //Green
            Color c_MultiplierColourEight = Color.FromArgb(255, 191, 135, 213); //Purple
            Color c_StarPowerColour = Color.FromArgb(255, 106, 203, 203); //Light blue

            Dictionary<string, Color> c_MultiplierColours = new Dictionary<string, Color> {
                { "0 Empty", c_EmptyComboColor },
                { "1 2x", c_MultiplierColourTwo },
                { "2 4x", c_MultiplierColourFour },
                { "3 8x", c_MultiplierColourEight },
                { "4 Star Power", c_StarPowerColour } };

            int darkThreshold = 2;
            int highwayThreshold = 600;
            bool isPlaying;
            int LastCombo = 0;
            Color LastColor = Color.Empty;
            int colourChangeDelta = 2500;

            int SpecialEventType = 100;
            int ComboChangeNote = 1;
            int MultiplierChangeNote = 2;

            try
            {
                while (true)
                {
                    string finalOutput = "";
                    if (GetActiveWindowTitle() == "Clone Hero")
                    {
                        //Reading Pixels...
                        isPlaying = (SumColor(GetColorOfPixel(isFourLaneHighway ? p_HighwayLeft_Drums : p_HighwayLeft_Guitar)) > highwayThreshold &&
                            SumColor(GetColorOfPixel(isFourLaneHighway ? p_HighwayRight_Drums : p_HighwayRight_Guitar)) > highwayThreshold);

                        if (isPlaying)
                        {
                            //Combo meter
                            int Combo = 0;
                            foreach (Point p in ComboMeter)
                            {
                                if (SumColor(GetColorOfPixel(p)) > darkThreshold)
                                {
                                    Combo++;
                                }
                            }

                            if (LastCombo != Combo)
                            {
                                finalOutput += $"{SpecialEventType},{ComboChangeNote},{Combo},0,0,";
                                LastCombo = Combo;
                            }

                            //Multiplier colour      
                            try
                            {
                                Color MultiplierColour = isFourLaneHighway ? GetAverage(GetColorOfManyPixels(p_MultiplierStart_Drums, p_MultiplierEnd_Drums)) : GetAverage(GetColorOfManyPixels(p_MultiplierStart_Guitar, p_MultiplierEnd_Guitar));

                                if (GetDistance(MultiplierColour, LastColor) > colourChangeDelta)
                                {
                                    //Console.WriteLine($"------Changed!-----");
                                    var lowest = 99999;
                                    KeyValuePair<string, Color> closest = KeyValuePair.Create("", Color.Empty);

                                    //Approximate...
                                    foreach (KeyValuePair<string, Color> entry in c_MultiplierColours)
                                    {
                                        var mcol = entry.Value;
                                        var dist = GetDistance(mcol, MultiplierColour);
                                        if (dist < lowest)
                                        {
                                            lowest = dist;
                                            closest = entry;
                                        }
                                    }

                                    Console.WriteLine($"{closest.Key}");
                                    finalOutput += $"{SpecialEventType},{MultiplierChangeNote},{closest.Key.Substring(0, 1)},0,0,";
                                }

                                LastColor = MultiplierColour;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.ToString());
                            }
                        }

                        //Finally, output to server
                        if (finalOutput.Length > 0)
                        {
                            TransmitMessage(finalOutput, stream);
                        }

                        Thread.Sleep(1000 / updatesPerSecond);
                    }
                    else
                    {
                        Thread.Sleep(1000 / 5);
                    }
                }
            }
            finally
            {
                stream.Close();
                client.Close();
                client.Dispose();
            }
        }

        static Bitmap bitmap_SinglePixel = new Bitmap(1, 1);
        static Graphics g_Graphics = Graphics.FromImage(bitmap_SinglePixel);

        static Color GetColorOfPixel(Point location)
        {
            //Todo: optimise and snip multiple screen parts into one bitmap!

            Rectangle bounds = new Rectangle(location.X + monitorOffset.X, location.Y + monitorOffset.Y, 1, 1);


            g_Graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

                //g.ReleaseHdc();
                /*
                    * Todo:
                    * GDCs leaking!
                    * */
            return bitmap_SinglePixel.GetPixel(0, 0);
            
        }

        static Bitmap bitmap_MultiPixel = new Bitmap(2, 2); //temp
        static Graphics g_Graphics_Multiple = Graphics.FromImage(bitmap_MultiPixel);

        static List<Color> GetColorOfManyPixels(Point startlocation, Point endlocation)
        {
            Rectangle space = new Rectangle(startlocation.X,
                startlocation.Y,
                (endlocation.X - startlocation.X),
                (endlocation.Y - startlocation.Y));

            List<Color> output = new List<Color>();

            g_Graphics_Multiple.CopyFromScreen(space.Location, Point.Empty, space.Size);

            for (int h = 0; h < space.Height; h++)
            {
                for (int w = 0; w < space.Width; w++)
                {
                    output.Add(bitmap_MultiPixel.GetPixel(w, h));
                }
            }
                  

            return output;           
        }
        static Color GetAverage(List<Color> inputs)
        {            
            var a = 0; //necessary?
            var r = 0;
            var g = 0;
            var b = 0;

            foreach (Color sample in inputs)
            {
                a += (sample.A * sample.A);
                r += (sample.R * sample.R);
                g += (sample.G * sample.G);
                b += (sample.B * sample.B);
            }
            return Color.FromArgb((int)Math.Sqrt(a / inputs.Count), 
                (int)Math.Sqrt(r / inputs.Count), 
                (int)Math.Sqrt(g / inputs.Count), 
                (int)Math.Sqrt(b / inputs.Count));
        }

        static int SumColor(Color input)
        {
            return (input.R + input.G + input.B);
        }

        static int GetDistance(Color current, Color match)
        {
            int redDifference = current.R - match.R;
            int greenDifference = current.G - match.G;
            int blueDifference = current.B - match.B;

            return (redDifference * redDifference) + (greenDifference * greenDifference) + (blueDifference * blueDifference);
        }

        //For getting the window name
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        private static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }

        private static void TransmitMessage(string msg, NetworkStream targetStream)
        {
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(msg);
            targetStream.Write(data, 0, data.Length);
        }
    }
}
