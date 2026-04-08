"""Generate all fishing mini-game sound effects.

Six SFX families, each with multiple randomised variants:

  1. fishing_cast   (4) — Whooshing rod sweep with singing line overtone.
  2. fishing_plop   (4) — Soft lure-hitting-water plop with bubbly tail.
  3. fishing_twitch (3) — Quick snap/pop when twitching the lure.
  4. fishing_reel   (3) — Short metallic reel click burst.
  5. fishing_strike (4) — Dramatic splash + tension zing (fish-on!).
  6. fishing_catch  (3) — Celebratory ascending chime (success!).

Output: 21 mono 16-bit 44100 Hz WAV files in Content/Audio/SFX/
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
RNG_SEED = 314


# ---------------------------------------------------------------------------
#  Utility helpers
# ---------------------------------------------------------------------------

def write_wav(filepath: str, samples: np.ndarray) -> None:
    """Write mono 16-bit PCM WAV, peak-normalised to 0.95."""
    peak = np.max(np.abs(samples))
    if peak > 0:
        samples = samples * (0.95 / peak)
    pcm = np.clip(samples * 32767, -32768, 32767).astype(np.int16)
    with wave.open(filepath, "w") as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(SAMPLE_RATE)
        wf.writeframes(pcm.tobytes())


def fade_out(signal: np.ndarray, fade_ms: float = 8.0) -> np.ndarray:
    """Apply a short linear fade-out to prevent end-of-sample clicks."""
    n = int(fade_ms * 0.001 * SAMPLE_RATE)
    if 0 < n < len(signal):
        signal[-n:] *= np.linspace(1.0, 0.0, n)
    return signal


def bandpass_simple(
    signal: np.ndarray,
    center_freq: float,
    bandwidth: float,
) -> np.ndarray:
    """Crude resonant bandpass via frequency-domain windowing.

    Good enough for SFX — no scipy dependency.
    """
    n = len(signal)
    spectrum = np.fft.rfft(signal)
    freqs = np.fft.rfftfreq(n, d=1.0 / SAMPLE_RATE)
    # Gaussian window centered on center_freq
    window = np.exp(-0.5 * ((freqs - center_freq) / (bandwidth / 2)) ** 2)
    return np.fft.irfft(spectrum * window, n=n)


def exp_env(length_s: float, decay: float) -> np.ndarray:
    """Exponential decay envelope."""
    n = int(length_s * SAMPLE_RATE)
    t = np.linspace(0, length_s, n, endpoint=False)
    return np.exp(-decay * t)


def sine(freq: float, length_s: float, phase: float = 0.0) -> np.ndarray:
    """Pure sine tone."""
    n = int(length_s * SAMPLE_RATE)
    t = np.linspace(0, length_s, n, endpoint=False)
    return np.sin(2 * np.pi * freq * t + phase)


def chirp(
    start_freq: float,
    end_freq: float,
    length_s: float,
) -> np.ndarray:
    """Exponential frequency sweep."""
    n = int(length_s * SAMPLE_RATE)
    t = np.linspace(0, length_s, n, endpoint=False)
    ratio = end_freq / max(start_freq, 1e-6)
    freq = start_freq * ratio ** (t / max(length_s, 1e-6))
    phase = 2 * np.pi * np.cumsum(freq) / SAMPLE_RATE
    return np.sin(phase)


def noise(length_s: float, rng: np.random.Generator) -> np.ndarray:
    """White noise in [-1, 1]."""
    return rng.uniform(-1.0, 1.0, int(length_s * SAMPLE_RATE))


# ---------------------------------------------------------------------------
#  1. Cast Whoosh  —  rod whip through air + singing line overtone
# ---------------------------------------------------------------------------

def generate_cast_whoosh(
    duration_s: float,
    sweep_start: float,
    sweep_end: float,
    line_freq: float,
    line_mix: float,
    decay: float,
    rng: np.random.Generator,
) -> np.ndarray:
    """Swooshing rod-cast sound.

    Layers:
      - Bandpass-swept noise (wind/whoosh)
      - Thin high sine chirp (singing fishing line through rod guides)
      - Quick attack, smooth exponential decay
    """
    n = int(SAMPLE_RATE * duration_s)
    t = np.linspace(0, duration_s, n, endpoint=False)

    # Layer 1: Whoosh — noise swept through a moving bandpass
    raw_noise = rng.uniform(-1.0, 1.0, n)
    # Sweep center frequency over time for the whoosh character
    center = sweep_start + (sweep_end - sweep_start) * (t / duration_s)
    # Process in chunks for the sweep effect
    chunk_size = max(n // 16, 256)
    whoosh = np.zeros(n)
    for c in range(0, n, chunk_size):
        end_idx = min(c + chunk_size, n)
        seg_len = end_idx - c
        mid_freq = center[(c + end_idx) // 2]
        seg = raw_noise[c:end_idx]
        # FFT bandpass on this chunk
        spec = np.fft.rfft(seg)
        freqs = np.fft.rfftfreq(seg_len, d=1.0 / SAMPLE_RATE)
        bw = mid_freq * 0.8  # bandwidth proportional to center
        window = np.exp(-0.5 * ((freqs - mid_freq) / max(bw / 2, 1)) ** 2)
        whoosh[c:end_idx] = np.fft.irfft(spec * window, n=seg_len)

    # Layer 2: Singing line — thin high chirp
    line_chirp = chirp(line_freq * 0.8, line_freq * 1.3, duration_s) * 0.3
    line_env = np.exp(-8 * t)  # fades faster than the whoosh

    # Attack envelope — quick ramp up over first 10ms
    attack_n = int(0.01 * SAMPLE_RATE)
    attack_env = np.ones(n)
    if attack_n > 0 and attack_n < n:
        attack_env[:attack_n] = np.linspace(0, 1, attack_n)

    # Overall decay
    env = np.exp(-decay * t) * attack_env

    mixed = ((1.0 - line_mix) * whoosh + line_mix * line_chirp * line_env) * env
    return fade_out(mixed, 5.0)


# ---------------------------------------------------------------------------
#  2. Water Plop  —  soft lure splashing into water
# ---------------------------------------------------------------------------

def generate_water_plop(
    duration_s: float,
    impact_freq: float,
    bubble_freq: float,
    bubble_rate: float,
    noise_mix: float,
    decay: float,
    rng: np.random.Generator,
) -> np.ndarray:
    """Soft plop of a small lure hitting the water surface.

    Layers:
      - Low-frequency impact thud (sine pop)
      - Mid-frequency filtered noise burst (splash character)
      - Bubbly tail — amplitude-modulated filtered noise (bubbles rising)
    """
    n = int(SAMPLE_RATE * duration_s)
    t = np.linspace(0, duration_s, n, endpoint=False)

    # Layer 1: Impact thud — low sine with very fast decay
    thud = np.sin(2 * np.pi * impact_freq * t) * np.exp(-45 * t)

    # Layer 2: Splash noise — band-limited noise burst
    raw = rng.uniform(-1.0, 1.0, n)
    splash = bandpass_simple(raw, center_freq=600, bandwidth=500)
    splash_env = np.exp(-25 * t)  # very quick fade
    splash *= splash_env

    # Layer 3: Bubble tail — lower-freq noise with AM (amplitude modulation)
    bubble_noise = rng.uniform(-1.0, 1.0, n)
    bubble_filtered = bandpass_simple(bubble_noise, center_freq=bubble_freq, bandwidth=300)
    # AM ring at bubble_rate Hz creates the "bloop bloop" character
    am = 0.5 + 0.5 * np.sin(2 * np.pi * bubble_rate * t)
    bubble_env = np.exp(-decay * t)
    # Delay bubbles slightly — they start after the initial impact
    delay_n = int(0.025 * SAMPLE_RATE)
    bubble_mask = np.zeros(n)
    if delay_n < n:
        ramp_len = min(n - delay_n, int(0.02 * SAMPLE_RATE))
        bubble_mask[delay_n:delay_n + ramp_len] = np.linspace(0, 1, ramp_len)
        ramp_end = delay_n + ramp_len
        if ramp_end < n:
            bubble_mask[ramp_end:] = 1.0
    bubbles = bubble_filtered * am * bubble_env * bubble_mask * 0.6

    mixed = thud * 1.2 + splash * noise_mix + bubbles
    return fade_out(mixed, 8.0)


# ---------------------------------------------------------------------------
#  3. Lure Twitch / Pop  —  quick snap-and-water-disturbance
# ---------------------------------------------------------------------------

def generate_twitch_pop(
    duration_s: float,
    click_freq: float,
    decay: float,
    rng: np.random.Generator,
) -> np.ndarray:
    """Very short snap/pop when twitching the lure.

    Layers:
      - Sharp click (high-freq sine burst, <5ms)
      - Tiny water puff (filtered noise with fast decay)
    """
    n = int(SAMPLE_RATE * duration_s)
    t = np.linspace(0, duration_s, n, endpoint=False)

    # Layer 1: Click — very short high sine burst
    click_dur = 0.004  # 4ms
    click_n = min(int(click_dur * SAMPLE_RATE), n)
    click = np.zeros(n)
    click_t = np.linspace(0, click_dur, click_n, endpoint=False)
    click[:click_n] = np.sin(2 * np.pi * click_freq * click_t) * np.exp(-300 * click_t)

    # Layer 2: Water puff — band-limited noise
    puff_noise = rng.uniform(-1.0, 1.0, n)
    puff = bandpass_simple(puff_noise, center_freq=800, bandwidth=600)
    puff_env = np.exp(-decay * t)
    # Slight delay so the puff follows the click
    delay = int(0.003 * SAMPLE_RATE)
    puff_shifted = np.zeros(n)
    if delay < n:
        puff_shifted[delay:] = puff[:n - delay]
    else:
        puff_shifted = puff

    mixed = click * 1.5 + puff_shifted * puff_env * 0.7
    return fade_out(mixed, 5.0)


# ---------------------------------------------------------------------------
#  4. Reel Click  —  metallic ticking of the reel mechanism
# ---------------------------------------------------------------------------

def generate_reel_click(
    duration_s: float,
    click_freq: float,
    body_freq: float,
    decay: float,
    rng: np.random.Generator,
) -> np.ndarray:
    """Single metallic reel click — played repeatedly while reeling.

    Layers:
      - Sharp metallic tick (high sine burst with harmonics)
      - Quiet body resonance (lower sine decay for mechanical feel)
    """
    n = int(SAMPLE_RATE * duration_s)
    t = np.linspace(0, duration_s, n, endpoint=False)

    # Layer 1: Metallic tick — fundamental + 2nd harmonic
    tick = (
        np.sin(2 * np.pi * click_freq * t)
        + 0.4 * np.sin(2 * np.pi * click_freq * 2.3 * t)
        + 0.2 * np.sin(2 * np.pi * click_freq * 3.7 * t)
    )
    tick_env = np.exp(-decay * t)

    # Percussive attack — boost first 2ms
    attack_n = int(0.002 * SAMPLE_RATE)
    attack = np.ones(n)
    if attack_n > 0 and attack_n < n:
        attack[:attack_n] = np.linspace(2.0, 1.0, attack_n)

    # Layer 2: Body resonance — lower ping
    body = np.sin(2 * np.pi * body_freq * t) * np.exp(-(decay * 1.5) * t) * 0.25

    # Tiny noise transient at the start for the "click" snap
    noise_n = int(0.001 * SAMPLE_RATE)
    click_noise = np.zeros(n)
    if noise_n > 0:
        click_noise[:noise_n] = rng.uniform(-0.4, 0.4, noise_n)

    mixed = (tick * tick_env * attack + body + click_noise)
    return fade_out(mixed, 3.0)


# ---------------------------------------------------------------------------
#  5. Fish Strike  —  big splash + tension zing  (the dramatic moment)
# ---------------------------------------------------------------------------

def generate_fish_strike(
    duration_s: float,
    splash_freq: float,
    zing_start: float,
    zing_end: float,
    splash_mix: float,
    decay: float,
    rng: np.random.Generator,
) -> np.ndarray:
    """Big dramatic splash when a fish strikes the lure.

    Layers:
      - Heavy low-end impact (sub thud)
      - Aggressive splash noise burst (wider bandwidth than plop)
      - Rising tension zing (line going taut — upward pitch sweep)
      - Quick percussive attack
    """
    n = int(SAMPLE_RATE * duration_s)
    t = np.linspace(0, duration_s, n, endpoint=False)

    # Layer 1: Sub-bass impact
    sub = np.sin(2 * np.pi * splash_freq * t) * np.exp(-30 * t)

    # Layer 2: Heavy splash — wider, rougher noise burst
    raw = rng.uniform(-1.0, 1.0, n)
    splash_lo = bandpass_simple(raw, center_freq=400, bandwidth=350)
    splash_hi = bandpass_simple(rng.uniform(-1.0, 1.0, n), center_freq=1200, bandwidth=600)
    splash = (splash_lo * 0.7 + splash_hi * 0.4) * np.exp(-12 * t)

    # Layer 3: Tension zing — upward sine chirp (line snapping taut)
    zing = chirp(zing_start, zing_end, duration_s)
    # Delayed onset: zing starts ~50ms in (after the splash impact)
    zing_delay = int(0.05 * SAMPLE_RATE)
    zing_shifted = np.zeros(n)
    if zing_delay < n:
        remaining = n - zing_delay
        zing_shifted[zing_delay:] = zing[:remaining]
    zing_env = np.exp(-8 * t) * 0.35
    # Gentle swell for the zing (not instant)
    zing_attack_n = int(0.03 * SAMPLE_RATE)
    zing_attack = np.ones(n)
    if zing_attack_n > 0 and zing_attack_n < n:
        zing_attack[:zing_attack_n] = np.linspace(0, 1, zing_attack_n)

    # Overall envelope
    env = np.exp(-decay * t)
    # Percussive attack boost
    atk_n = int(0.008 * SAMPLE_RATE)
    atk = np.ones(n)
    if atk_n > 0 and atk_n < n:
        atk[:atk_n] = np.linspace(1.8, 1.0, atk_n)

    mixed = (
        sub * 1.0
        + splash * splash_mix
        + zing_shifted * zing_env * zing_attack
    ) * env * atk

    return fade_out(mixed, 10.0)


# ---------------------------------------------------------------------------
#  6. Fish Catch  —  celebratory ascending chime
# ---------------------------------------------------------------------------

def generate_catch_chime(
    base_freq: float,
    note_count: int,
    note_dur: float,
    interval_ratio: float,
    shimmer_amount: float,
    decay: float,
    rng: np.random.Generator,
) -> np.ndarray:
    """Celebratory ascending chime when catching a fish.

    A short sequence of ascending tones (like a success jingle)
    with sparkle shimmer overlay.
    """
    total_dur = note_dur * note_count + 0.15  # extra tail
    n = int(SAMPLE_RATE * total_dur)

    result = np.zeros(n)

    for note_i in range(note_count):
        freq = base_freq * (interval_ratio ** note_i)
        start_sample = int(note_i * note_dur * SAMPLE_RATE)
        note_len = int((total_dur - note_i * note_dur) * SAMPLE_RATE)
        note_len = min(note_len, n - start_sample)
        if note_len <= 0:
            continue

        t_note = np.linspace(0, note_len / SAMPLE_RATE, note_len, endpoint=False)

        # Clean tone — sine + soft 2nd harmonic for bell-like quality
        tone = (
            np.sin(2 * np.pi * freq * t_note)
            + 0.3 * np.sin(2 * np.pi * freq * 2 * t_note)
            + 0.1 * np.sin(2 * np.pi * freq * 3 * t_note)
        )

        # Each note has its own soft attack + decay
        note_attack_n = int(0.008 * SAMPLE_RATE)
        note_env = np.exp(-decay * t_note)
        if note_attack_n > 0 and note_attack_n < note_len:
            note_env[:note_attack_n] *= np.linspace(0, 1, note_attack_n)

        # Slight shimmer: very fast amplitude tremolo
        shimmer = 1.0 + shimmer_amount * np.sin(2 * np.pi * 28 * t_note)

        result[start_sample:start_sample + note_len] += tone * note_env * shimmer * 0.6

    # Add a sparkle layer — high-freq filtered noise at the start of each note
    for note_i in range(note_count):
        start_sample = int(note_i * note_dur * SAMPLE_RATE)
        sparkle_len = int(0.04 * SAMPLE_RATE)
        sparkle_len = min(sparkle_len, n - start_sample)
        if sparkle_len <= 0:
            continue
        sparkle = rng.uniform(-1.0, 1.0, sparkle_len)
        sparkle_env = np.exp(-80 * np.linspace(0, 0.04, sparkle_len))
        result[start_sample:start_sample + sparkle_len] += sparkle * sparkle_env * 0.15

    return fade_out(result, 12.0)


# ---------------------------------------------------------------------------
#  Main — generate all variants
# ---------------------------------------------------------------------------

def main() -> None:
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    rng = np.random.default_rng(RNG_SEED)
    total = 0

    print("=== Fishing SFX Generator ===\n")

    # --- 1. Cast Whoosh (4 variants) ---
    print("1. Cast Whoosh")
    for i in range(4):
        dur = rng.uniform(0.20, 0.30)
        sweep_start = rng.uniform(200, 400)
        sweep_end = rng.uniform(800, 1400)
        line_freq = rng.uniform(2000, 3500)
        line_mix = rng.uniform(0.08, 0.18)
        decay_val = rng.uniform(8, 14)

        samples = generate_cast_whoosh(
            duration_s=dur,
            sweep_start=sweep_start,
            sweep_end=sweep_end,
            line_freq=line_freq,
            line_mix=line_mix,
            decay=decay_val,
            rng=rng,
        )
        name = f"fishing_cast_{i:02d}.wav"
        write_wav(os.path.join(OUTPUT_DIR, name), samples)
        print(f"  {name}  ({dur*1000:.0f}ms, sweep {sweep_start:.0f}->{sweep_end:.0f}Hz)")
        total += 1

    # --- 2. Water Plop (4 variants) ---
    print("\n2. Water Plop")
    for i in range(4):
        dur = rng.uniform(0.15, 0.25)
        impact = rng.uniform(80, 140)
        bubble_freq = rng.uniform(250, 450)
        bubble_rate = rng.uniform(12, 22)
        nmix = rng.uniform(0.5, 0.8)
        decay_val = rng.uniform(10, 18)

        samples = generate_water_plop(
            duration_s=dur,
            impact_freq=impact,
            bubble_freq=bubble_freq,
            bubble_rate=bubble_rate,
            noise_mix=nmix,
            decay=decay_val,
            rng=rng,
        )
        name = f"fishing_plop_{i:02d}.wav"
        write_wav(os.path.join(OUTPUT_DIR, name), samples)
        print(f"  {name}  ({dur*1000:.0f}ms, impact {impact:.0f}Hz, bubbles {bubble_rate:.0f}Hz)")
        total += 1

    # --- 3. Lure Twitch (3 variants) ---
    print("\n3. Lure Twitch")
    for i in range(3):
        dur = rng.uniform(0.06, 0.10)
        click_freq = rng.uniform(1800, 3200)
        decay_val = rng.uniform(40, 70)

        samples = generate_twitch_pop(
            duration_s=dur,
            click_freq=click_freq,
            decay=decay_val,
            rng=rng,
        )
        name = f"fishing_twitch_{i:02d}.wav"
        write_wav(os.path.join(OUTPUT_DIR, name), samples)
        print(f"  {name}  ({dur*1000:.0f}ms, click {click_freq:.0f}Hz)")
        total += 1

    # --- 4. Reel Click (3 variants) ---
    print("\n4. Reel Click")
    for i in range(3):
        dur = rng.uniform(0.04, 0.07)
        click_freq = rng.uniform(3000, 5000)
        body_freq = rng.uniform(800, 1400)
        decay_val = rng.uniform(60, 100)

        samples = generate_reel_click(
            duration_s=dur,
            click_freq=click_freq,
            body_freq=body_freq,
            decay=decay_val,
            rng=rng,
        )
        name = f"fishing_reel_{i:02d}.wav"
        write_wav(os.path.join(OUTPUT_DIR, name), samples)
        print(f"  {name}  ({dur*1000:.0f}ms, click {click_freq:.0f}Hz, body {body_freq:.0f}Hz)")
        total += 1

    # --- 5. Fish Strike (4 variants) ---
    print("\n5. Fish Strike")
    for i in range(4):
        dur = rng.uniform(0.30, 0.45)
        splash_freq = rng.uniform(60, 110)
        zing_start = rng.uniform(400, 800)
        zing_end = rng.uniform(1800, 3000)
        splash_mix = rng.uniform(0.6, 0.9)
        decay_val = rng.uniform(4, 7)

        samples = generate_fish_strike(
            duration_s=dur,
            splash_freq=splash_freq,
            zing_start=zing_start,
            zing_end=zing_end,
            splash_mix=splash_mix,
            decay=decay_val,
            rng=rng,
        )
        name = f"fishing_strike_{i:02d}.wav"
        write_wav(os.path.join(OUTPUT_DIR, name), samples)
        print(f"  {name}  ({dur*1000:.0f}ms, sub {splash_freq:.0f}Hz, zing {zing_start:.0f}->{zing_end:.0f}Hz)")
        total += 1

    # --- 6. Fish Catch Chime (3 variants) ---
    print("\n6. Fish Catch Chime")
    for i in range(3):
        # Randomise the base pitch (pentatonic-adjacent feel)
        base_freq = rng.uniform(520, 700)
        note_count = rng.integers(3, 5)  # 3 or 4 notes
        note_dur = rng.uniform(0.10, 0.15)
        # Major-third-ish intervals for bright, happy feel
        interval = rng.uniform(1.18, 1.28)
        shimmer = rng.uniform(0.04, 0.12)
        decay_val = rng.uniform(6, 10)

        samples = generate_catch_chime(
            base_freq=base_freq,
            note_count=int(note_count),
            note_dur=note_dur,
            interval_ratio=interval,
            shimmer_amount=shimmer,
            decay=decay_val,
            rng=rng,
        )
        name = f"fishing_catch_{i:02d}.wav"
        write_wav(os.path.join(OUTPUT_DIR, name), samples)
        print(
            f"  {name}  ({note_count} notes @ {base_freq:.0f}Hz, "
            f"interval x{interval:.2f}, {note_dur*1000:.0f}ms/note)"
        )
        total += 1

    print(f"\n=== Generated {total} fishing SFX in {os.path.abspath(OUTPUT_DIR)} ===")


if __name__ == "__main__":
    main()
