namespace XrEngine.Music
{
    public static class DrumKits
    {
        public static DrumKit Td9 = new DrumKit
        {
            Name = "Roland TD9",
            Pads = [
               new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.Kick,
                    Triggers = [
                        new()
                        {
                            MidiNote = 36,
                            Type = DrumTriggerType.Head
                        }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.Snare,
                    Triggers = [
                        new()
                        {
                            MidiNote = 38,
                            Type = DrumTriggerType.Head
                        },
                        new()
                        {
                            MidiNote = 40,
                            Type = DrumTriggerType.Rim
                        },
                        new()
                        {
                            MidiNote = 37,
                            Type = DrumTriggerType.Rim
                        }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.HiTom,
                    Triggers = [
                        new()
                        {
                            MidiNote = 48,
                            Type = DrumTriggerType.Head
                        },
                        new()
                        {
                            MidiNote = 50,
                            Type = DrumTriggerType.Rim
                        }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.MidTom,
                    Triggers = [
                        new()
                        {
                            MidiNote = 45,
                            Type = DrumTriggerType.Head
                        },
                        new()
                        {
                            MidiNote = 47,
                            Type = DrumTriggerType.Rim
                        }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.LowTom,
                    Triggers = [
                        new()
                        {
                            MidiNote = 43,
                            Type = DrumTriggerType.Head
                        },
                        new()
                        {
                            MidiNote = 58,
                            Type = DrumTriggerType.Rim
                        }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Cymbal,
                    Type = DrumPadType.HiHat,
                    Triggers = [
                        new()
                        {
                            MidiNote = 42,
                            HiHatState = HiHatState.Closed,
                            Type = DrumTriggerType.Head
                        },
                        new()
                        {
                            MidiNote = 22,
                            HiHatState = HiHatState.Closed,
                            Type = DrumTriggerType.Edge
                        },
                        new()
                        {
                            MidiNote = 46,
                            HiHatState = HiHatState.Open,
                            Type = DrumTriggerType.Head
                        },
                        new()
                        {
                            MidiNote = 26,
                            HiHatState = HiHatState.Open,
                            Type = DrumTriggerType.Edge
                        },
                        new()
                        {
                            MidiNote = 44,
                            HiHatState = HiHatState.Pedal,
                            Type = DrumTriggerType.Pedal
                        }
                    ],
                     Controls = [
                        new() 
                        { 
                            MidiCode = 4, 
                            Type = DrumControlType.PedalPosition 
                        }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Cymbal,
                    Type = DrumPadType.Crash,
                    Triggers = [
                        new()
                        {
                            MidiNote = 49,
                            Type = DrumTriggerType.Head
                        },
                        new()
                        {
                            MidiNote = 55,
                            Type = DrumTriggerType.Edge
                        }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Cymbal,
                    Type = DrumPadType.Crash,
                    Triggers = [
                        new()
                        {
                            MidiNote = 57,
                            Type = DrumTriggerType.Head
                        },
                        new()
                        {
                            MidiNote = 52,
                            Type = DrumTriggerType.Edge
                        }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Cymbal,
                    Type = DrumPadType.Ride,
                    Triggers = [
                        new()
                        {
                            MidiNote = 51,
                            Type = DrumTriggerType.Head
                        },
                        new()
                        {
                            MidiNote = 59,
                            Type = DrumTriggerType.Edge
                        },
                        new()
                        {
                            MidiNote = 53,
                            Type = DrumTriggerType.Bell
                        }
                    ]
                }
           ]
        };

        public static DrumKit NakedDrums = new DrumKit
        {
            Name = "Naked Drums",
            Switches = [
                new()
                {
                    Key = 25,
                    Name = DrumKitSwitches.VariableHiHatOn,
                },
                new()
                {
                    Key = 24,
                    Name = DrumKitSwitches.VariableHiHatOff,
                }
            ],
            Pads = [
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.Kick,
                    Triggers = [
                        new() { MidiNote = 36, Type = DrumTriggerType.Head }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.Snare,
                    Triggers = [
                        new() { MidiNote = 37, Type = DrumTriggerType.Rim },
                        new() { MidiNote = 38, Type = DrumTriggerType.Head },
                        new() { MidiNote = 40, Type = DrumTriggerType.Head }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.HiTom,
                    Triggers = [
                        new() { MidiNote = 48, Type = DrumTriggerType.Head }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.MidTom,
                    Triggers = [
                        new() { MidiNote = 47, Type = DrumTriggerType.Head }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.LowTom,
                    Triggers = [
                        new() { MidiNote = 45, Type = DrumTriggerType.Head }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.FloorTom,
                    Triggers = [
                        new() { MidiNote = 43, Type = DrumTriggerType.Head },
                        new() { MidiNote = 41, Type = DrumTriggerType.Head }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Cymbal,
                    Type = DrumPadType.HiHat,
                    Triggers = [
                        new() { MidiNote = 42, Type = DrumTriggerType.Head, HiHatState = HiHatState.Closed },
                        new() { MidiNote = 54, Type = DrumTriggerType.Head, HiHatState = HiHatState.Closed },
                        new() { MidiNote = 56, Type = DrumTriggerType.Head },
                        new() { MidiNote = 46, Type = DrumTriggerType.Head, HiHatState = HiHatState.Open },
                        new() { MidiNote = 44, Type = DrumTriggerType.Pedal, HiHatState = HiHatState.Pedal },
                    ],
                    Controls = [
                        new() { MidiCode = 4, Type = DrumControlType.PedalPosition }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Cymbal,
                    Type = DrumPadType.Ride,
                    Triggers = [
                        new() { MidiNote = 51, Type = DrumTriggerType.Head },
                        new() { MidiNote = 53, Type = DrumTriggerType.Bell },
                        new() { MidiNote = 60, Type = DrumTriggerType.Choke }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Cymbal,
                    Type = DrumPadType.Crash,
                    Triggers = [
                        new() { MidiNote = 49, Type = DrumTriggerType.Edge },
                        new() { MidiNote = 58, Type = DrumTriggerType.Bell },
                        new() { MidiNote = 61, Type = DrumTriggerType.Choke }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Cymbal,
                    Type = DrumPadType.Crash,
                    Triggers = [
                        new() { MidiNote = 57, Type = DrumTriggerType.Edge },
                        new() { MidiNote = 59, Type = DrumTriggerType.Bell },
                        new() { MidiNote = 62, Type = DrumTriggerType.Choke }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Cymbal,
                    Type = DrumPadType.Splash,
                    Triggers = [
                        new() { MidiNote = 55, Type = DrumTriggerType.Head },
                        new() { MidiNote = 63, Type = DrumTriggerType.Choke }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Cymbal,
                    Type = DrumPadType.China,
                    Triggers = [
                        new() { MidiNote = 52, Type = DrumTriggerType.Head },
                        new() { MidiNote = 65, Type = DrumTriggerType.Choke }
                    ]
                }
            ]
        };


        public static DrumKit Gm = new DrumKit
        {
            Name = "General MIDI",
            Pads = [
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.Kick,
                    Triggers = [
                        new() { MidiNote = 35, Type = DrumTriggerType.Head },
                        new() { MidiNote = 36, Type = DrumTriggerType.Head }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.Snare,
                    Triggers = [
                        new() { MidiNote = 37, Type = DrumTriggerType.Rim },
                        new() { MidiNote = 38, Type = DrumTriggerType.Head },
                        new() { MidiNote = 40, Type = DrumTriggerType.Head }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.HiTom,
                    Triggers = [
                        new() { MidiNote = 50, Type = DrumTriggerType.Head }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.MidTom,
                    Triggers = [
                        new() { MidiNote = 48, Type = DrumTriggerType.Head },
                        new() { MidiNote = 47, Type = DrumTriggerType.Head }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.LowTom,
                    Triggers = [
                        new() { MidiNote = 45, Type = DrumTriggerType.Head }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.FloorTom,
                    Triggers = [
                        new() { MidiNote = 43, Type = DrumTriggerType.Head },
                        new() { MidiNote = 41, Type = DrumTriggerType.Head }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Cymbal,
                    Type = DrumPadType.HiHat,
                    Triggers = [
                        new() { MidiNote = 42, Type = DrumTriggerType.Head, HiHatState = HiHatState.Closed },
                        new() { MidiNote = 44, Type = DrumTriggerType.Pedal, HiHatState = HiHatState.Pedal },
                        new() { MidiNote = 46, Type = DrumTriggerType.Head, HiHatState = HiHatState.Open }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Cymbal,
                    Type = DrumPadType.Ride,
                    Triggers = [
                        new() { MidiNote = 51, Type = DrumTriggerType.Head },
                        new() { MidiNote = 53, Type = DrumTriggerType.Bell },
                        new() { MidiNote = 59, Type = DrumTriggerType.Head }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Cymbal,
                    Type = DrumPadType.Crash,
                    Triggers = [
                        new() { MidiNote = 49, Type = DrumTriggerType.Head }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Cymbal,
                    Type = DrumPadType.Crash,
                    Triggers = [
                        new() { MidiNote = 57, Type = DrumTriggerType.Head }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Cymbal,
                    Type = DrumPadType.Splash,
                    Triggers = [
                        new() { MidiNote = 55, Type = DrumTriggerType.Head }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Cymbal,
                    Type = DrumPadType.China,
                    Triggers = [
                        new() { MidiNote = 52, Type = DrumTriggerType.Head }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.Cowbell,
                    Triggers = [
                        new() { MidiNote = 56, Type = DrumTriggerType.Head }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.Tamburine,
                    Triggers = [
                        new() { MidiNote = 54, Type = DrumTriggerType.Head }
                    ]
                }
            ]
        };

    }

}
