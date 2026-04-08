"""Generate front-door open/close sound effects.

Design goals:
    - door_open_creak: believable wooden hinge friction with irregular stick-slip,
        low body groan, narrow squeal resonances, and a very short quick-open gesture.
    - door_close_clunk: compact solid wooden thud with only a tiny latch accent,
        avoiding the brighter cabinet-clink character.

Output: 2 mono 16-bit 44100 Hz WAV files in Content/Audio/SFX/

The synthesis intentionally stays within the repo's existing tooling stack:
numpy + wave only.
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
RNG_SEED = 90210


def write_wav(filepath: str, samples: np.ndarray) -> None:
    """Write mono 16-bit PCM WAV, peak-normalised to 0.95."""
    peak = np.max(np.abs(samples))
    if peak > 0:
        samples = samples * (0.95 / peak)

    pcm = np.clip(samples * 32767, -32768, 32767).astype(np.int16)
    with wave.open(filepath, "w") as wav_file:
        wav_file.setnchannels(1)
        wav_file.setsampwidth(2)
        wav_file.setframerate(SAMPLE_RATE)
        wav_file.writeframes(pcm.tobytes())


def fade_out(signal: np.ndarray, fade_ms: float) -> np.ndarray:
    fade_samples = int(fade_ms * 0.001 * SAMPLE_RATE)
    if 0 < fade_samples < len(signal):
        signal[-fade_samples:] *= np.linspace(1.0, 0.0, fade_samples)
    return signal


def lowpass_noise(length_s: float, cutoff_hz: float, rng: np.random.Generator) -> np.ndarray:
    signal = rng.uniform(-1.0, 1.0, int(length_s * SAMPLE_RATE))
    rc = 1.0 / (2.0 * np.pi * max(cutoff_hz, 1.0))
    dt = 1.0 / SAMPLE_RATE
    alpha = dt / (rc + dt)
    output = np.empty_like(signal)
    output[0] = signal[0]
    for i in range(1, len(signal)):
        output[i] = output[i - 1] + alpha * (signal[i] - output[i - 1])
    return output


def bandpass_fft(signal: np.ndarray, center_hz: float, bandwidth_hz: float) -> np.ndarray:
    n = len(signal)
    spectrum = np.fft.rfft(signal)
    freqs = np.fft.rfftfreq(n, d=1.0 / SAMPLE_RATE)
    window = np.exp(-0.5 * ((freqs - center_hz) / max(bandwidth_hz * 0.5, 1.0)) ** 2)
    return np.fft.irfft(spectrum * window, n=n)


def sine_sweep(start_hz: float, end_hz: float, length_s: float) -> np.ndarray:
    sample_count = int(length_s * SAMPLE_RATE)
    t = np.linspace(0, length_s, sample_count, endpoint=False)
    ratio = end_hz / max(start_hz, 1e-6)
    freq = start_hz * ratio ** (t / max(length_s, 1e-6))
    phase = 2.0 * np.pi * np.cumsum(freq) / SAMPLE_RATE
    return np.sin(phase)


def multi_resonance(length_s: float, partials: list[tuple[float, float, float]]) -> np.ndarray:
    sample_count = int(length_s * SAMPLE_RATE)
    t = np.linspace(0, length_s, sample_count, endpoint=False)
    signal = np.zeros(sample_count)
    for frequency, amplitude, decay in partials:
        signal += amplitude * np.sin(2.0 * np.pi * frequency * t) * np.exp(-decay * t)
    return signal


def stick_slip_envelope(length_s: float, rng: np.random.Generator) -> np.ndarray:
    sample_count = int(length_s * SAMPLE_RATE)
    envelope = np.zeros(sample_count)
    cursor = 0
    while cursor < sample_count:
        gap = rng.integers(int(0.012 * SAMPLE_RATE), int(0.040 * SAMPLE_RATE))
        pulse_len = rng.integers(int(0.006 * SAMPLE_RATE), int(0.020 * SAMPLE_RATE))
        cursor += gap
        if cursor >= sample_count:
            break

        pulse_end = min(sample_count, cursor + pulse_len)
        pulse_t = np.linspace(0.0, 1.0, pulse_end - cursor, endpoint=False)
        pulse_shape = np.sin(np.pi * pulse_t) ** 1.6
        pulse_shape *= rng.uniform(0.45, 1.0)
        envelope[cursor:pulse_end] += pulse_shape
        cursor = pulse_end

    # Smooth the impulses so they feel like friction bursts, not clicks.
    kernel = np.exp(-np.linspace(0.0, 4.0, int(0.010 * SAMPLE_RATE)))
    kernel /= np.sum(kernel)
    smoothed = np.convolve(envelope, kernel, mode="same")
    return np.clip(smoothed, 0.0, None)


def generate_door_open_creak(rng: np.random.Generator) -> np.ndarray:
    """Synthesize a wooden door opening creak with stick-slip hinge motion."""
    duration_s = 0.34
    sample_count = int(duration_s * SAMPLE_RATE)
    t = np.linspace(0, duration_s, sample_count, endpoint=False)

    motion_env = np.sin(np.pi * np.clip(t / duration_s, 0.0, 1.0)) ** 1.35
    friction_env = stick_slip_envelope(duration_s, rng) * motion_env

    broad_noise = lowpass_noise(duration_s, cutoff_hz=3400.0, rng=rng)
    hinge_noise = bandpass_fft(broad_noise, center_hz=760.0, bandwidth_hz=900.0)
    scrape = hinge_noise * friction_env * 0.96

    squeal_a = sine_sweep(520.0, 700.0, duration_s)
    squeal_b = sine_sweep(1120.0, 1380.0, duration_s)
    squeal_c = sine_sweep(1760.0, 2040.0, duration_s)
    squeal_mix = (0.22 * squeal_a + 0.10 * squeal_b + 0.04 * squeal_c)
    squeal_mix *= np.clip(friction_env * 1.15, 0.0, 1.0)

    body_groan = multi_resonance(
        duration_s,
        [
            (104.0, 0.26, 7.0),
            (162.0, 0.14, 8.0),
            (244.0, 0.08, 9.5),
        ],
    ) * motion_env

    latch_click = np.zeros(sample_count)
    click_len = int(0.010 * SAMPLE_RATE)
    click_t = np.linspace(0.0, 0.010, click_len, endpoint=False)
    latch_click[:click_len] = (
        0.14 * np.sin(2.0 * np.pi * 1420.0 * click_t)
        + 0.05 * np.sin(2.0 * np.pi * 2100.0 * click_t)
    ) * np.exp(-150.0 * click_t)

    wood_rattle_noise = bandpass_fft(lowpass_noise(duration_s, 4200.0, rng), 280.0, 340.0)
    wood_rattle = wood_rattle_noise * np.exp(-12.0 * t) * 0.06

    signal = scrape + squeal_mix + body_groan + latch_click + wood_rattle
    signal *= np.exp(-1.3 * t)
    return fade_out(signal, 12.0)


def generate_door_close_clunk(rng: np.random.Generator) -> np.ndarray:
    """Synthesize a compact wooden thud with a restrained latch accent."""
    duration_s = 0.24
    sample_count = int(duration_s * SAMPLE_RATE)
    t = np.linspace(0, duration_s, sample_count, endpoint=False)

    body = multi_resonance(
        duration_s,
        [
            (74.0, 1.05, 24.0),
            (118.0, 0.58, 28.0),
            (182.0, 0.30, 34.0),
            (286.0, 0.10, 42.0),
        ],
    )

    impact_noise = bandpass_fft(lowpass_noise(duration_s, 3600.0, rng), 280.0, 700.0)
    impact_noise *= np.exp(-40.0 * t) * 0.22

    latch_delay = int(0.010 * SAMPLE_RATE)
    latch = np.zeros(sample_count)
    latch_len = int(0.014 * SAMPLE_RATE)
    latch_t = np.linspace(0.0, 0.014, latch_len, endpoint=False)
    latch_wave = (
        0.12 * np.sin(2.0 * np.pi * 1040.0 * latch_t)
        + 0.04 * np.sin(2.0 * np.pi * 1780.0 * latch_t)
    ) * np.exp(-165.0 * latch_t)
    latch[latch_delay:latch_delay + latch_len] = latch_wave[: max(0, sample_count - latch_delay)]

    after_rattle_delay = int(0.024 * SAMPLE_RATE)
    after_rattle = np.zeros(sample_count)
    rattle_len = int(0.045 * SAMPLE_RATE)
    rattle_t = np.linspace(0.0, 0.045, rattle_len, endpoint=False)
    rattle_wave = (
        0.08 * np.sin(2.0 * np.pi * 240.0 * rattle_t)
        + 0.04 * np.sin(2.0 * np.pi * 360.0 * rattle_t)
    ) * np.exp(-42.0 * rattle_t)
    after_rattle[after_rattle_delay:after_rattle_delay + rattle_len] = rattle_wave[: max(0, sample_count - after_rattle_delay)]

    signal = body + impact_noise + latch + after_rattle
    signal *= np.exp(-3.0 * t)
    return fade_out(signal, 14.0)


def main() -> None:
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    rng = np.random.default_rng(RNG_SEED)

    open_samples = generate_door_open_creak(rng)
    close_samples = generate_door_close_clunk(rng)

    open_path = os.path.join(OUTPUT_DIR, "door_open_creak.wav")
    close_path = os.path.join(OUTPUT_DIR, "door_close_clunk.wav")
    write_wav(open_path, open_samples)
    write_wav(close_path, close_samples)

    print(f"wrote {os.path.basename(open_path)} ({len(open_samples) / SAMPLE_RATE:.2f}s)")
    print(f"wrote {os.path.basename(close_path)} ({len(close_samples) / SAMPLE_RATE:.2f}s)")


if __name__ == "__main__":
    main()