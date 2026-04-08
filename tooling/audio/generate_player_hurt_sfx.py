"""Generate 4 slightly different player-hurt sound effects.

Design goal: Stand out from the enemy death pops (short downward chirps).
These use a heavy sub-bass THUD + dark mid-range GROWL layered together,
slightly longer duration, with a menacing "damage taken" character.

Each variant randomises the thud pitch, growl sweep, and distortion slightly
so the sound doesn't feel robotic on repeat hits.

Output: 4 mono 16-bit 44100 Hz WAV files in Content/Audio/SFX/
"""

import os
import wave
import numpy as np

SAMPLE_RATE = 44100
OUTPUT_DIR = os.path.join(
    os.path.dirname(__file__),
    "..", "..", "src", "DogDays.Game", "Content", "Audio", "SFX",
)
NUM_VARIANTS = 4
RNG_SEED = 42


def generate_player_hurt(
    duration_s: float,
    thud_freq: float,
    growl_start_freq: float,
    growl_end_freq: float,
    growl_mix: float,
    thud_decay: float,
    growl_decay: float,
    distortion: float,
    rng: np.random.Generator,
) -> np.ndarray:
    """Synthesise one player-hurt sound and return float samples in [-1, 1].

    Layers:
      1. Sub-bass thud  — low sine with slow decay (heavy impact)
      2. Dark growl      — mid-range downward sweep, hard-clipped for menace
      3. Noise rumble    — filtered noise on the attack for grit
    """
    n_samples = int(SAMPLE_RATE * duration_s)
    t = np.linspace(0, duration_s, n_samples, endpoint=False)

    # --- Layer 1: Sub-bass thud (sine, very low) ---
    thud = np.sin(2 * np.pi * thud_freq * t) * np.exp(-thud_decay * t)

    # --- Layer 2: Dark growl (downward sweep, hard-clipped for distortion) ---
    freq = growl_start_freq * (growl_end_freq / growl_start_freq) ** (t / duration_s)
    phase = 2 * np.pi * np.cumsum(freq) / SAMPLE_RATE
    # Raw sine sweep.
    growl = np.sin(phase)
    # Hard clip to create harsh harmonics (distortion amount controls clipping threshold).
    growl = np.clip(growl * (1.0 + distortion * 3), -distortion, distortion) / max(distortion, 0.01)
    growl *= np.exp(-growl_decay * t)

    # --- Mix ---
    mixed = (1.0 - growl_mix) * thud + growl_mix * growl

    # --- Layer 3: Noise rumble on attack (first 30 ms, heavier than before) ---
    grit_samples = int(0.03 * SAMPLE_RATE)
    if grit_samples < n_samples:
        noise = rng.uniform(-0.35, 0.35, grit_samples)
        # Shape noise with quick decay.
        noise_env = np.exp(-40 * np.linspace(0, 0.03, grit_samples))
        mixed[:grit_samples] += noise * noise_env

    # --- Envelope: fade out last 15 ms to avoid click ---
    fade_samples = int(0.015 * SAMPLE_RATE)
    if 0 < fade_samples < n_samples:
        mixed[-fade_samples:] *= np.linspace(1.0, 0.0, fade_samples)

    return mixed


def write_wav(filepath: str, samples: np.ndarray) -> None:
    """Write mono 16-bit PCM WAV."""
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
        duration = rng.uniform(0.18, 0.28)
        thud_freq = rng.uniform(30, 50)
        growl_start = rng.uniform(180, 300)
        growl_end = rng.uniform(40, 80)
        growl_mix = rng.uniform(0.45, 0.6)
        thud_decay = rng.uniform(6, 12)
        growl_decay = rng.uniform(8, 14)
        distortion = rng.uniform(0.2, 0.35)

        samples = generate_player_hurt(
            duration_s=duration,
            thud_freq=thud_freq,
            growl_start_freq=growl_start,
            growl_end_freq=growl_end,
            growl_mix=growl_mix,
            thud_decay=thud_decay,
            growl_decay=growl_decay,
            distortion=distortion,
            rng=rng,
        )

        filename = f"player_hurt_{i:02d}.wav"
        filepath = os.path.join(OUTPUT_DIR, filename)
        write_wav(filepath, samples)
        print(
            f"  wrote {filename}  ({duration*1000:.0f} ms, "
            f"thud {thud_freq:.0f} Hz, growl {growl_start:.0f}→{growl_end:.0f} Hz, "
            f"distortion {distortion:.0%})"
        )

    print(f"\nGenerated {NUM_VARIANTS} variants in {os.path.abspath(OUTPUT_DIR)}")


if __name__ == "__main__":
    main()
