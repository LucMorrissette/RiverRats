"""Generate a short quest-complete musical sting.

Design:
- Bright major-key rise that feels like progress, not loot pickup
- Short enough to read as a UI sting instead of background music
- Soft bass reinforcement so the melody still lands on laptop speakers

Output:
  Content/Audio/SFX/quest_complete_jingle.wav
"""

from __future__ import annotations

import math
import os
import wave

import numpy as np

SAMPLE_RATE = 44100
OUTPUT_DIR = os.path.join(
    os.path.dirname(__file__),
    "..", "..", "src", "RiverRats.Game", "Content", "Audio", "SFX",
)
OUTPUT_NAME = "quest_complete_jingle.wav"


def midi_to_freq(note: int) -> float:
    return 440.0 * (2.0 ** ((note - 69) / 12.0))


def adsr_envelope(length_s: float, attack_s: float, decay_s: float, sustain_level: float, release_s: float) -> np.ndarray:
    n = max(1, int(length_s * SAMPLE_RATE))
    t = np.linspace(0.0, length_s, n, endpoint=False)
    env = np.empty(n, dtype=np.float32)

    attack_end = attack_s
    decay_end = attack_s + decay_s
    release_start = max(length_s - release_s, decay_end)

    for i, time_s in enumerate(t):
        if time_s < attack_end and attack_s > 0:
            env[i] = time_s / attack_s
        elif time_s < decay_end and decay_s > 0:
            decay_progress = (time_s - attack_end) / decay_s
            env[i] = 1.0 + ((sustain_level - 1.0) * decay_progress)
        elif time_s < release_start:
            env[i] = sustain_level
        elif release_s > 0:
            release_progress = min(1.0, (time_s - release_start) / release_s)
            env[i] = sustain_level * (1.0 - release_progress)
        else:
            env[i] = 0.0

    return env


def voiced_tone(freq: float, length_s: float, phase_offset: float = 0.0) -> np.ndarray:
    n = max(1, int(length_s * SAMPLE_RATE))
    t = np.linspace(0.0, length_s, n, endpoint=False)

    vibrato = np.sin(2.0 * np.pi * 5.2 * t) * 0.003
    freq_curve = freq * (1.0 + vibrato)
    phase = (2.0 * np.pi * np.cumsum(freq_curve) / SAMPLE_RATE) + phase_offset

    lead = np.sign(np.sin(phase)) * 0.58
    sine = np.sin(phase * 0.5) * 0.28
    sparkle = np.sin(phase * 2.0 + 0.35) * 0.14
    body = lead + sine + sparkle

    env = adsr_envelope(length_s, attack_s=0.006, decay_s=0.08, sustain_level=0.62, release_s=0.09)
    return (body * env).astype(np.float32)


def bass_tone(freq: float, length_s: float) -> np.ndarray:
    n = max(1, int(length_s * SAMPLE_RATE))
    t = np.linspace(0.0, length_s, n, endpoint=False)
    phase = 2.0 * np.pi * np.cumsum(np.full(n, freq)) / SAMPLE_RATE
    tone = (np.sin(phase) * 0.8) + (np.sin(phase * 0.5) * 0.2)
    env = adsr_envelope(length_s, attack_s=0.004, decay_s=0.05, sustain_level=0.45, release_s=0.1)
    return (tone * env).astype(np.float32)


def add_clip(mix: np.ndarray, clip: np.ndarray, start_s: float, gain: float) -> None:
    start = int(start_s * SAMPLE_RATE)
    end = min(mix.shape[0], start + clip.shape[0])
    if end <= start:
        return

    mix[start:end] += clip[: end - start] * gain


def write_wav(path: str, samples: np.ndarray) -> None:
    peak = np.max(np.abs(samples))
    if peak > 0:
        samples = samples * (0.92 / peak)

    pcm = np.clip(samples * 32767, -32768, 32767).astype(np.int16)
    with wave.open(path, "w") as wav_file:
        wav_file.setnchannels(1)
        wav_file.setsampwidth(2)
        wav_file.setframerate(SAMPLE_RATE)
        wav_file.writeframes(pcm.tobytes())


def main() -> None:
    os.makedirs(OUTPUT_DIR, exist_ok=True)

    total_length_s = 1.28
    mix = np.zeros(int(total_length_s * SAMPLE_RATE), dtype=np.float32)

    # C major upward celebration: E5 -> G5 -> C6, with light chord support.
    events = [
        (0.00, 0.18, 76, [64, 71]),
        (0.18, 0.20, 79, [67, 72]),
        (0.38, 0.34, 84, [72, 79]),
        (0.76, 0.34, 88, [76, 84]),
    ]

    bass_events = [
        (0.00, 0.38, 48),
        (0.38, 0.34, 55),
        (0.76, 0.36, 60),
    ]

    for start_s, length_s, note, harmony in events:
        add_clip(mix, voiced_tone(midi_to_freq(note), length_s), start_s, gain=0.62)
        for index, harmony_note in enumerate(harmony):
            add_clip(
                mix,
                voiced_tone(midi_to_freq(harmony_note), length_s * 0.92, phase_offset=index * 0.7),
                start_s + (index * 0.008),
                gain=0.19,
            )

    for start_s, length_s, note in bass_events:
        add_clip(mix, bass_tone(midi_to_freq(note), length_s), start_s, gain=0.24)

    # Tiny sparkle burst on the landing note so the ending reads as completion.
    sparkle_start = int(0.78 * SAMPLE_RATE)
    sparkle_len = int(0.12 * SAMPLE_RATE)
    sparkle_t = np.linspace(0.0, sparkle_len / SAMPLE_RATE, sparkle_len, endpoint=False)
    sparkle = np.sin(2.0 * np.pi * 1760.0 * sparkle_t) * np.exp(-28.0 * sparkle_t)
    mix[sparkle_start:sparkle_start + sparkle_len] += sparkle.astype(np.float32) * 0.08

    # Fade the tail fully to zero to avoid a click.
    fade_len = int(0.08 * SAMPLE_RATE)
    mix[-fade_len:] *= np.linspace(1.0, 0.0, fade_len, dtype=np.float32)

    output_path = os.path.join(OUTPUT_DIR, OUTPUT_NAME)
    write_wav(output_path, mix)
    print(f"wrote {OUTPUT_NAME} ({total_length_s:.2f}s)")


if __name__ == "__main__":
    main()