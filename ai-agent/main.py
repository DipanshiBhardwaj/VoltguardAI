"""Voltaguard-AI agent entry point.

Polls the database every POLL_INTERVAL_SECONDS for audit reports that still need
a plain-English explanation, runs each through the LangGraph state machine, and
writes the findings back to the row.

Run with:
    python main.py
"""

import logging
import signal
import sys
import time

from voltaguard_agent.config import load_settings
from voltaguard_agent.db import connect, fetch_pending_reports, save_findings
from voltaguard_agent.graph import build_graph

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [agent] %(levelname)s %(message)s",
    datefmt="%H:%M:%S",
)
log = logging.getLogger(__name__)

_shutdown_requested = False


def _request_shutdown(_signum, _frame):
    global _shutdown_requested
    _shutdown_requested = True
    log.info("Shutdown signal received — finishing current cycle and exiting.")


def main() -> int:
    settings = load_settings()
    log.info(
        "Voltaguard agent starting. DB=%s/%s  Ollama=%s model=%s  poll=%ss batch=%s",
        settings.db_host, settings.db_name, settings.ollama_host,
        settings.ollama_model, settings.poll_interval_seconds, settings.batch_size,
    )

    graph = build_graph()

    signal.signal(signal.SIGINT, _request_shutdown)
    signal.signal(signal.SIGTERM, _request_shutdown)

    while not _shutdown_requested:
        try:
            with connect(settings) as conn:
                rows = fetch_pending_reports(conn, settings.batch_size)
                if not rows:
                    log.info("No pending reports. Sleeping.")
                else:
                    log.info("Found %d pending reports", len(rows))
                    for row in rows:
                        _process_one(conn, graph, settings, row)
        except Exception:
            # Wide except so one bad cycle doesn't kill the loop; the traceback
            # still prints for debugging.
            log.exception("Unexpected error in poll cycle")

        # Sleep in 1s slices so SIGINT lands quickly.
        for _ in range(settings.poll_interval_seconds):
            if _shutdown_requested:
                break
            time.sleep(1)

    log.info("Agent stopped.")
    return 0


def _process_one(conn, graph, settings, row: dict) -> None:
    log.info(
        "Processing report %s — %s, %s units, Rs %s",
        row["report_id"], row["billing_period"], row["total_units"], row["total_cost"],
    )

    highest_on = row.get("highest_demand_on")
    state = {
        "report_id":         row["report_id"],
        "billing_period":    row["billing_period"],
        "serial_number":     row["serial_number"],
        "slab_code":         row["slab_code"],
        "total_units":       float(row["total_units"]),
        "total_cost":        float(row["total_cost"]),
        "average_daily_kwh": float(row["average_daily_kwh"]),
        "peak_hour":         int(row["peak_hour"]),
        "morning_kwh":       float(row["morning_kwh"]),
        "day_kwh":           float(row["day_kwh"]),
        "evening_kwh":       float(row["evening_kwh"]),
        "night_kwh":         float(row["night_kwh"]),
        "highest_demand_kw": float(row["highest_demand_kw"]),
        "highest_demand_on": highest_on.strftime("%d %b %Y, %H:%M") if highest_on else "n/a",
        "_settings":         settings,
    }

    result = graph.invoke(state)
    markdown = result.get("findings_markdown", "")
    if not markdown:
        log.warning("Empty findings from LLM for report %s — skipping save.", row["report_id"])
        return

    save_findings(conn, row["report_id"], markdown)
    log.info("Wrote findings for report %s (%d chars)", row["report_id"], len(markdown))


if __name__ == "__main__":
    sys.exit(main())
