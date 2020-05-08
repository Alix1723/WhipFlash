using System;
using System.Threading.Tasks;

namespace LightFxServer
{
    public class LightControl
    {
        //todo: overlapping ranges for multiple notes
        //Midi note, vel multiplier, range start, range end, colour
        public LightChannel[] LightChannels = new LightChannel[]
        {
            new LightChannel(33, 3f, 0, 35, System.Drawing.Color.FromArgb(255, 255, 120, 0)), //O
            new LightChannel(38, 1f, 36, 80, System.Drawing.Color.FromArgb(255, 255, 2, 0)), //R
            new LightChannel(48, 0.9f, 81, 117, System.Drawing.Color.FromArgb(255, 225, 100, 0)), //Y
            new LightChannel(45, 1f, 118, 154, System.Drawing.Color.FromArgb(255, 0, 15, 255)), //B
            new LightChannel(41, 1f, 155, 191, System.Drawing.Color.FromArgb(255, 1, 255, 15)), //G

            new LightChannel(49, 1.2f, 192, 206, System.Drawing.Color.FromArgb(255, 1, 255, 15)), //g
            new LightChannel(51, 1.2f, 207, 221, System.Drawing.Color.FromArgb(255, 0, 15, 255)), //b
            new LightChannel(26, 1.2f, 222, 236, System.Drawing.Color.FromArgb(255, 225, 100, 0)), //y
            new LightChannel(22, 1f, 237, 251, System.Drawing.Color.FromArgb(255, 225, 100, 0)) //y
        };

        //Options
        bool StarPowerOverridesColours = true;
        bool StarPowerBacklights = true;
        bool StarPowerAnimates = true;
        float StarPowerAnimSpeed = 8f; //In LED/s sec

        //Manage colour/vel values and output to lights here
        int updatesPerSecond = 60;

        //Vel (envelope) control
        float decayValue = 0.70f; 
        float offValueCap = 0.002f;

        //Outputs
        System.Drawing.Color[] stripStack;
        LedStripOutput strip;      

        //State information
        bool isStarPowerOn = false;
        System.Drawing.Color currentComboColour;
        float starPowerAnimationCycle = 0; //quantises to int...

        //Color c_EmptyComboColor = Color.FromArgb(255, 1, 1, 1); //Black
        System.Drawing.Color[] c_multiplierColours =
        {
            System.Drawing.Color.FromArgb(255, 255, 216, 50), //Yellow (0x combo)
            System.Drawing.Color.FromArgb(255, 255, 150, 0), //Orange
            System.Drawing.Color.FromArgb(255, 25, 255, 25), //Green
            System.Drawing.Color.FromArgb(255, 255, 50, 255), //Purple
            System.Drawing.Color.FromArgb(255, 50, 150, 255) //Light blue
        };

        System.Drawing.Color c_StarPowerBackgroundColour = System.Drawing.Color.FromArgb(255, 1, 3, 5); //Faint blue
        System.Drawing.Color c_StarPowerForegroundColour = System.Drawing.Color.FromArgb(255, 50, 155, 255); //Light blue

        System.Drawing.Color[] c_StarPowerBackgroundColourPattern = new[] {
            System.Drawing.Color.FromArgb(255, 0, 0, 2),
            System.Drawing.Color.FromArgb(255, 0, 0, 2),
            System.Drawing.Color.FromArgb(255, 0, 1, 3),
            System.Drawing.Color.FromArgb(255, 0, 1, 3),
            System.Drawing.Color.FromArgb(255, 1, 2, 4),
            System.Drawing.Color.FromArgb(255, 1, 2, 4),
            System.Drawing.Color.FromArgb(255, 1, 3, 5),
            System.Drawing.Color.FromArgb(255, 1, 3, 5),
            System.Drawing.Color.FromArgb(255, 1, 2, 4),
            System.Drawing.Color.FromArgb(255, 1, 2, 4),
            System.Drawing.Color.FromArgb(255, 0, 1, 3),
            System.Drawing.Color.FromArgb(255, 0, 1, 3),
        }; //Faint blue


        public LightControl(int striplength = 0)
        { 
            if(striplength<1)
            {
                foreach(LightChannel lc in LightChannels)
                {
                    striplength += (lc.StripRange.Item2+1) - lc.StripRange.Item1;
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
            if (eventIn.EventType == (int)EventTypes.NoteOn || eventIn.EventType == (int)EventTypes.HitOn && eventIn.NoteVelocity>0)
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
                    currentComboColour = c_multiplierColours[eventIn.NoteVelocity];//Combo 
                    SetStarPower(eventIn.NoteVelocity == 4);
                }
                //Combo meter
                if (eventIn.NoteNumber == 1)
                {
                    var comboMeter = eventIn.NoteVelocity;                  
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
                        if(StarPowerBacklights)
                        {                      
                            if(isStarPowerOn)
                            {
                                if (StarPowerAnimates)
                                {
                                    SetStackRange(lightChannel.StripRange, c_StarPowerBackgroundColourPattern, (int)starPowerAnimationCycle);
                                    starPowerAnimationCycle = (starPowerAnimationCycle + ((1.0f/ (float)updatesPerSecond)*(StarPowerAnimSpeed))) % c_StarPowerBackgroundColourPattern.Length; //Todo use time-based
                                }
                                else
                                {
                                    SetStackRange(lightChannel.StripRange, c_StarPowerBackgroundColour);
                                }
                            }
                            else
                            {
                                //Clear stack 
                                SetStackRange(lightChannel.StripRange, System.Drawing.Color.Empty);
                            }
                        }
                        else
                        {
                            //Clear stack 
                            SetStackRange(lightChannel.StripRange, System.Drawing.Color.Empty);
                        } 

                        //Hit colours
                        if (lightChannel.HitValue >= 0)
                        {
                            if (lightChannel.HitValue <= offValueCap)
                            {
                                lightChannel.SetHitValue(-1f);
                            }

                            OverlayStackRange(lightChannel.StripRange, 
                                (StarPowerOverridesColours && isStarPowerOn) ? 
                                lightChannel.GetMultipliedColour(c_StarPowerForegroundColour) : 
                                lightChannel.GetMultipliedColour());

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

        //Set (overwrite) colours
        public void SetStackRange(Tuple<int,int> targetRange, System.Drawing.Color colourInput )
        {
            for(int k = targetRange.Item1; k <= targetRange.Item2; k++)
            {
                stripStack[k] = colourInput;
            }
        }

        //Set (overwrite) colours
        public void SetStackRange(Tuple<int, int> targetRange, System.Drawing.Color[] colourInputs, int offset = 0)
        {
            for (int k = targetRange.Item1; k <= targetRange.Item2; k++)
            {
                stripStack[k] = colourInputs[(k+ offset) % colourInputs.Length];
            }
        }

        //Add colours together on the stack
        public void AddStackRange(Tuple<int, int> targetRange, System.Drawing.Color addColourInput)
        {
            for (int k = targetRange.Item1; k <= targetRange.Item2; k++)
            {
                stripStack[k] = AddColours(stripStack[k], addColourInput); //
            }
        }

        //Combine colours already on the stack
        public void OverlayStackRange(Tuple<int, int> targetRange, System.Drawing.Color overlayColourInput)
        {
            for (int k = targetRange.Item1; k <= targetRange.Item2; k++)
            {
                stripStack[k] = OverlayColours(stripStack[k], overlayColourInput);
            }
        }

        public void ClearStack()
        {
            for (int i = 0; i < stripStack.Length; i++)
            {
                stripStack[i] = System.Drawing.Color.Transparent;
            }
        }
        
        public void ClearStack(System.Drawing.Color newColour)
        {
            for (int i = 0; i < stripStack.Length; i++)
            {
                stripStack[i] = newColour;
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

        private System.Drawing.Color AddColours(System.Drawing.Color a, System.Drawing.Color b)
        {
            return System.Drawing.Color.FromArgb(
                Math.Clamp((int)(a.A * b.A), 0, 255),
                Math.Clamp((int)(a.R * b.R), 0, 255),
                Math.Clamp((int)(a.G * b.G), 0, 255),
                Math.Clamp((int)(a.B * b.B), 0, 255));
        }

        private System.Drawing.Color OverlayColours(System.Drawing.Color a, System.Drawing.Color b)
        {
            return System.Drawing.Color.FromArgb(
                Math.Max(a.A,b.A),
                Math.Max(a.R, b.R),
                Math.Max(a.G, b.G),
                Math.Max(a.B, b.B));
        }

        public void SetStarPower(bool enabled)
        {
            isStarPowerOn = enabled;
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
