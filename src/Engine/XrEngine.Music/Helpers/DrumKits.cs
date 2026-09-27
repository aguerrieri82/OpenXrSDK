namespace XrEngine.Music
{
    public static class DrumKits
    {
        public static DrumKit Gm = new DrumKit
        {
            Pads = [
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.Snare,
                    Triggers = [
                        new()
                        {
                            MidiNote = 38,
                            Type = DrumTriggerType.Head
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
                            MidiNote = 50,
                            Type = DrumTriggerType.Head
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
                            MidiNote = 47,
                            Type = DrumTriggerType.Head
                        },
                        new()
                        {
                            MidiNote = 48,
                            Type = DrumTriggerType.Head
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
                            MidiNote = 45,
                            Type = DrumTriggerType.Head
                        }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.FloorTom,
                    Triggers = [
                        new()
                        {
                            MidiNote = 41,
                            Type = DrumTriggerType.Head
                        },
                        new()
                        {
                            MidiNote = 43,
                            Type = DrumTriggerType.Head
                        }
                    ]
                },
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
                    Type = DrumPadType.Kick,
                    Triggers = [
                        new()
                        {
                            MidiNote = 35,
                            Type = DrumTriggerType.Head
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
                            MidiNote = 46,
                            HiHatState = HiHatState.Open,
                            Type = DrumTriggerType.Head
                        },
                        new()
                        {
                            MidiNote = 44,
                            HiHatState = HiHatState.Pedal,
                            Type = DrumTriggerType.Pedal
                        },
                         new()
                        {
                            MidiNote = 04,
                            HiHatState = HiHatState.Pedal,
                            Type = DrumTriggerType.Pedal
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
                            Type = DrumTriggerType.Head
                        },
                        new()
                        {
                            MidiNote = 53,
                            Type = DrumTriggerType.Bell
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
                            MidiNote = 57,
                            Type = DrumTriggerType.Head
                        }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Cymbal,
                    Type = DrumPadType.Splash,
                    Triggers = [
                        new()
                        {
                            MidiNote = 55,
                            Type = DrumTriggerType.Head
                        }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.Cowbell,
                    Triggers = [
                        new()
                        {
                            MidiNote = 56,
                            Type = DrumTriggerType.Head
                        }
                    ]
                },
                new()
                {
                    Category = DrumPadCategory.Drum,
                    Type = DrumPadType.Tamburine,
                    Triggers = [
                        new()
                        {
                            MidiNote = 54,
                            Type = DrumTriggerType.Head
                        }
                    ]
                },
            ]
        };
    }
}
