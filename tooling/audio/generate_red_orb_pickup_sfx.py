"""Generate a distinct red orb pickup sound.

Design:
- Triple punch (three quick attacks)
- Extra shimmer layer vs blue orb pickup sounds

Output:
  Content/Audio/SFX/orb_collect_red_special.wav
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
OUTPUT_NAME = "orb_collect_red_special.wav"


def chirp(start_freq: float, end_freq: float, length_s: float) -> np.ndarray:
    n = int(length_s * SAMPLE_RATE)
    t = np.linspace(0, length_s, n, endpoint=False)
    freq = start_freq * (end_freq / start_freq) ** (t / max(length_s, 1e-6))
    phase = 2 * np.pi * np.cumsum(freq) / SAMPLE_RATE
    return np.sin(phase)


def pulse(length_s: float, start_freq: float, end_freq: float, shimmer_gain: float) -> np.ndarray:
    base = chirp(start_freq, end_freq, length_s)
    shimmer = chirp(start_freq * 2.25, end_freq * 2.75, length_s)

    n = int(length_s * SAMPLE_RATE)
    t = np.linspace(0, length_s, n, endpoint=False)
    env = np.exp(-26.0 * t)

    out = (base + shimmer * shimmer_gain) * env

    # Add a short crunchy edge for impact.
    out += np.sign(base) * 0.12 * env
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

    total_len = 0.23
    n_total = int(total_len * SAMPLE_RATE)
    out = np.zeros(n_total, dtype=np.float32)

    # Three quick punches.
    starts = [0.000, 0.052, 0.104]
    pulse_len = 0.085

    freqs = [
        (880.0, 1320.0),
        (940.0, 1410.0),
        (1000.0, 1500.0),
    ]

    for i, start_s in enumerate(starts):
        start = int(start_s * SAMPLE_RATE)
        seg = pulse(pulse_len, freqs[i][0], freqs[i][1], shimmer_gain=0.55)
        end = min(n_total, start + seg.shape[0])
        out[start:end] += seg[: end - start]

    # Tail fade to silence.
    fade_n = int(0.015 * SAMPLE_RATE)
    if 0 < fade_n < out.shape[0]:
        out[-fade_n:] *= np.linspace(1.0, 0.0, fade_n, dtype=np.float32)

    output_path = os.path.join(OUTPUT_DIR, OUTPUT_NAME)
    write_wav(output_path, out)
    print(f"wrote {OUTPUT_NAME}")


if __name__ == "__main__":
    main()
