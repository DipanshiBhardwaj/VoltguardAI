"""PostgreSQL access for the agent.

Plain parameterised SQL over psycopg 3. The agent only touches the audit-report
and telemetry tables, so an ORM would be more weight than it's worth here.
"""

from contextlib import contextmanager
from typing import Iterator

import psycopg
from psycopg.rows import dict_row

from .config import Settings


@contextmanager
def connect(settings: Settings) -> Iterator[psycopg.Connection]:
    conn = psycopg.connect(
        host=settings.db_host,
        port=settings.db_port,
        dbname=settings.db_name,
        user=settings.db_user,
        password=settings.db_password,
        row_factory=dict_row,
    )
    try:
        yield conn
    finally:
        conn.close()


def fetch_pending_reports(conn: psycopg.Connection, limit: int) -> list[dict]:
    """Return the oldest audit reports the agent has not yet explained.

    Joins to the meter so the prompt knows the tariff category the bill was
    computed under.
    """
    sql = """
        SELECT
            r."AuditReportId"                AS report_id,
            r."BillingCyclePeriod"           AS billing_period,
            r."TotalUnits"                   AS total_units,
            r."CalculatedDeterministicCost"  AS total_cost,
            r."PeakHour"                     AS peak_hour,
            r."MorningKwh"                   AS morning_kwh,
            r."DayKwh"                       AS day_kwh,
            r."EveningKwh"                   AS evening_kwh,
            r."NightKwh"                     AS night_kwh,
            r."HighestDemandKW"              AS highest_demand_kw,
            r."HighestDemandOn"              AS highest_demand_on,
            r."AverageDailyKwh"              AS average_daily_kwh,
            m."SerialNumber"                 AS serial_number,
            m."SlabCode"                     AS slab_code
        FROM "EnergyAuditReport" r
        JOIN "SmartMeterAsset" m ON m."MeterAssetId" = r."MeterAssetId"
        WHERE r."Status" = 'Pending'
          AND r."IsDeleted" = FALSE
        ORDER BY r."CreatedOn" ASC
        LIMIT %s
    """
    with conn.cursor() as cur:
        cur.execute(sql, (limit,))
        return cur.fetchall()


def save_findings(conn: psycopg.Connection, report_id: int, markdown: str) -> None:
    """Write the plain-English findings back and mark the report Completed so it
    is never processed twice."""
    sql = """
        UPDATE "EnergyAuditReport"
        SET "AiAnalysisFindings" = %s,
            "Status" = 'Completed'
        WHERE "AuditReportId" = %s
    """
    with conn.cursor() as cur:
        cur.execute(sql, (markdown, report_id))
    conn.commit()
