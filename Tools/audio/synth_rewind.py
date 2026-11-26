#!/usr/bin/env python3
"""Synthesizes the Rewind loop (an original Echoryx sound, no third-party material).

The loop is built on a circular buffer: noise frames, echoes and filters wrap from the end to the start,
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


def whoosh(rng):
    """Sci-fi whoosh: fast rising band-pass noise sweeps with a short echo. Noise only, no tone."""
    sweeps = 4  # every half second
    period = SECONDS / sweeps
    centre = lambda t: 700 * (5200 / 700) ** (((t % period) / period) ** 1.4)
    phase = (np.arange(N) / RATE % period) / period
    envelope = np.sin(np.pi * phase) ** 1.6  # silent where the sweep jumps back down
    dry = sweeping_noise(rng, centre, 0.32) * envelope
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
        verify(render("rewind-C", whoosh(np.random.default_rng(3)), work))
