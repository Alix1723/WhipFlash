using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace WhipFlashServer
{
    [Serializable]
    public class Colour : IXmlSerializable
    {
        [XmlElement]
        public int Alpha;
        [XmlElement]
        public int Red;
        [XmlElement]
        public int Green;
        [XmlElement]
        public int Blue;

        //ARGB colour to hold some helper methods as well as spell it correctly I guess
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

        public Colour(int r, int g, int b)
        {
            this.Alpha = 255;
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
            var output = new Colour(
                (int)(inputA.Alpha + ((inputB.Alpha - inputA.Alpha) * factor)),
                (int)(inputA.Red + ((inputB.Red - inputA.Red) * factor)),
                (int)(inputA.Green + ((inputB.Green - inputA.Green) * factor)),
                (int)(inputA.Blue + ((inputB.Blue - inputA.Blue) * factor)));

            if (output.Alpha < 0) { throw new InvalidOperationException($"Alpha negative? a{inputA} b{inputB} f {factor}"); }

            if (output.Red < 0) { throw new InvalidOperationException($"Red negative? a{inputA} b{inputB} f {factor}"); }
            if (output.Green < 0) { throw new InvalidOperationException($"Green negative? a{inputA} b{inputB} f {factor}"); }
            if (output.Blue < 0) { throw new InvalidOperationException($"Blue negative? a{inputA} b{inputB} f {factor}"); }


            return output;
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        //https://stackoverflow.com/questions/22098564/implementing-custom-xml-serialization-deserialization-of-compound-data-type
        public void ReadXml(XmlReader reader)
        {
            reader.MoveToContent();

            Boolean isEmptyElement = reader.IsEmptyElement; 
            reader.ReadStartElement();
            if (!isEmptyElement) 
            {
                var valuesString = reader.ReadContentAsString();
                string[] values = valuesString.Split(',');
                this.Alpha = int.Parse(values[0]);
                this.Red = int.Parse(values[1]);
                this.Green = int.Parse(values[2]);
                this.Blue = int.Parse(values[3]);
                reader.ReadEndElement();
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteString($"{this.Alpha},{this.Red},{this.Green},{this.Blue}");
        }

        //Auto-convert to 'Color'
        public static implicit operator System.Drawing.Color(Colour x)
        {
            return System.Drawing.Color.FromArgb(x.Alpha, x.Red, x.Green, x.Blue);
        }
        public override string ToString()
        {
            return $"{Alpha},{Red},{Green},{Blue}";
        }
    }
}
