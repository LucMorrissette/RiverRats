"""Generate a bright quest-discovery UI sting.

Design:
- Airy upward motion that reads as "new objective" rather than completion
- Short bell-like timbre with soft body so it cuts through the mix cleanly
- Brief sparkle bursts at note onsets for extra UI juice

Output:
  Content/Audio/SFX/quest_discovery_sting.wav
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
OUTPUT_NAME = "quest_discovery_sting.wav"


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


def bell_tone(freq: float, length_s: float, phase_offset: float = 0.0) -> np.ndarray:
    n = max(1, int(length_s * SAMPLE_RATE))
    t = np.linspace(0.0, length_s, n, endpoint=False)
    phase = (2.0 * np.pi * freq * t) + phase_offset

    body = (
        np.sin(phase) * 0.60
        + np.sin(phase * 2.01 + 0.35) * 0.24
        + np.sin(phase * 3.02 + 1.12) * 0.16
        + np.sin(phase * 4.75 + 0.54) * 0.08
    )
    shimmer = np.sin(phase * 7.1 + 0.2) * np.exp(-8.0 * t) * 0.10
    env = adsr_envelope(length_s, attack_s=0.003, decay_s=0.12, sustain_level=0.34, release_s=0.14)
    return ((body + shimmer) * env).astype(np.float32)


def pad_tone(freq: float, length_s: float) -> np.ndarray:
    n = max(1, int(length_s * SAMPLE_RATE))
    t = np.linspace(0.0, length_s, n, endpoint=False)
    phase = 2.0 * np.pi * freq * t
    tone = np.sin(phase) * 0.72 + np.sin(phase * 0.5 + 0.4) * 0.18
    env = adsr_envelope(length_s, attack_s=0.01, decay_s=0.10, sustain_level=0.44, release_s=0.18)
    return (tone * env).astype(np.float32)


def add_clip(mix: np.ndarray, clip: np.ndarray, start_s: float, gain: float) -> None:
    start = int(start_s * SAMPLE_RATE)
    end = min(mix.shape[0], start + clip.shape[0])
    if end <= start:
        return

    mix[start:end] += clip[: end - start] * gain


def add_sparkle(mix: np.ndarray, start_s: float, freq: float, gain: float) -> None:
    start = int(start_s * SAMPLE_RATE)
    length = int(0.08 * SAMPLE_RATE)
    end = min(mix.shape[0], start + length)
    if end <= start:
        return

    t = np.linspace(0.0, (end - start) / SAMPLE_RATE, end - start, endpoint=False)
    sparkle = (
        np.sin(2.0 * np.pi * freq * t)
        + np.sin(2.0 * np.pi * freq * 1.5 * t + 0.3) * 0.4
    ) * np.exp(-34.0 * t)
    mix[start:end] += sparkle.astype(np.float32) * gain


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

    total_length_s = 1.02
    mix = np.zeros(int(total_length_s * SAMPLE_RATE), dtype=np.float32)

    lead_events = [
        (0.00, 0.14, 74, [79]),
        (0.10, 0.16, 79, [83]),
        (0.24, 0.22, 83, [86]),
        (0.44, 0.34, 86, [90]),
    ]
    pad_events = [
        (0.00, 0.30, 55),
        (0.24, 0.44, 62),
    ]

    for start_s, length_s, note, harmony in lead_events:
        add_clip(mix, bell_tone(midi_to_freq(note), length_s), start_s, gain=0.58)
        for index, harmony_note in enumerate(harmony):
            add_clip(
                mix,
                bell_tone(midi_to_freq(harmony_note), length_s * 0.92, phase_offset=index * 0.6),
                start_s + (index * 0.012),
                gain=0.22,
            )
        add_sparkle(mix, start_s, midi_to_freq(note) * 4.0, gain=0.045)

    for start_s, length_s, note in pad_events:
        add_clip(mix, pad_tone(midi_to_freq(note), length_s), start_s, gain=0.20)

    fade_len = int(0.10 * SAMPLE_RATE)
    mix[-fade_len:] *= np.linspace(1.0, 0.0, fade_len, dtype=np.float32)

    output_path = os.path.join(OUTPUT_DIR, OUTPUT_NAME)
    write_wav(output_path, mix)
    print(f"wrote {OUTPUT_NAME} ({total_length_s:.2f}s)")


if __name__ == "__main__":
    main()