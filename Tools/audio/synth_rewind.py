#!/usr/bin/env python3
"""Synthesizes the three Rewind loop candidates (original Echoryx sounds, no third-party material).

Every loop is built on a circular buffer: grains, ticks, echoes and filters wrap from the end to the start,
so the file is periodic and has no seam. Needs numpy and ffmpeg. Run from the repository root:
    python3 Tools/audio/synth_rewind.py
"""
import re
import subprocess
import tempfile
import wave
from pathlib import Path

import numpy as np

RATE = 44100
SECONDS = 2.0
N = int(RATE * SECONDS)
OUT = Path("Assets/_Echoryx/Audio")
TARGET_LUFS = -18.0
CEILING = 10 ** (-1.5 / 20)  # leaves room for the Vorbis encoder


def shape(signal, points):
    """Circular zero-phase EQ: gain interpolated between (Hz, gain) points, applied to the whole loop."""
    freqs = np.fft.rfftfreq(len(signal), 1 / RATE)
    hz, gain = zip(*points)
    return np.fft.irfft(np.fft.rfft(signal) * np.interp(freqs, hz, gain), len(signal))


def highpass(signal):
    # Nothing at all below 380 Hz, smooth rise to full level at 560 Hz.
    freqs = np.fft.rfftfreq(len(signal), 1 / RATE)
    ramp = np.clip((freqs - 380) / 180, 0, 1)
    return np.fft.irfft(np.fft.rfft(signal) * np.sin(ramp * np.pi / 2) ** 2, len(signal))


def band_noise(rng, length, low, high):
    """Noise limited to [low, high] Hz with soft edges."""
    size = max(length, 2048)
    freqs = np.fft.rfftfreq(size, 1 / RATE)
    gain = np.clip((freqs - low * 0.85) / (low * 0.15), 0, 1) * np.clip((high * 1.15 - freqs) / (high * 0.15), 0, 1)
    noise = np.fft.irfft(np.fft.rfft(rng.standard_normal(size)) * gain, size)[:length]
    return noise / (np.std(noise) + 1e-9)


def sweeping_noise(rng, centre_at, octaves_wide):
    """Loop-long noise through a band-pass whose centre follows centre_at(t); circular overlap-add."""
    size, hop = 1024, 147
    assert N % hop == 0
    source = rng.standard_normal(N)
    window = np.hanning(size)
    freqs = np.fft.rfftfreq(size, 1 / RATE)
    out = np.zeros(N)
    for start in range(0, N, hop):
        index = (start + np.arange(size)) % N
        centre = centre_at((start + size / 2) / RATE % SECONDS)
        mask = np.exp(-0.5 * (np.log2(np.maximum(freqs, 1) / centre) / octaves_wide) ** 2)
        out[index] += np.fft.irfft(np.fft.rfft(source[index] * window) * mask, size) * window
    return out / np.std(out)


def add(buffer, start, grain, gain=1.0):
    index = (int(start) + np.arange(len(grain))) % N
    buffer[index] += grain * gain


def faded(length, attack, release):
    envelope = np.ones(length)
    a, r = int(attack * RATE), int(release * RATE)
    envelope[:a] = np.sin(np.linspace(0, np.pi / 2, a)) ** 2
    envelope[-r:] *= np.cos(np.linspace(0, np.pi / 2, r)) ** 2
    return envelope


def tape(rng):
    """A: tape rewinding. Rapid bursts of 1-6 kHz noise, each rising; the run of bursts climbs and restarts."""
    out = np.zeros(N)
    bursts = 28  # 14 per second
    step = N / bursts
    for i in range(bursts):
        climb = (i % 7) / 6  # four climbs per loop
        length = int(step * rng.uniform(0.74, 0.9))
        time = np.linspace(0, 1, length)
        noise = sweeping_noise(rng, lambda t: 2000, 3.0)[:length]
        # Rising brightness inside the burst: crossfade through three noise bands.
        low, mid, high = (band_noise(rng, length, a, b) for a, b in ((1000, 2300), (2000, 3900), (3500, 6000)))
        position = np.clip(climb * 0.45 + time * 0.75, 0, 1) * 2
        swept = sum(band * np.clip(1 - np.abs(position - k), 0, 1) for k, band in enumerate((low, mid, high)))
        add(out, i * step, (swept + 0.12 * noise) * faded(length, 0.006, 0.022), rng.uniform(0.75, 1.0))
    hiss = band_noise(rng, N, 2500, 6000)
    out += 0.10 * hiss
    return shape(out, [(0, 0), (800, 0), (1100, 1), (6000, 1), (7500, 0.1), (22050, 0)])


def clockwork(rng):
    """B: an accelerated mechanism. Dry clicks in a tick-tock pattern with a soft shimmer on top."""
    out = np.zeros(N)
    ticks = 32  # 16 per second
    step = N / ticks
    for i in range(ticks):
        strong = i % 4 == 0
        for part, (delay, level) in enumerate(((0.0, 1.0), (0.011, 0.38))):  # click and its little rebound
            length = int(RATE * (0.016 if strong else 0.011))
            band = (1400, 4200) if i % 2 == 0 else (2400, 7000)
            body = band_noise(rng, length, *band) * np.exp(-np.arange(length) / (RATE * 0.0022))
            ring = np.sin(2 * np.pi * (2350 if i % 2 == 0 else 3300) * np.arange(length) / RATE)
            ring *= np.exp(-np.arange(length) / (RATE * 0.0016))
            click = (body + 0.9 * ring) * faded(length, 0.0006, 0.003)
            jitter = rng.uniform(-0.002, 0.002) * RATE
            add(out, i * step + delay * RATE + jitter + 200, click, level * (1.0 if strong else 0.72))
    shimmer = band_noise(rng, N, 6000, 11000)
    t = np.arange(N) / RATE
    out += 0.035 * shimmer * (0.65 + 0.35 * np.sin(2 * np.pi * 3 * t) * np.sin(2 * np.pi * 8 * t))
    return out


def whoosh(rng):
    """C: sci-fi whoosh. Fast rising band-pass sweeps with a short echo and a barely audible airy tone."""
    sweeps = 4  # every half second
    period = SECONDS / sweeps
    centre = lambda t: 700 * (5200 / 700) ** (((t % period) / period) ** 1.4)
    t = np.arange(N) / RATE
    phase = (t % period) / period
    envelope = np.sin(np.pi * phase) ** 1.6  # silent where the sweep jumps back down
    air = sweeping_noise(rng, centre, 0.32) * envelope
    # A very quiet detuned shimmer following the sweep, amplitude-fluttered so it never holds a note.
    frequency = np.array([centre(x) for x in t]) * 1.5
    tone = np.sin(2 * np.pi * np.cumsum(frequency) / RATE) * envelope * (0.5 + 0.5 * np.sin(2 * np.pi * 23 * t))
    dry = air + 0.07 * tone
    wet = dry.copy()
    for k in range(1, 4):
        wet += 0.38 ** k * np.roll(shape(dry, [(0, 1), (3000, 1), (9000, 0.4), (22050, 0.2)]), int(k * 0.115 * RATE))
    return wet


def loudness(path):
    report = subprocess.run(["ffmpeg", "-hide_banner", "-nostats", "-stream_loop", "2", "-i", str(path),
                             "-af", "ebur128=peak=true", "-f", "null", "-"], capture_output=True, text=True).stderr
    summary = report[report.rindex("Summary:"):]
    return (float(re.search(r"I:\s+(-?[\d.]+)", summary).group(1)),
            float(re.search(r"Peak:\s+(-?[\d.]+)", summary).group(1)))


def write_wav(path, signal):
    with wave.open(str(path), "wb") as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(RATE)
        out.writeframes((np.clip(signal, -1, 1) * 32767).astype("<i2").tobytes())


def soft_limit(signal, ceiling):
    knee = ceiling * 0.6
    over = np.abs(signal) > knee
    out = signal.copy()
    out[over] = np.sign(signal[over]) * (knee + (ceiling - knee) * np.tanh((np.abs(signal[over]) - knee) / (ceiling - knee)))
    return out


def render(name, signal, work):
    signal = highpass(signal)
    signal /= np.max(np.abs(signal))
    gain = 0.5
    wav = Path(work) / (name + ".wav")
    for _ in range(12):  # find the gain that lands on the target after soft limiting
        # Limiting makes a little low distortion, so the high-pass runs again afterwards.
        done = highpass(soft_limit(signal * gain, CEILING * 0.95))
        done *= min(1.0, CEILING / np.max(np.abs(done)))
        write_wav(wav, done)
        level, _ = loudness(wav)
        if abs(level - TARGET_LUFS) < 0.15:
            break
        gain *= 10 ** ((TARGET_LUFS - level) / 20)
    target = OUT / (name + ".ogg")
    subprocess.run(["ffmpeg", "-hide_banner", "-v", "error", "-y", "-i", str(wav), "-c:a", "libvorbis", "-q:a", "7",
                    "-map_metadata", "-1", "-fflags", "+bitexact", "-flags:a", "+bitexact", str(target)], check=True)
    return target


def verify(path):
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", str(path), "-f", "f32le", "-ac", "1", "-"],
                         capture_output=True, check=True).stdout
    x = np.frombuffer(raw, "<f4").astype(float)
    spectrum = np.abs(np.fft.rfft(x)) ** 2
    freqs = np.fft.rfftfreq(len(x), 1 / RATE)
    low = 10 * np.log10(spectrum[freqs < 300].sum() / spectrum.sum() + 1e-20)
    steps = np.abs(np.diff(x))
    level, true_peak = loudness(path)
    print(f"{path.name}: {len(x)} muestras, {level} LUFS, pico real {true_peak} dBFS, "
          f"energía <300 Hz {low:.1f} dB del total, costura {abs(x[0] - x[-1]):.4f} (p99 entre muestras {np.percentile(steps, 99):.4f}), "
          f"máx {np.max(np.abs(x)):.3f}")


if __name__ == "__main__":
    with tempfile.TemporaryDirectory() as work:
        for seed, (name, make) in enumerate((("rewind-A", tape), ("rewind-B", clockwork), ("rewind-C", whoosh)), start=1):
            verify(render(name, make(np.random.default_rng(seed)), work))
