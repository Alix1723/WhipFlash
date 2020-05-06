using System;
using System.Threading.Tasks;

namespace LightFxServer
{
    public class LightControl
    {
        //Midi note, vel multiplier, range start, range end, colour
        public LightChannel[] LightChannels = new LightChannel[5]
        {
            new LightChannel(33, 3f, 0, 35, System.Drawing.Color.FromArgb(255, 255, 120, 0)),
            new LightChannel(38, 1f, 36, 80, System.Drawing.Color.FromArgb(255, 255, 2, 0)),
            new LightChannel(48, 0.9f, 81, 117, System.Drawing.Color.FromArgb(255, 225, 100, 0)),
            new LightChannel(45, 1f, 118, 154, System.Drawing.Color.FromArgb(255, 0, 15, 255)),
            new LightChannel(41, 1f, 155, 191, System.Drawing.Color.FromArgb(255, 1, 255, 15))
        };

        //Manage colour/vel values and output to lights here
        int updatesPerSecond = 60;

        //Vel (envelope) control
        float decayValue = 0.75f; 
        float offValueCap = 0.002f;

        //Outputs
        System.Drawing.Color[] stripStack;
        LedStripOutput strip;      

        //State information
        bool isStarPowerOn = false;
        System.Drawing.Color currentComboColour;

        //Color c_EmptyComboColor = Color.FromArgb(255, 1, 1, 1); //Black
        System.Drawing.Color[] c_multiplierColours =
        {
            System.Drawing.Color.FromArgb(255, 255, 216, 50), //Yellow (0x combo)
            System.Drawing.Color.FromArgb(255, 255, 150, 0), //Orange
            System.Drawing.Color.FromArgb(255, 25, 255, 25), //Green
            System.Drawing.Color.FromArgb(255, 255, 50, 255), //Purple
            System.Drawing.Color.FromArgb(255, 50, 150, 255) //Light blue
        };

        public LightControl(int striplength = 0)
        { 
            if(striplength<1)
            {
                foreach(LightChannel lc in LightChannels)
                {
                    striplength += (lc.StripRange.Item2 - lc.StripRange.Item1);
                }
            }

            stripStack = new System.Drawing.Color[striplength];
            strip = new LedStripOutput(striplength);
            strip.SetStrip(System.Drawing.Color.Empty);
            currentComboColour = c_multiplierColours[0];

            var bgtask = Task.Run(UpdateValues);
        }        

        public void ProcessEvent(MidiEvent eventIn)
        {          
            //Filter to NoteOn Events
            if (eventIn.EventType == (int)EventTypes.NoteOn || eventIn.EventType== (int)EventTypes.HitOn && eventIn.NoteVelocity>0)
            {
                //Filter to specific note nums#
                foreach(LightChannel curChannel in LightChannels)
                {
                    if(eventIn.NoteNumber == curChannel.ChannelTriggerNote)
                    {
                        curChannel.SetHitValue((eventIn.NoteVelocity / 127f));
                        //Console.WriteLine($"Set Hitvalues {i} to {HitValues[i]}");
                        break;
                    }

                }
            }

            //Special events 
            if(eventIn.EventType == (int)EventTypes.SpecialEvent)
            {
                Console.WriteLine($"Got a {eventIn.NoteNumber} event, {eventIn.NoteVelocity} value");

                //Multiplier (and SP)
                if (eventIn.NoteNumber == 2)
                {                    
                    currentComboColour = c_multiplierColours[eventIn.NoteVelocity];
                    //UpdateStackValues(currentComboColour);
                    //Console.WriteLine($"Setting multiplier colour to: {eventIn.NoteVelocity} ({currentComboColour})");
                    isStarPowerOn = (eventIn.NoteVelocity == 4);
                }
                //Combo meter
                if (eventIn.NoteNumber == 1)
                {
                    //ClearStack();
                    var comboMeter = eventIn.NoteVelocity;
                    var SegmentLength = 7; //about 5 per segment, 6 spare at the end (0-10)

                    /*if (comboMeter == 0) 
                    { 
                        for(int i = 0; i < stripStack.Length; i++) 
                        { 
                            stripStack[i] = System.Drawing.Color.Empty; 
                        }
                    }
                    else
                    {

                        for (int i = 1; i < 11; i++)
                        { //Combo goes 0 - 10 (11 values)
                            for (int j = 0; j < SegmentLength; j++)
                            {
                                stripStack[j + ((i - 1) * SegmentLength)] = (comboMeter >= i) ? currentComboColour : System.Drawing.Color.Empty;
                            }
                        }
                    }*/

                    
                }                      
            }
        }

        //Main refresh loop
        public async Task UpdateValues()
        {
            try
            {
                Console.WriteLine("Started refreshing...");
                while (true)
                {
                    foreach(LightChannel lightChannel in LightChannels)
                    { 
                        if (lightChannel.HitValue >= 0)
                        {
                            if (lightChannel.HitValue <= offValueCap)
                            {
                                lightChannel.SetHitValue(-1f);
                            }

                            //Apply drum colours
                            SetStackRange(lightChannel.StripRange, lightChannel.GetMultipliedColour());

                            //Envelope control happens here
                            lightChannel.DecayHitValue(decayValue);
                        }
                    }

                    //Refresh strip
                    strip.SetStrip(stripStack);
                    await Task.Delay(1000 / updatesPerSecond);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Exception in refresh loop");
                Console.WriteLine(e.ToString());
                throw;
            }
        }

        void SetStackRange(Tuple<int,int> targetRange, System.Drawing.Color colourInput )
        {
            for(int k = targetRange.Item1; k <= targetRange.Item2; k++)
            {
                stripStack[k] = colourInput;
            }
        }

        void ClearStack()
        {
            for (int i = 0; i < stripStack.Length; i++)
            {
                stripStack[i] = System.Drawing.Color.Transparent;
            }
        }
        
        //todo: refactor
        void UpdateStackValues(System.Drawing.Color newColour)
        {
            for (int i = 0; i < stripStack.Length; i++)
            {
                if (stripStack[i].A > 0)
                {
                    stripStack[i] = newColour;
                }
            }
        }

        public void TestStrip(System.Drawing.Color setColour)
        {
            for (int i = 0; i < stripStack.Length; i++)
            {
                stripStack[i] = setColour;
            }
            strip.SetStrip(stripStack);
        }

        public void TestStrip(System.Drawing.Color[] setColours, int offset = 0)
        {
            for (int i = 0; i < stripStack.Length; i++)
            {
                stripStack[i] = setColours[(offset+i) % setColours.Length];
            }
            strip.SetStrip(stripStack);
        }
    }

    //Note: Not strictly MIDI event bytes, but just what I've gathered through testing
    enum EventTypes
    {
        HitOn = 153, //2 events per hit, one with Velocity value and one with 0 Velocity to 'end' it
        NoteOn = 144, //Actually just for keys?
        NoteOff = 128,
        //ControlChange =  244? Pitch is then a factor of NV and Vel
        //Pedal = 185, HH is a factor of Vel only
        PitchBend = 244,
        HiHatPedal = 185,
        SpecialEvent = 100 //Special events controlled by non-instruments
            //NV 1 is Combo (v = 0-10), NV 2 is Multiplier&SP (V = 0-5)
    }
}
