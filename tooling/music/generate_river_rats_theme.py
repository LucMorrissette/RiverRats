"""
River Rats Theme Song Generator
================================
Upbeat jangly rock song — energetic, fun, outdoorsy feel.
Think early Kings of Leon jangle meets Modest Mouse energy.

Song Structure (56 measures @ 144 BPM ≈ 1:33):
  Intro (4 bars) → Verse 1 (8) → Chorus 1 (8) → Verse 2 (8) →
  Chorus 2 (8) → Bridge (8) → Final Chorus (8) → Outro (4)
"""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from midi_utils import MidiBuilder, GMInstrument, DrumNote

# ── Constants ──────────────────────────────────────────────────────────────────
TEMPO = 144
BEATS_PER_MEASURE = 4

# Track indices
BASS = 0
JANGLE_GTR = 1
DRUMS = 2
RHYTHM_GTR = 3

# Bass channel / Guitar channels
BASS_CH = 0
JANGLE_CH = 1
RHYTHM_CH = 3  # channel 2 reserved, use 3

# ── Chord voicings (MIDI pitches) ─────────────────────────────────────────────
# Lower voicings for bass reference
CHORD_ROOTS = {"G": 43, "D": 38, "Em": 40, "C": 36, "Am": 33}

# Jangly guitar voicings (mid-high register)
JANGLE_CHORDS = {
    "G":  [55, 59, 62, 67, 74],   # G3 B3 D4 G4 D5
    "D":  [54, 57, 62, 66, 74],   # F#3 A3 D4 F#4 D5
    "Em": [52, 55, 59, 64, 71],   # E3 G3 B3 E4 B4
    "C":  [48, 52, 55, 60, 67],   # C3 E3 G3 C4 G4
    "Am": [45, 52, 57, 60, 69],   # A2 E3 A3 C4 A4
}

# High-register arpeggio notes for the jangly rolling banjo-esque sound.
# Only notes in octave 4+ (MIDI 60+) to avoid clashing with bass guitar.
JANGLE_ARPEGGIO_NOTES = {
    "G":  [62, 67, 71, 74, 79],   # D4 G4 B4 D5 G5
    "D":  [62, 66, 69, 74, 78],   # D4 F#4 A4 D5 F#5
    "Em": [64, 67, 71, 76, 79],   # E4 G4 B4 E5 G5
    "C":  [60, 64, 67, 72, 76],   # C4 E4 G4 C5 E5
    "Am": [60, 64, 69, 72, 76],   # C4 E4 A4 C5 E5
}

# Rhythm guitar power chords (lower, thicker)
RHYTHM_CHORDS = {
    "G":  [43, 50, 55],   # G2 D3 G3
    "D":  [38, 45, 50],   # D2 A2 D3
    "Em": [40, 47, 52],   # E2 B2 E3
    "C":  [36, 43, 48],   # C2 G2 C3
    "Am": [33, 40, 45],   # A1 E2 A2
}

# ── Section definitions ────────────────────────────────────────────────────────
VERSE_CHORDS  = ["G", "D", "Em", "C"]     # 2 bars each
CHORUS_CHORDS = ["C", "G", "D", "Em"]     # 2 bars each
BRIDGE_CHORDS = ["Am", "Em", "C", "D"]    # 2 bars each

# ── G-major-scale bass notes per chord ────────────────────────────────────────
# Every pitch is guaranteed to be in G major (G A B C D E F#).
# Keys: root, 2nd, 3rd, 5th, 6th, 7th, oct  (intervals relative to chord root
# but snapped to the G major scale).
#
# Reference pitches in G major:
#   Octave 1: G=31 A=33 B=35
#   Octave 2: C=36 D=38 E=40 F#=42 G=43 A=45 B=47
#   Octave 3: C=48 D=50 E=52 F#=54 G=55
BASS_NOTES = {
    "G":  {"root": 43, "2nd": 45, "3rd": 47, "5th": 50, "6th": 52, "7th": 54, "oct": 55},
    "D":  {"root": 38, "2nd": 40, "3rd": 42, "5th": 45, "6th": 47, "7th": 48, "oct": 50},
    "Em": {"root": 40, "2nd": 42, "3rd": 43, "5th": 47, "6th": 48, "7th": 50, "oct": 52},
    "C":  {"root": 36, "2nd": 38, "3rd": 40, "5th": 43, "6th": 45, "7th": 47, "oct": 48},
    "Am": {"root": 33, "2nd": 35, "3rd": 36, "5th": 40, "6th": 42, "7th": 43, "oct": 45},
}


# ═══════════════════════════════════════════════════════════════════════════════
#  BASS PATTERNS
# ═══════════════════════════════════════════════════════════════════════════════

def bass_verse_pattern(builder, start, chord, variation=False):
    """Bouncy bass: root-fifth with scale-tone walk-ups. 2 measures."""
    n = BASS_NOTES[chord]
    root, fifth, octave = n["root"], n["5th"], n["oct"]

    if not variation:
        # Measure 1: root on 1, fifth on 'and' of 2, root on 3, scale walk on 4
        builder.add_note(BASS, BASS_CH, root, start, 1.0, 100)
        builder.add_note(BASS, BASS_CH, fifth, start + 1.5, 0.5, 85)
        builder.add_note(BASS, BASS_CH, root, start + 2.0, 0.75, 95)
        builder.add_note(BASS, BASS_CH, n["2nd"], start + 3.0, 0.5, 80)
        builder.add_note(BASS, BASS_CH, n["3rd"], start + 3.5, 0.5, 80)
        # Measure 2: octave pop, walk down through scale
        builder.add_note(BASS, BASS_CH, octave, start + 4.0, 0.5, 105)
        builder.add_note(BASS, BASS_CH, root, start + 4.5, 0.5, 85)
        builder.add_note(BASS, BASS_CH, fifth, start + 5.0, 1.0, 90)
        builder.add_note(BASS, BASS_CH, n["3rd"], start + 6.0, 0.5, 80)
        builder.add_note(BASS, BASS_CH, n["2nd"], start + 6.5, 0.5, 75)
        builder.add_note(BASS, BASS_CH, root, start + 7.0, 0.5, 80)
        builder.add_note(BASS, BASS_CH, n["7th"] - 12, start + 7.5, 0.5, 85)
    else:
        # Variation: more syncopated, octave jumps
        builder.add_note(BASS, BASS_CH, root, start, 0.75, 100)
        builder.add_note(BASS, BASS_CH, octave, start + 0.75, 0.25, 90)
        builder.add_note(BASS, BASS_CH, root, start + 1.0, 0.5, 85)
        builder.add_note(BASS, BASS_CH, fifth, start + 1.5, 0.75, 80)
        builder.add_note(BASS, BASS_CH, n["2nd"], start + 2.5, 0.5, 85)
        builder.add_note(BASS, BASS_CH, root, start + 3.0, 0.5, 95)
        builder.add_note(BASS, BASS_CH, n["7th"] - 12, start + 3.5, 0.5, 80)
        # Measure 2
        builder.add_note(BASS, BASS_CH, root, start + 4.0, 0.5, 100)
        builder.add_note(BASS, BASS_CH, octave, start + 4.5, 0.25, 95)
        builder.add_note(BASS, BASS_CH, fifth, start + 5.0, 0.75, 85)
        builder.add_note(BASS, BASS_CH, n["3rd"], start + 5.75, 0.25, 75)
        builder.add_note(BASS, BASS_CH, n["2nd"], start + 6.0, 0.5, 80)
        builder.add_note(BASS, BASS_CH, root, start + 6.5, 0.5, 85)
        builder.add_note(BASS, BASS_CH, n["7th"] - 12, start + 7.0, 0.5, 80)
        builder.add_note(BASS, BASS_CH, n["6th"] - 12, start + 7.5, 0.5, 75)


def bass_chorus_pattern(builder, start, chord):
    """Driving 8th-note bass with octave pops on beat 3. 2 measures."""
    root = CHORD_ROOTS[chord]
    fifth = root + 7
    octave = root + 12

    for m in range(2):
        t = start + m * 4
        # Driving 8th notes
        builder.add_note(BASS, BASS_CH, root, t, 0.5, 100)
        builder.add_note(BASS, BASS_CH, root, t + 0.5, 0.5, 80)
        builder.add_note(BASS, BASS_CH, root, t + 1.0, 0.5, 95)
        builder.add_note(BASS, BASS_CH, fifth, t + 1.5, 0.5, 80)
        # Octave pop on beat 3
        builder.add_note(BASS, BASS_CH, octave, t + 2.0, 0.5, 110)
        builder.add_note(BASS, BASS_CH, root, t + 2.5, 0.5, 85)
        builder.add_note(BASS, BASS_CH, fifth, t + 3.0, 0.5, 90)
        builder.add_note(BASS, BASS_CH, root, t + 3.5, 0.5, 80)


def bass_bridge_pattern(builder, start, chord):
    """Sustained roots with scale-tone fills on beat 4. 2 measures."""
    n = BASS_NOTES[chord]
    root, fifth = n["root"], n["5th"]

    # Measure 1: sustained whole note feel
    builder.add_note(BASS, BASS_CH, root, start, 2.5, 95)
    builder.add_note(BASS, BASS_CH, n["2nd"], start + 3.0, 0.5, 75)
    builder.add_note(BASS, BASS_CH, n["3rd"], start + 3.5, 0.5, 80)
    # Measure 2: sustained then walk
    builder.add_note(BASS, BASS_CH, root, start + 4.0, 2.5, 90)
    builder.add_note(BASS, BASS_CH, fifth, start + 6.5, 0.5, 75)
    builder.add_note(BASS, BASS_CH, n["3rd"], start + 7.0, 0.5, 80)
    builder.add_note(BASS, BASS_CH, n["2nd"], start + 7.5, 0.5, 85)


def bass_intro(builder, start):
    """Sparse bass for intro — just roots on 1 for last 2 bars."""
    # Bars 3-4 of intro (bass enters)
    builder.add_note(BASS, BASS_CH, CHORD_ROOTS["G"], start + 8, 3.5, 85)
    builder.add_note(BASS, BASS_CH, CHORD_ROOTS["D"], start + 12, 3.5, 85)


def bass_outro(builder, start):
    """Slowing down — longer notes."""
    builder.add_note(BASS, BASS_CH, CHORD_ROOTS["G"], start, 4.0, 90)
    builder.add_note(BASS, BASS_CH, CHORD_ROOTS["C"], start + 4, 4.0, 85)
    builder.add_note(BASS, BASS_CH, CHORD_ROOTS["D"], start + 8, 4.0, 80)
    builder.add_note(BASS, BASS_CH, CHORD_ROOTS["G"], start + 12, 4.0, 75)


# ═══════════════════════════════════════════════════════════════════════════════
#  JANGLY GUITAR PATTERNS
# ═══════════════════════════════════════════════════════════════════════════════

def jangle_arpeggio(builder, start, chord, measures=2):
    """Picked arpeggio pattern — the jangly signature. For verses."""
    notes = JANGLE_ARPEGGIO_NOTES[chord]
    for m in range(measures):
        t = start + m * 4
        # 16th-note arpeggio pattern: up-down-up-down with accents
        pattern_indices = [0, 2, 4, 3, 1, 3, 4, 2,
                           0, 3, 4, 2, 1, 4, 3, 0]
        for i, idx in enumerate(pattern_indices):
            note_time = t + i * 0.25
            vel = 95 if i % 4 == 0 else (80 if i % 2 == 0 else 70)
            builder.add_note(JANGLE_GTR, JANGLE_CH, notes[idx % len(notes)],
                             note_time, 0.25, vel)


def jangle_strum(builder, start, chord, measures=2):
    """Strummed chord pattern — for choruses. More energy."""
    notes = JANGLE_CHORDS[chord]
    for m in range(measures):
        t = start + m * 4
        # Strummed 8th-note pattern with accents on 1 and 3
        for beat in range(8):
            note_time = t + beat * 0.5
            vel = 105 if beat % 4 == 0 else (90 if beat % 2 == 0 else 78)
            # Strum: slight delay between notes (rake effect)
            for j, pitch in enumerate(notes):
                builder.add_note(JANGLE_GTR, JANGLE_CH, pitch,
                                 note_time + j * 0.02, 0.45, vel - j * 2)


def jangle_intro_arpeggio(builder, start):
    """Intro: guitar alone with a sparse, atmospheric high-register arpeggio."""
    # 4 bars of intro arpeggio over G and D
    chords_intro = ["G", "G", "D", "D"]
    for i, chord in enumerate(chords_intro):
        notes = JANGLE_ARPEGGIO_NOTES[chord]
        t = start + i * 4
        # Sparser pattern — dotted rhythm, let notes ring. All high register.
        pattern = [
            (0.0, notes[1], 1.0, 90),    # mid-high
            (0.75, notes[2], 0.75, 80),
            (1.5, notes[4], 1.0, 85),
            (2.25, notes[3], 0.75, 75),
            (3.0, notes[0], 0.5, 80),     # lowest of high set
            (3.5, notes[4], 0.5, 85),
        ]
        for offset, pitch, dur, vel in pattern:
            builder.add_note(JANGLE_GTR, JANGLE_CH, pitch, t + offset, dur, vel)


def jangle_bridge(builder, start, chord):
    """Bridge: arpeggiated but slower, building. 2 measures."""
    notes = JANGLE_ARPEGGIO_NOTES[chord]
    for m in range(2):
        t = start + m * 4
        # Quarter-note arpeggios, letting notes ring longer
        arp = [0, 2, 4, 3, 1, 3, 4, 2]
        for i, idx in enumerate(arp):
            note_time = t + i * 0.5
            vel = 85 + (m * 5)  # build volume in second measure
            builder.add_note(JANGLE_GTR, JANGLE_CH, notes[idx % len(notes)],
                             note_time, 0.75, vel)


def jangle_outro(builder, start):
    """Outro: gradually longer, ringing chords."""
    chords = ["G", "C", "D", "G"]
    durations = [3.0, 3.5, 4.0, 4.0]
    t = start
    for chord, dur in zip(chords, durations):
        notes = JANGLE_CHORDS[chord]
        for j, pitch in enumerate(notes):
            builder.add_note(JANGLE_GTR, JANGLE_CH, pitch,
                             t + j * 0.03, dur - 0.5, 85 - j * 2)
        t += 4


# ═══════════════════════════════════════════════════════════════════════════════
#  RHYTHM GUITAR PATTERNS
# ═══════════════════════════════════════════════════════════════════════════════

def rhythm_power_chords(builder, start, chord, measures=2, volume=90):
    """Chunky power chords on 8th notes with palm-mute feel."""
    notes = RHYTHM_CHORDS[chord]
    for m in range(measures):
        t = start + m * 4
        # Driving 8th-note power chords, accent on 1 and 3
        for beat in range(8):
            note_time = t + beat * 0.5
            vel = volume if beat % 4 == 0 else (volume - 12 if beat % 2 == 0 else volume - 20)
            for pitch in notes:
                builder.add_note(RHYTHM_GTR, RHYTHM_CH, pitch,
                                 note_time, 0.4, vel)


def rhythm_sustained(builder, start, chord, measures=2, volume=80):
    """Sustained whole chords for bridge/quiet sections."""
    notes = RHYTHM_CHORDS[chord]
    for m in range(measures):
        t = start + m * 4
        for pitch in notes:
            builder.add_note(RHYTHM_GTR, RHYTHM_CH, pitch, t, 3.8, volume)


def rhythm_outro(builder, start):
    """Outro rhythm: long sustained chords fading."""
    chords = ["G", "C", "D", "G"]
    t = start
    for i, chord in enumerate(chords):
        notes = RHYTHM_CHORDS[chord]
        vol = 80 - i * 10
        for pitch in notes:
            builder.add_note(RHYTHM_GTR, RHYTHM_CH, pitch, t, 3.8, vol)
        t += 4


# ═══════════════════════════════════════════════════════════════════════════════
#  DRUM PATTERNS
# ═══════════════════════════════════════════════════════════════════════════════

D = DrumNote  # shorthand


def drums_verse_beat(builder, start, measures=2):
    """Driving verse beat with 16th-note hi-hats and ghost snares."""
    for m in range(measures):
        t = start + m * 4
        # Kick pattern: 1, 'and' of 2, 3
        for k_offset in [0.0, 1.5, 2.0]:
            builder.add_drum_note(DRUMS, D.BassDrum1, t + k_offset, 0.25, 105)
        # Extra kick ghost on 'e' of 4
        builder.add_drum_note(DRUMS, D.BassDrum1, t + 3.25, 0.25, 75)

        # Snare on 2 and 4
        builder.add_drum_note(DRUMS, D.AcousticSnare, t + 1.0, 0.25, 110)
        builder.add_drum_note(DRUMS, D.AcousticSnare, t + 3.0, 0.25, 110)
        # Ghost snares
        builder.add_drum_note(DRUMS, D.AcousticSnare, t + 0.75, 0.25, 45)
        builder.add_drum_note(DRUMS, D.AcousticSnare, t + 2.5, 0.25, 40)
        builder.add_drum_note(DRUMS, D.SideStick, t + 3.75, 0.25, 50)

        # 16th-note hi-hats
        for i in range(16):
            ht = t + i * 0.25
            vel = 90 if i % 4 == 0 else (70 if i % 2 == 0 else 55)
            builder.add_drum_note(DRUMS, D.ClosedHiHat, ht, 0.25, vel)

        # Open hi-hat on 'and' of 4 (last 16th pair)
        builder.add_drum_note(DRUMS, D.OpenHiHat, t + 3.5, 0.25, 85)


def drums_chorus_beat(builder, start, measures=2, use_ride=False):
    """Chorus beat: more energy, ride cymbal option, accented kick."""
    hat = D.RideCymbal1 if use_ride else D.ClosedHiHat
    for m in range(measures):
        t = start + m * 4
        # Kick: driving 4-on-floor with extra syncopation
        for k_offset in [0.0, 1.0, 1.5, 2.0, 3.0]:
            vel = 110 if k_offset in [0.0, 2.0] else 90
            builder.add_drum_note(DRUMS, D.BassDrum1, t + k_offset, 0.25, vel)

        # Snare on 2 and 4, plus ghost
        builder.add_drum_note(DRUMS, D.AcousticSnare, t + 1.0, 0.25, 115)
        builder.add_drum_note(DRUMS, D.AcousticSnare, t + 3.0, 0.25, 115)
        builder.add_drum_note(DRUMS, D.AcousticSnare, t + 2.5, 0.25, 45)

        # Hi-hat / ride 8th notes
        for i in range(8):
            ht = t + i * 0.5
            vel = 95 if i % 2 == 0 else 75
            builder.add_drum_note(DRUMS, hat, ht, 0.25, vel)

        # Open hi-hat at end of measure (if not ride)
        if not use_ride:
            builder.add_drum_note(DRUMS, D.OpenHiHat, t + 3.5, 0.25, 90)


def drums_bridge_halftime(builder, start, measures=2, intensity=0):
    """Half-time bridge beat, building intensity."""
    for m in range(measures):
        t = start + m * 4
        vol_boost = intensity + m * 5

        # Kick on 1 and 3 only (half-time feel)
        builder.add_drum_note(DRUMS, D.BassDrum1, t, 0.25, 95 + vol_boost)
        builder.add_drum_note(DRUMS, D.BassDrum1, t + 2.0, 0.25, 90 + vol_boost)

        # Snare on 3 only (half-time)
        builder.add_drum_note(DRUMS, D.AcousticSnare, t + 2.0, 0.25, 100 + vol_boost)

        # Ride bell quarter notes
        for i in range(4):
            builder.add_drum_note(DRUMS, D.RideBell, t + i, 0.25, 70 + vol_boost)

        # Ghost snare texture
        builder.add_drum_note(DRUMS, D.AcousticSnare, t + 1.0, 0.25, 35 + vol_boost)
        builder.add_drum_note(DRUMS, D.AcousticSnare, t + 3.5, 0.25, 40 + vol_boost)


# ── Drum Fills ────────────────────────────────────────────────────────────────

def fill_tom_cascade(builder, start):
    """16th-note tom cascade: HighTom → HiMidTom → LowMidTom → LowTom → Crash."""
    toms = [D.HighTom, D.HighTom, D.HiMidTom, D.HiMidTom,
            D.LowMidTom, D.LowMidTom, D.LowTom, D.LowTom]
    for i, tom in enumerate(toms):
        builder.add_drum_note(DRUMS, tom, start + i * 0.125, 0.125, 90 + i * 2)
    # Crash at end
    builder.add_drum_note(DRUMS, D.CrashCymbal1, start + 1.0, 0.5, 120)
    builder.add_drum_note(DRUMS, D.BassDrum1, start + 1.0, 0.25, 110)


def fill_snare_roll_crescendo(builder, start, duration=1.0):
    """Snare roll building in volume."""
    num_hits = int(duration / 0.125)
    for i in range(num_hits):
        vel = 50 + int((70 * i) / num_hits)
        builder.add_drum_note(DRUMS, D.AcousticSnare, start + i * 0.125, 0.125, vel)
    builder.add_drum_note(DRUMS, D.CrashCymbal1, start + duration, 0.5, 120)
    builder.add_drum_note(DRUMS, D.BassDrum1, start + duration, 0.25, 110)


def fill_kick_snare_tom_triplets(builder, start):
    """Kick-snare-tom triplet pattern over 1 beat."""
    triplet = 1.0 / 3.0
    pattern = [D.BassDrum1, D.AcousticSnare, D.HiMidTom,
               D.BassDrum1, D.AcousticSnare, D.LowMidTom]
    for i, drum in enumerate(pattern):
        builder.add_drum_note(DRUMS, drum, start + i * triplet, triplet * 0.9, 95)
    builder.add_drum_note(DRUMS, D.CrashCymbal1, start + 2.0, 0.5, 115)


def fill_syncopated_ghost(builder, start):
    """Syncopated ghost snare pattern between main hits. 1 measure."""
    # Main hits
    builder.add_drum_note(DRUMS, D.BassDrum1, start, 0.25, 110)
    builder.add_drum_note(DRUMS, D.AcousticSnare, start + 1.0, 0.25, 110)
    builder.add_drum_note(DRUMS, D.BassDrum1, start + 2.0, 0.25, 105)
    builder.add_drum_note(DRUMS, D.AcousticSnare, start + 3.0, 0.25, 110)
    # Ghost notes in between
    ghosts = [0.5, 0.75, 1.5, 2.25, 2.75, 3.25, 3.5, 3.75]
    for g in ghosts:
        builder.add_drum_note(DRUMS, D.AcousticSnare, start + g, 0.125, 40)
    # Toms at end
    builder.add_drum_note(DRUMS, D.HiMidTom, start + 3.5, 0.25, 90)
    builder.add_drum_note(DRUMS, D.LowTom, start + 3.75, 0.25, 90)


def fill_big_transition(builder, start):
    """Big 2-beat transition fill with toms and crash. Use at section ends."""
    # Beat 3: fast snare-tom burst
    builder.add_drum_note(DRUMS, D.AcousticSnare, start, 0.125, 100)
    builder.add_drum_note(DRUMS, D.AcousticSnare, start + 0.125, 0.125, 105)
    builder.add_drum_note(DRUMS, D.HighTom, start + 0.25, 0.125, 100)
    builder.add_drum_note(DRUMS, D.HighTom, start + 0.375, 0.125, 105)
    builder.add_drum_note(DRUMS, D.HiMidTom, start + 0.5, 0.125, 100)
    builder.add_drum_note(DRUMS, D.HiMidTom, start + 0.625, 0.125, 105)
    builder.add_drum_note(DRUMS, D.LowMidTom, start + 0.75, 0.125, 105)
    builder.add_drum_note(DRUMS, D.LowTom, start + 0.875, 0.125, 110)
    # Beat 4: kick flam + crash
    builder.add_drum_note(DRUMS, D.BassDrum1, start + 1.0, 0.25, 115)
    builder.add_drum_note(DRUMS, D.CrashCymbal1, start + 1.0, 0.5, 120)
    builder.add_drum_note(DRUMS, D.AcousticSnare, start + 1.25, 0.25, 100)
    builder.add_drum_note(DRUMS, D.BassDrum1, start + 1.5, 0.25, 100)
    builder.add_drum_note(DRUMS, D.LowTom, start + 1.75, 0.25, 105)


def fill_bridge_crescendo(builder, start):
    """Big crescendo fill for end of bridge. 2 measures of building snare + toms."""
    # Measure 1: 8th-note snare roll building
    for i in range(8):
        vel = 60 + i * 6
        builder.add_drum_note(DRUMS, D.AcousticSnare, start + i * 0.5, 0.25, vel)
    builder.add_drum_note(DRUMS, D.BassDrum1, start, 0.25, 100)
    builder.add_drum_note(DRUMS, D.BassDrum1, start + 2.0, 0.25, 105)

    # Measure 2: 16th-note tom cascade with crescendo
    t2 = start + 4.0
    toms_seq = [D.AcousticSnare, D.HighTom, D.AcousticSnare, D.HighTom,
                D.HiMidTom, D.AcousticSnare, D.HiMidTom, D.LowMidTom,
                D.AcousticSnare, D.LowMidTom, D.LowTom, D.AcousticSnare,
                D.LowTom, D.LowTom, D.BassDrum1, D.BassDrum1]
    for i, drum in enumerate(toms_seq):
        vel = 70 + i * 3
        builder.add_drum_note(DRUMS, drum, t2 + i * 0.25, 0.2, vel)
    # Final crash
    builder.add_drum_note(DRUMS, D.CrashCymbal1, t2 + 4.0, 0.5, 127)
    builder.add_drum_note(DRUMS, D.CrashCymbal2, t2 + 4.0, 0.5, 120)
    builder.add_drum_note(DRUMS, D.BassDrum1, t2 + 4.0, 0.25, 120)


def fill_intro_pickup(builder, start):
    """Drum pickup fill at end of bar 2 going into bar 3."""
    # Hi-hat count-in feel
    builder.add_drum_note(DRUMS, D.ClosedHiHat, start, 0.5, 70)
    builder.add_drum_note(DRUMS, D.ClosedHiHat, start + 0.5, 0.5, 60)
    builder.add_drum_note(DRUMS, D.AcousticSnare, start + 1.0, 0.25, 80)
    builder.add_drum_note(DRUMS, D.AcousticSnare, start + 1.25, 0.25, 85)
    builder.add_drum_note(DRUMS, D.AcousticSnare, start + 1.5, 0.25, 90)
    builder.add_drum_note(DRUMS, D.HighTom, start + 1.75, 0.25, 95)
    builder.add_drum_note(DRUMS, D.BassDrum1, start + 2.0, 0.25, 100)
    builder.add_drum_note(DRUMS, D.CrashCymbal1, start + 2.0, 0.5, 110)


def crash_accent(builder, time):
    """Add a crash cymbal accent on beat 1 of a new section."""
    builder.add_drum_note(DRUMS, D.CrashCymbal1, time, 0.5, 115)


# ═══════════════════════════════════════════════════════════════════════════════
#  SONG ASSEMBLY
# ═══════════════════════════════════════════════════════════════════════════════

def generate_song():
    builder = MidiBuilder(num_tracks=4, tempo=TEMPO)

    # Set up tracks
    builder.add_track(BASS, "Electric Bass", GMInstrument.ElectricBassPick, BASS_CH)
    builder.add_track(JANGLE_GTR, "Jangly Guitar", GMInstrument.ElectricGuitarClean, JANGLE_CH)
    builder.add_track(DRUMS, "Drums")  # drums use channel 9 automatically
    builder.add_track(RHYTHM_GTR, "Rhythm Guitar", GMInstrument.OverdrivenGuitar, RHYTHM_CH)

    t = 0  # current time in beats

    # ── INTRO (4 measures = 16 beats) ──────────────────────────────────────────
    print("  Building Intro...")
    # Guitar alone for first 2 bars
    jangle_intro_arpeggio(builder, t)

    # Light hi-hat count for bars 1-2 (guitar is solo but hi-hat keeps pulse)
    for i in range(8):  # 8 quarter notes across 2 measures
        builder.add_drum_note(DRUMS, D.ClosedHiHat, t + i, 0.25, 50 + i * 3)

    # Drums pickup fill at end of bar 2
    fill_intro_pickup(builder, t + 6)  # last 2 beats of bar 2

    # Drums join bar 3-4 with simple beat
    drums_verse_beat(builder, t + 8, measures=2)

    # Bass enters bar 3
    bass_intro(builder, t)

    t += 16  # 4 bars done

    # ── VERSE 1 (8 measures = 32 beats) ────────────────────────────────────────
    print("  Building Verse 1...")
    crash_accent(builder, t)
    for i, chord in enumerate(VERSE_CHORDS):
        bar_start = t + i * 8
        bass_verse_pattern(builder, bar_start, chord, variation=False)
        jangle_arpeggio(builder, bar_start, chord, measures=2)
        rhythm_sustained(builder, bar_start, chord, measures=2, volume=65)

        if i < 3:
            drums_verse_beat(builder, bar_start, measures=2)
        else:
            # Last 2 bars: normal beat first bar, fill second bar
            drums_verse_beat(builder, bar_start, measures=1)
            fill_syncopated_ghost(builder, bar_start + 4)

    # Transition fill into chorus
    fill_tom_cascade(builder, t + 30)  # last beat of verse

    t += 32  # 8 bars done (total: 48 beats = 12 bars)

    # ── CHORUS 1 (8 measures = 32 beats) ───────────────────────────────────────
    print("  Building Chorus 1...")
    crash_accent(builder, t)
    for i, chord in enumerate(CHORUS_CHORDS):
        bar_start = t + i * 8
        bass_chorus_pattern(builder, bar_start, chord)
        jangle_strum(builder, bar_start, chord, measures=2)
        rhythm_power_chords(builder, bar_start, chord, measures=2, volume=95)

        if i < 3:
            drums_chorus_beat(builder, bar_start, measures=2)
        else:
            drums_chorus_beat(builder, bar_start, measures=2)
            # Big tom fill at end of chorus
            fill_big_transition(builder, bar_start + 6)

    t += 32

    # ── VERSE 2 (8 measures = 32 beats) ────────────────────────────────────────
    print("  Building Verse 2...")
    crash_accent(builder, t)
    for i, chord in enumerate(VERSE_CHORDS):
        bar_start = t + i * 8
        bass_verse_pattern(builder, bar_start, chord, variation=True)  # variation!
        jangle_arpeggio(builder, bar_start, chord, measures=2)
        rhythm_sustained(builder, bar_start, chord, measures=2, volume=70)

        if i == 1:
            # Fill after 2nd chord change
            drums_verse_beat(builder, bar_start, measures=1)
            drums_verse_beat(builder, bar_start + 4, measures=1)
            fill_kick_snare_tom_triplets(builder, bar_start + 6)  # moved to last 2 beats
        elif i == 3:
            drums_verse_beat(builder, bar_start, measures=2)
            fill_snare_roll_crescendo(builder, bar_start + 6, duration=1.5)
        else:
            drums_verse_beat(builder, bar_start, measures=2)

    t += 32

    # ── CHORUS 2 (8 measures = 32 beats) ───────────────────────────────────────
    print("  Building Chorus 2...")
    crash_accent(builder, t)
    for i, chord in enumerate(CHORUS_CHORDS):
        bar_start = t + i * 8
        bass_chorus_pattern(builder, bar_start, chord)
        jangle_strum(builder, bar_start, chord, measures=2)
        rhythm_power_chords(builder, bar_start, chord, measures=2, volume=100)

        if i == 1:
            drums_chorus_beat(builder, bar_start, measures=2)
            fill_tom_cascade(builder, bar_start + 6)
        elif i == 3:
            drums_chorus_beat(builder, bar_start, measures=2)
            fill_big_transition(builder, bar_start + 6)
        else:
            drums_chorus_beat(builder, bar_start, measures=2)

    t += 32

    # ── BRIDGE (8 measures = 32 beats) ─────────────────────────────────────────
    print("  Building Bridge...")
    crash_accent(builder, t)
    for i, chord in enumerate(BRIDGE_CHORDS):
        bar_start = t + i * 8
        bass_bridge_pattern(builder, bar_start, chord)
        jangle_bridge(builder, bar_start, chord)
        rhythm_sustained(builder, bar_start, chord, measures=2, volume=75 + i * 5)

        if i < 3:
            drums_bridge_halftime(builder, bar_start, measures=2, intensity=i * 8)
        else:
            # Big crescendo fill at end of bridge
            fill_bridge_crescendo(builder, bar_start)

    t += 32

    # ── FINAL CHORUS (8 measures = 32 beats) ───────────────────────────────────
    print("  Building Final Chorus...")
    crash_accent(builder, t)
    builder.add_drum_note(DRUMS, D.CrashCymbal2, t, 0.5, 120)  # double crash!

    for i, chord in enumerate(CHORUS_CHORDS):
        bar_start = t + i * 8
        bass_chorus_pattern(builder, bar_start, chord)
        jangle_strum(builder, bar_start, chord, measures=2)
        rhythm_power_chords(builder, bar_start, chord, measures=2, volume=105)

        # Use ride cymbal for energy
        if i == 2:
            drums_chorus_beat(builder, bar_start, measures=2, use_ride=True)
            fill_tom_cascade(builder, bar_start + 6)
        elif i == 3:
            drums_chorus_beat(builder, bar_start, measures=2, use_ride=True)
            fill_big_transition(builder, bar_start + 6)
        else:
            drums_chorus_beat(builder, bar_start, measures=2, use_ride=True)

        # Extra crash accents in final chorus
        if i > 0:
            crash_accent(builder, bar_start)

    t += 32

    # ── OUTRO (4 measures = 16 beats) ──────────────────────────────────────────
    print("  Building Outro...")
    crash_accent(builder, t)

    bass_outro(builder, t)
    jangle_outro(builder, t)
    rhythm_outro(builder, t)

    # Outro drums: winding down but no dead air
    drums_chorus_beat(builder, t, measures=1)
    drums_verse_beat(builder, t + 4, measures=1)
    # Bar 3: half-time wind-down (still has ride bell pulse)
    drums_bridge_halftime(builder, t + 8, measures=1, intensity=0)
    # Bar 4: final measure — simple quarter-note ride into final crash
    for i in range(4):
        builder.add_drum_note(DRUMS, D.RideCymbal1, t + 12 + i, 0.5, 75 - i * 10)
    builder.add_drum_note(DRUMS, D.BassDrum1, t + 12, 0.25, 95)
    builder.add_drum_note(DRUMS, D.AcousticSnare, t + 14, 0.25, 85)
    # Final crash
    builder.add_drum_note(DRUMS, D.BassDrum1, t + 15, 0.25, 100)
    builder.add_drum_note(DRUMS, D.CrashCymbal1, t + 15, 2.0, 110)
    builder.add_drum_note(DRUMS, D.CrashCymbal2, t + 15, 2.0, 105)

    t += 16

    # ── Save ───────────────────────────────────────────────────────────────────
    total_bars = t // 4
    duration_secs = t * 60.0 / TEMPO
    print(f"\n  Total: {total_bars} measures, {t} beats")
    print(f"  Duration: {duration_secs:.0f}s ({duration_secs/60:.1f} min) @ {TEMPO} BPM")

    output_path = os.path.join(os.path.dirname(__file__), "river_rats_theme.mid")
    builder.save(output_path)
    return output_path


if __name__ == "__main__":
    print("🎸 River Rats Theme Song Generator")
    print("=" * 50)
    path = generate_song()
    print(f"\n✅ Done! Open '{path}' in GarageBand to convert to MP3.")
