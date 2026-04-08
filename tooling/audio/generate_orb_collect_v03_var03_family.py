"""Generate 12 close variations around orb collect v03 variation 03.

Target reference: v03_var_03 (pitch x1.15 from base arcade blip rise).
This script creates a tight family with subtle differences in:
- pitch (small +/- offsets around 1.15x)
- decay
- grit amount
- length

Output: Content/Audio/SFX/orb_collect_v03p03_family_00..11.wav
"""

from __future__ import annotations

import os
import wave
import numpy as np

SAMPLE_RATE = 44100
OUTPUT_DIR = os.path.join(
    os.path.dirname(__file__),
    "..", "..", "src", "DogDays.Game", "Content", "Audio", "SFX",
)

BASE_START_FREQ = 520.0
BASE_END_FREQ = 980.0
BASE_MULTIPLIER = 1.15
NUM_VARIANTS = 12
RNG_SEED = 403


def exp_env(length_s: float, decay: float) -> np.ndarray:
    n = int(length_s * SAMPLE_RATE)
    t = np.linspace(0, length_s, n, endpoint=False)
    return np.exp(-decay * t)


def chirp(start_freq: float, end_freq: float, length_s: float) -> np.ndarray:
    n = int(length_s * SAMPLE_RATE)
    t = np.linspace(0, length_s, n, endpoint=False)
    freq = start_freq * (end_freq / start_freq) ** (t / max(length_s, 1e-6))
    phase = 2 * np.pi * np.cumsum(freq) / SAMPLE_RATE
    return np.sin(phase)


def synth_variant(
    pitch_multiplier: float,
    length_s: float,
    decay: float,
    grit_amount: float,
    wobble_amount: float,
) -> np.ndarray:
    start_freq = BASE_START_FREQ * pitch_multiplier
    end_freq = BASE_END_FREQ * pitch_multiplier

    base = chirp(start_freq, end_freq, length_s)
    grit = np.sign(base) * grit_amount

    # Tiny amplitude wobble to keep repeated pickups from feeling static.
    n = int(length_s * SAMPLE_RATE)
    t = np.linspace(0, length_s, n, endpoint=False)
    wobble = 1.0 + np.sin(2 * np.pi * 9.0 * t) * wobble_amount

    out = (base + grit) * wobble
    out *= exp_env(length_s, decay=decay)

    # Fade final 8 ms to prevent clicks.
    fade_n = int(0.008 * SAMPLE_RATE)
    if 0 < fade_n < out.shape[0]:
        out[-fade_n:] *= np.linspace(1.0, 0.0, fade_n, dtype=np.float32)

    return out.astype(np.float32)


def write_wav(path: str, data: np.ndarray) -> None:
    peak = np.max(np.abs(data))
    if peak > 0:
        data = data * (0.95 / peak)
    pcm = np.clip(data * 32767, -32768, 32767).astype(np.int16)

    with wave.open(path, "w") as wav_file:
        wav_file.setnchannels(1)
        wav_file.setsampwidth(2)
        wav_file.setframerate(SAMPLE_RATE)
        wav_file.writeframes(pcm.tobytes())


def main() -> None:
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    rng = np.random.default_rng(RNG_SEED)

    for i in range(NUM_VARIANTS):
        pitch_offset = rng.uniform(-0.045, 0.045)
        pitch_multiplier = BASE_MULTIPLIER + pitch_offset
        length_s = rng.uniform(0.145, 0.18)
        decay = rng.uniform(20.0, 26.0)
        grit_amount = rng.uniform(0.18, 0.30)
        wobble_amount = rng.uniform(0.02, 0.06)

        samples = synth_variant(
            pitch_multiplier=pitch_multiplier,
            length_s=length_s,
            decay=decay,
            grit_amount=grit_amount,
            wobble_amount=wobble_amount,
        )

        name = f"orb_collect_v03p03_family_{i:02d}.wav"
        path = os.path.join(OUTPUT_DIR, name)
        write_wav(path, samples)
        print(
            f"wrote {name} "
            f"(pitch x{pitch_multiplier:.3f}, len {length_s*1000:.0f} ms, grit {grit_amount:.2f})"
        )

    print(f"Generated {NUM_VARIANTS} family variants in {os.path.abspath(OUTPUT_DIR)}")


if __name__ == "__main__":
    main()
