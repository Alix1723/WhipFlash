using System;
using System.Xml.Serialization;

namespace LightFxServer
{
    [Serializable]
    public class Colour
    {
        [XmlElement]
        public int Alpha;
        [XmlElement]
        public int Red;
        [XmlElement]
        public int Green;
        [XmlElement]
        public int Blue;


        //DIY ARGB colour to hold some helper methods as well as spell it correctly I guess
        public Colour()
        {
            this.Alpha = 0;
            this.Red = 0;
            this.Green = 0;
            this.Blue = 0;
        }

        public Colour(int a, int r, int g, int b)
        {
            this.Alpha = a;
            this.Red = r;
            this.Green = g;
            this.Blue = b;
        }

        public static Colour Blank()
        {
            return new Colour();
        }

        public static Colour White()
        {
            return new Colour(255, 255, 255, 255);
        }

        //Useful methods
        public static Colour MultiplyColours(Colour input, float factor)
        {
            return new Colour(
                Math.Clamp((int)(input.Alpha * factor), 0, 255),
                Math.Clamp((int)(input.Red * factor), 0, 255),
                Math.Clamp((int)(input.Green * factor), 0, 255),
                Math.Clamp((int)(input.Blue * factor), 0, 255));
        }

        public static Colour AddColours(Colour inputA, Colour inputB)
        {
            return new Colour(
                Math.Clamp(inputA.Alpha + inputB.Alpha, 0, 255),
                Math.Clamp(inputA.Red + inputB.Red, 0, 255),
                Math.Clamp(inputA.Green + inputB.Green, 0, 255),
                Math.Clamp(inputA.Blue + inputB.Blue, 0, 255));
        }

        public static Colour OverlayColours(Colour inputA, Colour inputB)
        {
            return new Colour(
                Math.Max(inputA.Alpha, inputB.Alpha),
                Math.Max(inputA.Red, inputB.Red),
                Math.Max(inputA.Green, inputB.Green),
                Math.Max(inputA.Blue, inputB.Blue));
        }

        public static Colour CrossfadeColours(Colour inputA, Colour inputB, float factor)
        {
            return new Colour(
                (int)(inputA.Alpha + ((inputB.Alpha- inputA.Alpha)*factor)),
                (int)(inputA.Red + ((inputB.Red - inputA.Red) * factor)),
                (int)(inputA.Green + ((inputB.Green - inputA.Green) * factor)),
                (int)(inputA.Blue + ((inputB.Blue - inputA.Blue) * factor)));
        }

        public static implicit operator System.Drawing.Color(Colour x)
        {
            return System.Drawing.Color.FromArgb(x.Alpha, x.Red, x.Green, x.Blue);
        }
    }
}
