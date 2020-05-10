using System;
using System.Threading.Tasks;
using System.Transactions;

namespace LightFxServer
{
    public class LightControl
    {
        //Channels (MIDI note(s) and colour(s) to apply to a range of LEDs
        public LightChannel[] LightChannels;

        //Options
        bool BootAnimation;
        bool StarPowerOverridesColours;
        bool StarPowerBacklights;
        bool StarPowerAnimates;
        float StarPowerAnimSpeed; //In LED/s sec

        bool FastNotesDetection;
        int FastNoteTimeThreshold; //ms between notes to be considered 'fast'
        float FastNoteVelocityThreshold; //How hard to hit to be considered 'fast'

        bool FlamNotesDetection;
        int FlamNotesTimeThreshold; //ms between flam hits
        float FlamNotesVelocityThreshold; //How hard to hit to flam (and not e.g. ghost)

        //Manage colour/vel values and output to lights here
        int updatesPerSecond;

        //Vel (envelope) control
        float decayValue;
        float offValueCap;

        float intensityDecayRate;

        //Outputs
        Colour[] stripStack;
        LedStripOutput strip;

        //State information
        bool isStarPowerOn = false;
        Colour currentComboColour;
        float starPowerAnimationCycle = 0; //quantises to int...
        bool debugmode;

        //Mul colours
        Colour[] c_multiplierColours =
        {
            new Colour(255, 255, 216, 50), //Yellow (0x combo)
            new Colour(255, 255, 150, 0), //Orange
            new Colour(255, 25, 255, 25), //Green
            new Colour(255, 255, 50, 255), //Purple
            new Colour(255, 50, 150, 255) //Light blue
        };

        Colour c_StarPowerBackgroundColour;
        Colour c_StarPowerForegroundColour;
        Colour[] c_StarPowerBackgroundColourPattern; 
        Colour c_IntensityColour;

        public LightControl(int striplength = 0, bool isDebug = false, LightsConfiguration inputConfig = null)
        {
            debugmode = isDebug;

            if(inputConfig!=null)
            {
                SetCurrentConfig(inputConfig);
            }

            if (striplength < 1)
            {
                foreach (LightChannel lc in LightChannels)
                {
                    striplength = Math.Max(striplength, lc.StripRangeEnd + 1);
                }
            }

            stripStack = new Colour[striplength];

            if (!debugmode)
            {
                strip = new LedStripOutput(striplength);
                strip.SetStrip(Colour.Blank());
            }
            currentComboColour = c_multiplierColours[0];

            var bgtask = Task.Run(UpdateValues);
        }

        public void ProcessEvent(MidiEvent eventIn)
        {
            //Filter to NoteOn Events
            if (eventIn.EventType == (int)EventTypes.NoteOn || eventIn.EventType == (int)EventTypes.HitOn && eventIn.NoteVelocity > 0)
            {
                //Filter to specific note nums#
                foreach (LightChannel curChannel in LightChannels)
                {
                    for (int i = 0; i < curChannel.ChannelTriggerNotes.Length; i++)
                    {
                        if (eventIn.NoteNumber == curChannel.ChannelTriggerNotes[i])
                        {
                            curChannel.SetHitValue((eventIn.NoteVelocity / 127f));
                            curChannel.SetNoteIndex(i);

                            int noteDelta = curChannel.GetTimeFromLastHit(eventIn.TimeStamp);

                            //Flams and fast notes
                            if (noteDelta < FlamNotesTimeThreshold & FlamNotesDetection)
                            {
                                if (curChannel.HitValue > FlamNotesVelocityThreshold)
                                {
                                    curChannel.SetIntensity(1);
                                }
                            }
                            else if (noteDelta < FastNoteTimeThreshold & FastNotesDetection)
                            {
                                if (curChannel.HitValue > FastNoteVelocityThreshold) 
                                {
                                    
                                    curChannel.SetIntensity(Math.Min(curChannel.ChannelIntensity + (curChannel.HitValue * 0.15f), 1));
                                }
                            }

                            break;
                        }
                    }
                }
            }

            //Special events 
            if (eventIn.EventType == (int)EventTypes.SpecialEvent)
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
            float bootAnimationIndex = 0f; //0-1
            try
            {
                Console.WriteLine("Started refreshing...");
                while (true)
                {
                    if (debugmode)
                    {
                        //ow my eyes
                        //Console.Clear();
                        //Console.WriteLine("LC debug:");
                    }

                    ClearStack();

                    foreach (LightChannel currentChannel in LightChannels)
                    {
                        if (StarPowerBacklights)
                        {
                            if (isStarPowerOn)
                            {
                                if (StarPowerAnimates)
                                {
                                    SetStackRange(currentChannel.GetStripRange(), c_StarPowerBackgroundColourPattern, (int)starPowerAnimationCycle);
                                    starPowerAnimationCycle = (starPowerAnimationCycle + ((1.0f / (float)updatesPerSecond) * (StarPowerAnimSpeed))) % c_StarPowerBackgroundColourPattern.Length; //Todo use time-based
                                }
                                else
                                {
                                    SetStackRange(currentChannel.GetStripRange(), c_StarPowerBackgroundColour);
                                }
                            }
                        }

                        //Hit colours
                        if (currentChannel.HitValue >= 0)
                        {
                            if (currentChannel.HitValue <= offValueCap)
                            {
                                currentChannel.SetHitValue(-1f);
                            }

                            //todo: SP and fast/flams are exclusive?
                            OverlayStackRange(currentChannel.GetStripRange(),
                                (StarPowerOverridesColours && isStarPowerOn) ?
                                    Colour.MultiplyColours(c_StarPowerForegroundColour, currentChannel.HitValue)
                                :
                                (FlamNotesDetection | FastNotesDetection) ?
                                    Colour.MultiplyColours(Colour.CrossfadeColours(currentChannel.GetCurrentHitColour(), c_IntensityColour, currentChannel.ChannelIntensity), currentChannel.HitValue)
                                :
                                    Colour.MultiplyColours(currentChannel.GetCurrentHitColour(), currentChannel.HitValue)) ;

                            //Envelope control happens here
                            currentChannel.DecayHitValue(decayValue);
                        }

                        if (currentChannel.ChannelIntensity > 0)
                        {
                            currentChannel.DecayIntensityValue(intensityDecayRate);
                        }

                        //debug
                        if (debugmode)
                        {
                            //Console.WriteLine(lightChannel.ToString());
                        }

                        if(BootAnimation & bootAnimationIndex >= 0)
                        {
                            int rangedifference = currentChannel.StripRangeEnd - currentChannel.StripRangeStart;
                            int specific = (int)(currentChannel.StripRangeStart + (bootAnimationIndex * 2 * (currentChannel.StripRangeEnd - currentChannel.StripRangeStart) % rangedifference));
                            SetStackRange(Tuple.Create(specific, specific), Colour.CrossfadeColours(currentChannel.GetCurrentHitColour(), Colour.Blank(), 0.9f));
                        }
                    }

                    if (BootAnimation)
                    {
                        if (bootAnimationIndex >= 0f)
                        {
                            bootAnimationIndex += (2.0f / updatesPerSecond);
                            if(bootAnimationIndex >= 1.0f)
                            {
                                bootAnimationIndex = -1f;
                            }
                        }
                    }

                    //Refresh strip
                    if (!debugmode)
                    {
                        strip.SetStrip(stripStack);
                    }
                    await Task.Delay(1000 / updatesPerSecond);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in refresh loop");
                Console.WriteLine(e.ToString());
                throw;
            }
        }

        //Set (overwrite) colour
        public void SetStackRange(Tuple<int, int> targetRange, Colour colourInput)
        {
            for (int k = targetRange.Item1; k <= targetRange.Item2; k++)
            {
                stripStack[k] = colourInput;
            }
        }

        //Set (overwrite) colours
        public void SetStackRange(Tuple<int, int> targetRange, Colour[] colourInputs, int offset = 0)
        {
            for (int k = targetRange.Item1; k <= targetRange.Item2; k++)
            {
                stripStack[k] = colourInputs[(k + offset) % colourInputs.Length];
            }
        }

        //Add colours together on the stack
        public void AddStackRange(Tuple<int, int> targetRange, Colour addColourInput)
        {
            for (int k = targetRange.Item1; k <= targetRange.Item2; k++)
            {
                stripStack[k] = Colour.AddColours(stripStack[k], addColourInput); //
            }
        }

        //Combine colours already on the stack
        public void OverlayStackRange(Tuple<int, int> targetRange, Colour overlayColourInput)
        {
            for (int k = targetRange.Item1; k <= targetRange.Item2; k++)
            {
                stripStack[k] = Colour.OverlayColours(stripStack[k], overlayColourInput);
            }
        }

        public void ClearStack()
        {
            for (int i = 0; i < stripStack.Length; i++)
            {
                stripStack[i] = Colour.Blank();
            }
        }

        public void ClearStack(Colour newColour)
        {
            for (int i = 0; i < stripStack.Length; i++)
            {
                stripStack[i] = newColour;
            }
        }

        public void TestStrip(Colour[] setColours, int offset = 0)
        {
            for (int i = 0; i < stripStack.Length; i++)
            {
                stripStack[i] = setColours[(offset + i) % setColours.Length];
            }
            strip.SetStrip(stripStack);
        }

        public void SetStarPower(bool enabled)
        {
            isStarPowerOn = enabled;
        }

        public void LoadParameters(string filepath)
        {


        }

        //Params
        public LightsConfiguration GetCurrentConfig()
        {
            LightsConfiguration outLc = new LightsConfiguration();

            outLc.UpdatesPerSecond = updatesPerSecond;
            outLc.BootAnimation = BootAnimation;
            outLc.StarPowerAnimates = StarPowerAnimates;
            outLc.StarPowerBacklights = StarPowerBacklights;
            outLc.StarPowerOverridesColours = StarPowerOverridesColours;
            outLc.StarPowerAnimSpeed = StarPowerAnimSpeed;
            outLc.DefinedChannels = LightChannels;
            outLc.FastNotesDetection = FastNotesDetection;
            outLc.FastNotesTimeThreshold = FastNoteTimeThreshold;
            outLc.FastNotesVelocityThreshold = FastNoteVelocityThreshold;
            outLc.FlamNotesDetection = FlamNotesDetection;
            outLc.FlamNotesTimeThreshold = FlamNotesTimeThreshold;
            outLc.FlamNotesVelocityThreshold = FlamNotesVelocityThreshold;
            outLc.HitDecayRate = decayValue;
            outLc.HitMinimumCap = offValueCap;
            outLc.IntensityDecayRate = intensityDecayRate;
            outLc.ColourStarPowerForeground = c_StarPowerForegroundColour;
            outLc.ColourStarPowerBackground = c_StarPowerBackgroundColour;
            outLc.ColourIntensityHighlight = c_IntensityColour;
            outLc.ColoursStarPowerPattern = c_StarPowerBackgroundColourPattern;

            return outLc;
        }

        private void SetCurrentConfig(LightsConfiguration conf)
        {
            this.updatesPerSecond = conf.UpdatesPerSecond;
            this.BootAnimation = conf.BootAnimation;
            this.StarPowerAnimates = conf.StarPowerAnimates;
            this.StarPowerBacklights = conf.StarPowerBacklights;
            this.StarPowerOverridesColours = conf.StarPowerOverridesColours;
            this.StarPowerAnimSpeed = conf.StarPowerAnimSpeed;
            this.LightChannels = conf.DefinedChannels;
            this.FastNotesDetection = conf.FastNotesDetection;
            this.FastNoteTimeThreshold = conf.FastNotesTimeThreshold;
            this.FastNoteVelocityThreshold = conf.FastNotesVelocityThreshold;
            this.FlamNotesDetection = conf.FlamNotesDetection;
            this.FlamNotesTimeThreshold = conf.FlamNotesTimeThreshold;
            this.FlamNotesVelocityThreshold = conf.FlamNotesVelocityThreshold;
            this.decayValue = conf.HitDecayRate;
            this.offValueCap = conf.HitMinimumCap;
            this.intensityDecayRate = conf.IntensityDecayRate;
            this.c_StarPowerForegroundColour = conf.ColourStarPowerForeground;
            this.c_StarPowerBackgroundColour = conf.ColourStarPowerBackground;
            this.c_IntensityColour = conf.ColourIntensityHighlight;
            this.c_StarPowerBackgroundColourPattern = conf.ColoursStarPowerPattern;
            Console.WriteLine("Set lights config");
        }

        ~LightControl()
        {
            //Todo: autosave on close?
        }
    }

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
