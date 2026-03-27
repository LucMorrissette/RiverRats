"""
Death Jingle Generator - Sombre Chiptune Dirge
===============================================
A very slow, sombre ~10-second death dirge in D minor.
Sparse, heavy, mournful. Low register, long held notes,
plenty of silence between phrases for gravitas.

Output: tooling/music/death_jingle.mid
"""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from midi_utils import MidiBuilder, GMInstrument

# ── Config ─────────────────────────────────────────────────────────────────────
TEMPO = 50  # Very slow - grave
OUTPUT_FILE = os.path.join(os.path.dirname(__file__), "death_jingle.mid")

MELODY_TRACK = 0
BASS_TRACK = 1
MELODY_CH = 0
BASS_CH = 1

# ── MIDI pitches ───────────────────────────────────────────────────────────────
D2  = 38
A2  = 45
D3  = 50
F3  = 53
A3  = 57
D4  = 62
Cs4 = 61
Bb3 = 58
G3  = 55
E3  = 52

REST = None

# ── Melody (square wave) ──────────────────────────────────────────────────────
# Sparse, mournful. Each note lingers. Wide intervals evoke emptiness.
# (pitch, duration_in_beats, velocity)

MELODY = [
    (D3,   2.0,   80),   # low D, soft, hollow
    (REST, 0.5,    0),

    (F3,   1.5,   75),   # minor third - sorrowful
    (REST, 0.5,    0),

    (E3,   1.5,   70),   # half-step down - sinking feeling
    (REST, 0.5,    0),

    (D3,   3.0,   90),   # return to D, long fade - finality
    (REST, 0.5,    0),
]

# ── Bass (synth bass) ─────────────────────────────────────────────────────────
# Just two deep tones. Weight and emptiness.

BASS = [
    (D2,   3.0,   90),   # deep drone
    (REST, 1.0,    0),

    (A2,   2.5,   80),   # dominant - unresolved tension
    (REST, 0.5,    0),

    (D2,   3.5,  100),   # final low toll
    (REST, 0.5,    0),
]


def build_death_jingle():
    builder = MidiBuilder(num_tracks=2, tempo=TEMPO)

    builder.add_track(
        MELODY_TRACK,
        "Dirge Lead",
        instrument=GMInstrument.Lead1Square,
        channel=MELODY_CH,
    )
    builder.add_track(
        BASS_TRACK,
        "Dirge Bass",
        instrument=GMInstrument.SynthBass1,
        channel=BASS_CH,
    )

    # Write melody
    beat = 0.0
    for pitch, duration, velocity in MELODY:
        if pitch is not REST:
            builder.add_note(MELODY_TRACK, MELODY_CH, pitch, beat, duration * 0.95, velocity)
        beat += duration

    # Write bass
    beat = 0.0
    for pitch, duration, velocity in BASS:
        if pitch is not REST:
            builder.add_note(BASS_TRACK, BASS_CH, pitch, beat, duration * 0.95, velocity)
        beat += duration

    builder.save(OUTPUT_FILE)

    melody_beats = sum(d for _, d, _ in MELODY)
    bass_beats = sum(d for _, d, _ in BASS)
    total_beats = max(melody_beats, bass_beats)
    total_seconds = total_beats * 60.0 / TEMPO
    print(f"Duration: {total_beats:.1f} beats = {total_seconds:.1f}s @ {TEMPO} BPM")


if __name__ == "__main__":
    build_death_jingle()
