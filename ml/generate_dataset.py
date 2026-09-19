"""
Synthetic Dataset Generator for Mirror
Generates realistic behavioral sequence archetypes for edge-inference training.
Each sample is a [60, 10] tensor representing 60 one-minute temporal bins with 10 features:
0: active_seconds
1: switch_count
2: unique_apps
3: reopen_count
4: longest_session_seconds
5: top_app_share
6: app_entropy
7: late_night_ratio
8: single_app_ratio
9: category_diversity
"""

import numpy as np
import os
import argparse

CLASS_NAMES = [
    "NORMAL",
    "HIGH_SWITCHING",
    "EXTENDED_SESSION",
    "RAPID_REOPEN",
    "LATE_NIGHT_OR_SCROLL_LIKE"
]

def generate_normal_sample(rng):
    # Moderate activity, moderate switching, 2-3 apps, balanced
    seq = np.zeros((60, 10), dtype=np.float32)
    for t in range(60):
        active = rng.uniform(0.3, 0.9)
        switches = rng.uniform(0.0, 0.3)
        unique_apps = rng.uniform(0.2, 0.6)
        reopens = rng.uniform(0.0, 0.2)
        longest = active * rng.uniform(0.4, 0.8)
        top_share = rng.uniform(0.4, 0.7)
        entropy = rng.uniform(0.3, 0.7)
        late_night = 0.0
        single_app = 0.0
        diversity = rng.uniform(0.2, 0.5)
        seq[t] = [active, switches, unique_apps, reopens, longest, top_share, entropy, late_night, single_app, diversity]
    return seq

def generate_high_switching_sample(rng):
    # Frequent switches, short sessions, many unique apps, high entropy
    seq = np.zeros((60, 10), dtype=np.float32)
    burst_start = rng.integers(10, 40)
    burst_len = rng.integers(10, 20)
    for t in range(60):
        in_burst = burst_start <= t < (burst_start + burst_len)
        active = rng.uniform(0.6, 1.0)
        switches = rng.uniform(0.7, 1.0) if in_burst else rng.uniform(0.3, 0.6)
        unique_apps = rng.uniform(0.6, 1.0) if in_burst else rng.uniform(0.4, 0.7)
        reopens = rng.uniform(0.1, 0.4)
        longest = active * rng.uniform(0.1, 0.3)
        top_share = rng.uniform(0.2, 0.4)
        entropy = rng.uniform(0.7, 1.0)
        late_night = 0.0
        single_app = 0.0
        diversity = rng.uniform(0.5, 0.9)
        seq[t] = [active, switches, unique_apps, reopens, longest, top_share, entropy, late_night, single_app, diversity]
    return seq

def generate_extended_session_sample(rng):
    # Long uninterrupted period in a single app, low switching, low entropy
    seq = np.zeros((60, 10), dtype=np.float32)
    for t in range(60):
        active = rng.uniform(0.8, 1.0)
        switches = rng.uniform(0.0, 0.1)
        unique_apps = 0.2  # 1 app
        reopens = 0.0
        longest = active
        top_share = 1.0
        entropy = 0.0
        late_night = 0.0
        single_app = 1.0
        diversity = 0.25
        seq[t] = [active, switches, unique_apps, reopens, longest, top_share, entropy, late_night, single_app, diversity]
    return seq

def generate_rapid_reopen_sample(rng):
    # Bursts of switching away and repeatedly reopening the same application
    seq = np.zeros((60, 10), dtype=np.float32)
    reopen_burst_start = rng.integers(10, 35)
    reopen_burst_len = rng.integers(15, 25)
    for t in range(60):
        in_burst = reopen_burst_start <= t < (reopen_burst_start + reopen_burst_len)
        active = rng.uniform(0.5, 0.9)
        switches = rng.uniform(0.4, 0.7) if in_burst else rng.uniform(0.1, 0.3)
        unique_apps = rng.uniform(0.3, 0.5)
        reopens = rng.uniform(0.6, 1.0) if in_burst else rng.uniform(0.0, 0.2)
        longest = active * rng.uniform(0.3, 0.6)
        top_share = rng.uniform(0.5, 0.8)
        entropy = rng.uniform(0.3, 0.6)
        late_night = 0.0
        single_app = 0.0
        diversity = rng.uniform(0.2, 0.5)
        seq[t] = [active, switches, unique_apps, reopens, longest, top_share, entropy, late_night, single_app, diversity]
    return seq

def generate_late_night_sample(rng):
    # Late night hours, continuous usage, low app diversity, scroll-like pattern
    seq = np.zeros((60, 10), dtype=np.float32)
    for t in range(60):
        active = rng.uniform(0.7, 1.0)
        switches = rng.uniform(0.2, 0.5)
        unique_apps = rng.uniform(0.2, 0.4)
        reopens = rng.uniform(0.4, 0.8)
        longest = active * rng.uniform(0.5, 0.8)
        top_share = rng.uniform(0.6, 0.9)
        entropy = rng.uniform(0.2, 0.5)
        late_night = 1.0
        single_app = rng.choice([0.0, 1.0], p=[0.4, 0.6])
        diversity = rng.uniform(0.2, 0.4)
        seq[t] = [active, switches, unique_apps, reopens, longest, top_share, entropy, late_night, single_app, diversity]
    return seq

def generate_dataset(samples_per_class=400, seed=42, output_path="ml/dataset.npz"):
    rng = np.random.default_rng(seed)
    generators = [
        generate_normal_sample,
        generate_high_switching_sample,
        generate_extended_session_sample,
        generate_rapid_reopen_sample,
        generate_late_night_sample
    ]

    X = []
    y = []

    for class_idx, gen_func in enumerate(generators):
        for _ in range(samples_per_class):
            sample = gen_func(rng)
            # Add small random noise for regularization
            noise = rng.normal(0.0, 0.02, sample.shape).astype(np.float32)
            sample = np.clip(sample + noise, 0.0, 1.0)
            X.append(sample)
            y.append(class_idx)

    X = np.array(X, dtype=np.float32)
    y = np.array(y, dtype=np.int64)

    # Shuffle
    perm = rng.permutation(len(X))
    X = X[perm]
    y = y[perm]

    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    np.savez_compressed(output_path, X=X, y=y, class_names=CLASS_NAMES)
    print(f"Generated {len(X)} synthetic behavioral sequences with shape {X.shape} saved to {output_path}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--samples_per_class", type=int, default=400)
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--output", type=str, default="ml/dataset.npz")
    args = parser.parse_args()
    generate_dataset(args.samples_per_class, args.seed, args.output)
