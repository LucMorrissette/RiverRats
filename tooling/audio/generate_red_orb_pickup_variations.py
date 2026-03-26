"""Generate 3 close variations of the red orb pickup sound.

Design anchor:
- Triple quick punches
- Bright shimmer layer

Output:
  Content/Audio/SFX/orb_collect_red_var_00..02.wav
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
RNG_SEED = 19


def chirp(start_freq: float, end_freq: float, length_s: float) -> np.ndarray:
    n = int(length_s * SAMPLE_RATE)
    t = np.linspace(0, length_s, n, endpoint=False)
    freq = start_freq * (end_freq / start_freq) ** (t / max(length_s, 1e-6))
    phase = 2 * np.pi * np.cumsum(freq) / SAMPLE_RATE
    return np.sin(phase)


def pulse(length_s: float, start_freq: float, end_freq: float, shimmer_gain: float, grit_gain: float) -> np.ndarray:
    base = chirp(start_freq, end_freq, length_s)
    shimmer = chirp(start_freq * 2.25, end_freq * 2.75, length_s)

    n = int(length_s * SAMPLE_RATE)
    t = np.linspace(0, length_s, n, endpoint=False)
    env = np.exp(-26.0 * t)

    out = (base + shimmer * shimmer_gain) * env
    out += np.sign(base) * grit_gain * env
    return out


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

    for i in range(3):
        total_len = float(rng.uniform(0.215, 0.245))
        n_total = int(total_len * SAMPLE_RATE)
        out = np.zeros(n_total, dtype=np.float32)

        starts = [0.000, 0.051 + float(rng.uniform(-0.004, 0.004)), 0.103 + float(rng.uniform(-0.005, 0.005))]
        pulse_len = float(rng.uniform(0.078, 0.092))
        shimmer_gain = float(rng.uniform(0.50, 0.62))
        grit_gain = float(rng.uniform(0.10, 0.16))
        pitch_mul = float(rng.uniform(0.96, 1.06))

        freqs = [
            (880.0 * pitch_mul, 1320.0 * pitch_mul),
            (940.0 * pitch_mul, 1410.0 * pitch_mul),
            (1000.0 * pitch_mul, 1500.0 * pitch_mul),
        ]

        for p, start_s in enumerate(starts):
            start = int(start_s * SAMPLE_RATE)
            seg = pulse(pulse_len, freqs[p][0], freqs[p][1], shimmer_gain, grit_gain)
            end = min(n_total, start + seg.shape[0])
            out[start:end] += seg[: end - start]

        fade_n = int(0.015 * SAMPLE_RATE)
        if 0 < fade_n < out.shape[0]:
            out[-fade_n:] *= np.linspace(1.0, 0.0, fade_n, dtype=np.float32)

        name = f"orb_collect_red_var_{i:02d}.wav"
        write_wav(os.path.join(OUTPUT_DIR, name), out)
        print(f"wrote {name}")


if __name__ == "__main__":
    main()
