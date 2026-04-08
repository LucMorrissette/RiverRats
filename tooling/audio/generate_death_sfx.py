"""Generate 12 slightly different enemy death/pop/explode sound effects.

Each variant is a short (80-150ms) layered burst:
  - A sine chirp that sweeps downward (gives the "pop" pitch)
  - A noise burst with an exponential decay (gives the "explode" crunch)
  - Slight random variation in pitch, sweep speed, noise mix, and duration

Output: 12 mono 16-bit 44100 Hz WAV files in Content/Audio/SFX/
"""

import os
import wave
import struct
import numpy as np

SAMPLE_RATE = 44100
OUTPUT_DIR = os.path.join(
    os.path.dirname(__file__),
    "..", "..", "src", "DogDays.Game", "Content", "Audio", "SFX",
)
NUM_VARIANTS = 12

# Seed for reproducibility — change to get a different family of sounds.
RNG_SEED = 77


def generate_death_pop(
    duration_s: float,
    start_freq: float,
    end_freq: float,
    noise_mix: float,
    decay_rate: float,
    rng: np.random.Generator,
) -> np.ndarray:
    """Synthesise one death-pop sound and return float samples in [-1, 1]."""
    n_samples = int(SAMPLE_RATE * duration_s)
    t = np.linspace(0, duration_s, n_samples, endpoint=False)

    # --- Downward sine chirp ---
    # Exponential frequency sweep from start_freq → end_freq.
    freq = start_freq * (end_freq / start_freq) ** (t / duration_s)
    phase = 2 * np.pi * np.cumsum(freq) / SAMPLE_RATE
    chirp = np.sin(phase)

    # --- Noise burst ---
    noise = rng.uniform(-1.0, 1.0, n_samples)

    # --- Mix ---
    mixed = (1.0 - noise_mix) * chirp + noise_mix * noise

    # --- Amplitude envelope: instant attack, exponential decay ---
    envelope = np.exp(-decay_rate * t)
    # Gentle fade-out over last 5 ms to avoid click.
    fade_samples = int(0.005 * SAMPLE_RATE)
    if fade_samples > 0 and fade_samples < n_samples:
        envelope[-fade_samples:] *= np.linspace(1.0, 0.0, fade_samples)

    return mixed * envelope


def write_wav(filepath: str, samples: np.ndarray) -> None:
    """Write mono 16-bit PCM WAV."""
    # Normalise peak to 0.95 to avoid clipping.
    peak = np.max(np.abs(samples))
    if peak > 0:
        samples = samples * (0.95 / peak)
    int_samples = np.clip(samples * 32767, -32768, 32767).astype(np.int16)

    with wave.open(filepath, "w") as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(SAMPLE_RATE)
        wf.writeframes(int_samples.tobytes())


def main() -> None:
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    rng = np.random.default_rng(RNG_SEED)

    for i in range(NUM_VARIANTS):
        # Randomise parameters within a tight family range.
        duration = rng.uniform(0.08, 0.15)
        start_freq = rng.uniform(600, 1000)
        end_freq = rng.uniform(80, 200)
        noise_mix = rng.uniform(0.35, 0.6)
        decay_rate = rng.uniform(18, 35)

        samples = generate_death_pop(
            duration_s=duration,
            start_freq=start_freq,
            end_freq=end_freq,
            noise_mix=noise_mix,
            decay_rate=decay_rate,
            rng=rng,
        )

        filename = f"gnome_death_{i:02d}.wav"
        filepath = os.path.join(OUTPUT_DIR, filename)
        write_wav(filepath, samples)
        print(f"  wrote {filename}  ({duration*1000:.0f} ms, {start_freq:.0f}→{end_freq:.0f} Hz, noise {noise_mix:.0%})")

    print(f"\nGenerated {NUM_VARIANTS} variants in {os.path.abspath(OUTPUT_DIR)}")


if __name__ == "__main__":
    main()
