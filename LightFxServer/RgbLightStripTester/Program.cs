using System;
using System.Drawing;
using System.Threading.Tasks;
using LightFxServer;

namespace RgbLightStripTester
{
    class Program
    {

        static bool demoPattern = false;
        static LightControl testLc;

        static void Main(string[] args)
        {
            Console.WriteLine("Test connected light strips");
            Console.WriteLine("Enter number of LEDs:");

            var numberinput = Console.ReadLine();
            int total = int.Parse(numberinput);
            testLc = new LightControl(total);


            var bgTask = Task.Run(CyclePattern);
            
            while (true)
            {
                Console.WriteLine("Input an R,G,B value:");
                var testinput = Console.ReadLine();

                if (testinput.ToLowerInvariant() == "rainbow")
                {
                    demoPattern = true;
                }
                else
                {
                    demoPattern = false;
                    var values = testinput.Split(",");
                    var testColor = Color.FromArgb(255, int.Parse(values[0]), int.Parse(values[1]), int.Parse(values[2]));
                    testLc.TestStrip(testColor);
                    Console.WriteLine($"Strip set to {testColor}");
                }                
            }
        }

        static async Task CyclePattern()
        {
            var cycle = 0; //0-7
            Color[] pattern = new Color[]
            {
                Color.FromArgb(255, 255, 0, 0),
                Color.FromArgb(255, 128, 128, 0),
                Color.FromArgb(255, 80, 180, 0),
                Color.FromArgb(255, 0, 255, 0),
                Color.FromArgb(255, 0, 128, 128),
                Color.FromArgb(255, 0, 0, 255),
                Color.FromArgb(255, 128, 0, 128),
            };

            while(true)
            {
                while (demoPattern)
                {
                    testLc.TestStrip(pattern, cycle);


                    cycle = (cycle + 1) % pattern.Length;

                    await Task.Delay(1000 / 60);
                }
            }
        }
    }
}
