from enum import IntEnum
from midiutil import MIDIFile

class GMInstrument(IntEnum):
    # Pianos (0-7)
    AcousticGrandPiano = 0
    BrightAcousticPiano = 1
    ElectricGrandPiano = 2
    HonkyTonkPiano = 3
    ElectricPiano1 = 4
    ElectricPiano2 = 5
    Harpsichord = 6
    Clavinet = 7
    
    # Organs (16-23)
    DrawbarOrgan = 16
    PercussiveOrgan = 17
    RockOrgan = 18
    ChurchOrgan = 19
    ReedOrgan = 20
    Accordion = 21
    Harmonica = 22
    TangoAccordion = 23
    
    # Guitars (24-31)
    AcousticGuitarNylon = 24
    AcousticGuitarSteel = 25
    ElectricGuitarJazz = 26
    ElectricGuitarClean = 27
    ElectricGuitarMuted = 28
    OverdrivenGuitar = 29
    DistortionGuitar = 30
    GuitarHarmonics = 31
    
    # Bass (32-39)
    AcousticBass = 32
    ElectricBassFinger = 33
    ElectricBassPick = 34
    FretlessBass = 35
    SlapBass1 = 36
    SlapBass2 = 37
    SynthBass1 = 38
    SynthBass2 = 39
    
    # Strings & Orchestral (40-55)
    Violin = 40
    Viola = 41
    Cello = 42
    Contrabass = 43
    TremoloStrings = 44
    PizzicatoStrings = 45
    OrchestralHarp = 46
    Timpani = 47
    StringEnsemble1 = 48
    StringEnsemble2 = 49
    SynthStrings1 = 50
    SynthStrings2 = 51
    ChoirAahs = 52
    VoiceOohs = 53
    SynthVoice = 54
    OrchestraHit = 55
    
    # Brass & Wind (56-79)
    Trumpet = 56
    Trombone = 57
    Tuba = 58
    MutedTrumpet = 59
    FrenchHorn = 60
    BrassSection = 61
    SynthBrass1 = 62
    SynthBrass2 = 63
    SopranoSax = 64
    AltoSax = 65
    TenorSax = 66
    BaritoneSax = 67
    Oboe = 68
    EnglishHorn = 69
    Bassoon = 70
    Clarinet = 71
    Piccolo = 72
    Flute = 73
    Recorder = 74
    PanFlute = 75
    BlownBottle = 76
    Shakuhachi = 77
    Whistle = 78
    Ocarina = 79
    
    # Synthesizers (Leads & Pads) (80-95)
    Lead1Square = 80
    Lead2Sawtooth = 81
    Lead3Calliope = 82
    Lead4Chiff = 83
    Lead5Charang = 84
    Lead6Voice = 85
    Lead7Fifths = 86
    Lead8BassAndLead = 87
    Pad1NewAge = 88
    Pad2Warm = 89
    Pad3Polysynth = 90
    Pad4Choir = 91
    Pad5Bowed = 92
    Pad6Metallic = 93
    Pad7Halo = 94
    Pad8Sweep = 95

class DrumNote(IntEnum):
    # Standard MIDI Percussion Key Map (Channel 10 / Index 9)
    AcousticBassDrum = 35
    BassDrum1 = 36
    SideStick = 37
    AcousticSnare = 38
    HandClap = 39
    ElectricSnare = 40
    LowFloorTom = 41
    ClosedHiHat = 42
    HighFloorTom = 43
    PedalHiHat = 44
    LowTom = 45
    OpenHiHat = 46
    LowMidTom = 47
    HiMidTom = 48
    CrashCymbal1 = 49
    HighTom = 50
    RideCymbal1 = 51
    ChineseCymbal = 52
    RideBell = 53
    Tambourine = 54
    SplashCymbal = 55
    Cowbell = 56
    CrashCymbal2 = 57
    Vibraslap = 58
    RideCymbal2 = 59
    HiBongo = 60
    LowBongo = 61
    MuteHiConga = 62
    OpenHiConga = 63
    LowConga = 64
    HighTimbale = 65
    LowTimbale = 66

class MidiBuilder:
    """A helper class to easily construct MIDI files using named instruments and drums."""
    
    def __init__(self, num_tracks=1, tempo=120):
        self.midi = MIDIFile(num_tracks, deinterleave=False)
        self.tempo = tempo
        self.num_tracks = num_tracks
        for i in range(num_tracks):
            self.midi.addTempo(i, 0, tempo)
            
    def add_track(self, track_idx, name, instrument: GMInstrument = None, channel=0):
        """Initializes a track with a name and an optional General MIDI instrument."""
        self.midi.addTrackName(track_idx, 0, name)
        if instrument is not None:
            self.midi.addProgramChange(track_idx, channel, 0, instrument.value)
            
    def add_note(self, track_idx, channel, pitch, time, duration, volume=100):
        """Adds a standard pitched note to a track."""
        self.midi.addNote(track_idx, channel, pitch, time, duration, volume)
        
    def add_drum_note(self, track_idx, drum_note: DrumNote, time, duration=1, volume=100):
        """Adds a drum hit. Automatically uses MIDI channel 9 (percussion)."""
        self.midi.addNote(track_idx, 9, drum_note.value, time, duration, volume)
        
    def add_pitch_bend(self, track_idx, channel, time, value):
        """Adds a pitch bend event. Value should be between -8192 and 8191."""
        self.midi.addPitchWheelEvent(track_idx, channel, time, value)

    def save(self, filename):
        """Writes the MIDI data to a file."""
        with open(filename, "wb") as output_file:
            self.midi.writeFile(output_file)
        print(f"MIDI file generated: {filename}")
