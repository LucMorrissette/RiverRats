"""Generate typewriter letter-tick sound effects for the dialog system.

Produces 4 short mono 16-bit WAV files:
    dialog_letter_tick_00.wav  ..  dialog_letter_tick_03.wav

Each tick is a brief, soft click/tck sound with slight pitch variation to avoid
monotony.  The synthesis mimics a key-press on a light mechanical typewriter.
"""

from __future__ import annotations

import os
import wave

import numpy as np

SAMPLE_RATE = 44100
OUTPUT_DIR = os.path.join(
    os.path.dirname(__file__),
    "..", "..", "src", "RiverRats.Game", "Content", "Audio", "SFX",
)
RNG_SEED = 31415

rng = np.random.default_rng(RNG_SEED)


def write_wav(filepath: str, samples: np.ndarray) -> None:
    """Write mono 16-bit PCM peak-normalised WAV."""
    peak = np.max(np.abs(samples)) or 1.0
    samples = samples * (0.75 / peak)
    pcm = np.clip(samples * 32767, -32768, 32767).astype(np.int16)
    with wave.open(filepath, "w") as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(SAMPLE_RATE)
        f.writeframes(pcm.tobytes())
    print(f"  wrote {filepath}")


def make_tick(duration_s: float, freq_hz: float, noise_amp: float,
              tone_amp: float, decay: float) -> np.ndarray:
    """Single tick: brief tone burst + filtered noise transient."""
    n = int(duration_s * SAMPLE_RATE)
    t = np.linspace(0, duration_s, n, endpoint=False)

    # Very fast exponential decay envelope
    env = np.exp(-decay * t)

    # Short tone burst (typewriter key resonance)
    tone = tone_amp * np.sin(2 * np.pi * freq_hz * t) * env

    # High-pass flavoured noise transient (the mechanical "click" body)
    noise_raw = rng.uniform(-1.0, 1.0, n)
    # Simple 1-pole high-pass: y[n] = x[n] - x[n-1]
    noise_hp = np.diff(noise_raw, prepend=noise_raw[0])
    click = noise_amp * noise_hp * env

    return tone + click


# Four variations with slight pitch/decay offsets
variations = [
    # (duration, base_freq, noise_amp, tone_amp, decay)
    (0.06, 1800, 0.60, 0.25, 180),
    (0.06, 2000, 0.55, 0.28, 200),
    (0.06, 1650, 0.65, 0.22, 160),
    (0.06, 2200, 0.50, 0.30, 220),
]

os.makedirs(OUTPUT_DIR, exist_ok=True)
for i, (dur, freq, na, ta, dec) in enumerate(variations):
    samples = make_tick(dur, freq, na, ta, dec)
    path = os.path.join(OUTPUT_DIR, f"dialog_letter_tick_{i:02d}.wav")
    write_wav(path, samples)

print("Done – 4 typewriter tick files written.")
