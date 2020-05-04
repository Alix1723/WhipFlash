using System;

using Iot.Device.Ws28xx;
using System.Device.Spi;
using System.Runtime.CompilerServices;
using Iot.Device.Graphics;
using Iot.Device.BrickPi3.Sensors;

namespace LightFxServer
{
    class LedStripOutput
    {
        private Ws2812b strip;
        BitmapImage stripImage;

        public LedStripOutput(int numberofleds)
        {
            strip = Initialise(numberofleds);
            stripImage = strip.Image;
        }

        public void SetStrip(System.Drawing.Color[] inputColours)
        {
            //todo: overrun
            for(int i = 0; i < inputColours.Length; i++)
            {
                stripImage.SetPixel(i, 0, inputColours[i]);
            }

            strip.Update();
        }

        public void SetStrip(System.Drawing.Color singleColor)
        {
            //todo: overrun
            for (int i = 0; i < stripImage.Width; i++)
            {
                stripImage.SetPixel(i, 0, singleColor);
            }

            strip.Update();
        }


        private Ws2812b Initialise(int count)
        {
            var settings = new SpiConnectionSettings(0, 0)
            {
                ClockFrequency = 2_400_000,
                Mode = SpiMode.Mode0,
                DataBitLength = 8
            };

            var spi = SpiDevice.Create(settings);
            
            return new Ws2812b(spi, count);
        }
    }
}
