"""Generate a sample smart-meter ledger for Voltguard-AI.

Writes samples/sample-ledger-2026-05.csv: one domestic meter's 15-minute readings
for 1-31 May 2026, with IST timestamps written without an offset (the way Indian
utility portals export them). Standard library only, with a fixed random seed so
the file is identical every time it is generated.

The file also contains, on purpose:
  * invalid lines the upload must reject (wrong column count, bad timestamp,
    non-numeric value, impossible 999 V reading), and
  * valid but abnormal readings for the anomaly checks (a low-voltage sag to
    about 205 V, and a phase/neutral current mismatch).

Run from the repository root:
    python tools/generate_sample_ledger.py
"""

import csv
import random
from datetime import datetime, timedelta
from pathlib import Path

SEED = 2026
START = datetime(2026, 5, 1, 0, 0)          # IST wall-clock time
END = datetime(2026, 6, 1, 0, 0)            # exclusive
STEP = timedelta(minutes=15)
STARTING_KWH = 1200.0
MIN_LOAD_KW = 0.08                          # fridge compressor off, standby only
TIMESTAMP_FORMAT = "%Y-%m-%d %H:%M"

HEADER = [
    "IntervalEnding", "Voltage", "ActivePower", "PhaseCurrent",
    "NeutralCurrent", "PowerFactor", "Frequency", "CumulativeKWH",
]

OUTPUT = Path(__file__).resolve().parent.parent / "samples" / "sample-ledger-2026-05.csv"

# Valid-but-abnormal readings (IST). The sag sits inside the accepted 100-300 V
# range but well below the normal 230 V +/- 6% supply band.
SAG_START = datetime(2026, 5, 12, 19, 0)
SAG_END = datetime(2026, 5, 12, 20, 0)
MISMATCH_TIMES = {
    datetime(2026, 5, 20, 8, 0),
    datetime(2026, 5, 20, 8, 15),
    datetime(2026, 5, 20, 8, 30),
}

# Lines the upload must reject, inserted after the reading at the given time.
INVALID_LINES = {
    datetime(2026, 5, 3, 10, 0): "2026-05-03 10:05,231.2,1.10,5.02",                      # 4 columns
    datetime(2026, 5, 9, 14, 0): "2026-05-09 25:70,229.8,0.60,2.75,2.74,0.95,50.01,0",     # bad timestamp
    datetime(2026, 5, 16, 21, 0): "2026-05-16 21:05,abc,2.40,10.80,10.79,0.96,49.98,0",    # non-numeric
    datetime(2026, 5, 25, 7, 0): "2026-05-25 07:05,999,1.90,8.50,8.49,0.95,50.02,0",       # impossible voltage
}


def base_load_kw(hour: int, weekend: bool) -> float:
    """Typical household draw for an hour of the day, in kW."""
    # Tuned to roughly 9-10 kWh a day (about 300 units a month), typical for
    # an Indian household with a geyser and a cooler.
    if 6 <= hour < 10:          # morning: geyser, kitchen, iron
        load = 0.65
    elif 10 <= hour < 17:       # daytime: fridge, fans, some appliances
        load = 0.4 if weekend else 0.25
    elif 17 <= hour < 23:       # evening peak: lights, TV, cooking, cooler
        load = 0.8
    else:                       # night: fridge and standby
        load = 0.12
    return load


def generate(rng: random.Random) -> list[list[str] | str]:
    rows: list[list[str] | str] = []
    cumulative = STARTING_KWH
    t = START

    while t < END:
        weekend = t.weekday() >= 5
        load = base_load_kw(t.hour, weekend)
        power = round(max(MIN_LOAD_KW, rng.gauss(load, load * 0.3)), 2)

        voltage = min(max(rng.gauss(230.0, 2.5), 220.0), 240.0)
        if SAG_START <= t < SAG_END:
            voltage = rng.uniform(203.5, 206.5)
        voltage = round(voltage, 1)

        power_factor = round(rng.uniform(0.88, 0.98), 2)
        frequency = round(min(max(rng.gauss(50.0, 0.04), 49.9), 50.1), 2)

        # Single-phase supply: in a healthy installation the neutral carries the
        # same current as the phase.
        phase_current = power * 1000 / (voltage * power_factor)
        neutral_current = phase_current + rng.uniform(-0.03, 0.03)
        if t in MISMATCH_TIMES:
            neutral_current = phase_current * 0.3    # most current returning by another path

        # Accumulate the rounded power so units and power always agree. Every
        # slice adds at least 0.02 kWh (> 0.01), so the 2-dp value strictly increases.
        cumulative += power * 0.25

        rows.append([
            t.strftime(TIMESTAMP_FORMAT),
            f"{voltage:.1f}",
            f"{power:.2f}",
            f"{phase_current:.2f}",
            f"{max(neutral_current, 0):.2f}",
            f"{power_factor:.2f}",
            f"{frequency:.2f}",
            f"{cumulative:.2f}",
        ])

        if t in INVALID_LINES:
            rows.append(INVALID_LINES[t])

        t += STEP

    return rows


def main() -> None:
    rows = generate(random.Random(SEED))
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)

    with OUTPUT.open("w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f, lineterminator="\n")
        writer.writerow(HEADER)
        for row in rows:
            if isinstance(row, str):
                f.write(row + "\n")   # deliberately malformed, written as-is
            else:
                writer.writerow(row)

    valid = sum(1 for r in rows if not isinstance(r, str))
    print(f"Wrote {OUTPUT} ({valid} readings, {len(rows) - valid} invalid lines)")


if __name__ == "__main__":
    main()
