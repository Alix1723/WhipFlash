using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace WhipFlashServer
{
    //Serializable class to manage all config items
    [Serializable]
    public class LightsConfiguration
    {
        [XmlElement]
        public bool BootAnimation;

        [XmlElement]
        public bool KeysMode;

        [XmlElement]
        public bool StarPowerOverridesColours;
        [XmlElement]
        public bool StarPowerBacklights;
        [XmlElement]
        public bool StarPowerAnimates;
        [XmlElement]
        public float StarPowerAnimSpeed;

        [XmlElement]
        public bool FastNotesDetection;
        [XmlElement]
        public int FastNotesTimeThreshold;
        [XmlElement]
        public float FastNotesVelocityThreshold;

        [XmlElement]
        public bool FlamNotesDetection;
        [XmlElement]
        public int FlamNotesTimeThreshold; //ms between flam hits
        [XmlElement]
        public float FlamNotesVelocityThreshold; //How hard to hit to flam (and not e.g. ghost)

        [XmlElement]
        public int UpdatesPerSecond;

        [XmlElement]
        public float HitDecayRate;

        [XmlElement]
        public float HitMinimumCap;

        [XmlElement]
        public float IntensityDecayRate;

        [XmlElement]
        public Colour ColourStarPowerBackground;

        [XmlElement]
        public Colour ColourStarPowerForeground;

        [XmlArray]
        public Colour[] ColoursStarPowerPattern;

        [XmlElement]
        public Colour ColourIntensityHighlight;

        [XmlArray]
        public LightChannel[] DefinedChannels;


        static LightsConfiguration() { }

        public static LightsConfiguration LoadConfigFromFile(string filepath)
        {
            try
            {
                XmlSerializer configDeSerializer = new XmlSerializer(typeof(LightsConfiguration));
                TextReader reader = new StreamReader(filepath);

                var loadedConfig = (LightsConfiguration)configDeSerializer.Deserialize(reader);
                reader.Close();
                Console.WriteLine("Load successful.");
                return loadedConfig;
            }
            catch(Exception e)
            {
                Console.WriteLine($"Error loading configuration! {e}");
                throw;
            }
        }

        public static void SaveConfigToFile(LightsConfiguration input, string filepath)
        {
            Console.WriteLine($"Writing to {filepath}");
            XmlSerializer configSerializer = new XmlSerializer(typeof(LightsConfiguration));
            TextWriter writer = new StreamWriter(filepath);

            configSerializer.Serialize(writer, input);
            writer.Close();
            Console.WriteLine("Save successful.");
        }
    }   
}
