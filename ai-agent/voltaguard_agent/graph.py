"""LangGraph state machine for explaining one audit report.

Two nodes:
1. summarise_usage — derives a one-line consumption note used to steer the model
2. call_local_llm   — asks Ollama to write the plain-English briefing

build_graph() returns a compiled graph the caller invokes once per report.
"""

from typing import TypedDict

import ollama
from langgraph.graph import StateGraph, END

from .config import Settings
from .prompts import SYSTEM_PROMPT, USER_PROMPT_TEMPLATE


class AuditState(TypedDict, total=False):
    report_id: int
    billing_period: str
    serial_number: str
    slab_code: str
    total_units: float
    total_cost: float
    average_daily_kwh: float
    peak_hour: int
    morning_kwh: float
    day_kwh: float
    evening_kwh: float
    night_kwh: float
    highest_demand_kw: float
    highest_demand_on: str

    usage_note: str
    findings_markdown: str

    _settings: Settings


def _summarise_usage(state: AuditState) -> AuditState:
    """Pick the heaviest time-of-day band from the measured figures, so the model
    has a correct, data-backed cue to lead with."""
    bands = {
        "morning (06:00-12:00)": state.get("morning_kwh", 0.0),
        "daytime (12:00-17:00)": state.get("day_kwh", 0.0),
        "evening (17:00-23:00)": state.get("evening_kwh", 0.0),
        "night (23:00-06:00)": state.get("night_kwh", 0.0),
    }
    heaviest = max(bands, key=bands.get)
    state["usage_note"] = f"The heaviest band in the data is {heaviest}."
    return state


def _call_local_llm(state: AuditState) -> AuditState:
    settings = state["_settings"]
    client = ollama.Client(host=settings.ollama_host)

    user_prompt = USER_PROMPT_TEMPLATE.format(
        serial_number=state["serial_number"],
        slab_code=state["slab_code"],
        billing_period=state["billing_period"],
        total_units=state["total_units"],
        total_cost=state["total_cost"],
        average_daily_kwh=state["average_daily_kwh"],
        peak_hour=state["peak_hour"],
        morning_kwh=state["morning_kwh"],
        day_kwh=state["day_kwh"],
        evening_kwh=state["evening_kwh"],
        night_kwh=state["night_kwh"],
        highest_demand_kw=state["highest_demand_kw"],
        highest_demand_on=state["highest_demand_on"],
    )

    response = client.chat(
        model=settings.ollama_model,
        messages=[
            {"role": "system", "content": SYSTEM_PROMPT + "\n\nUsage note: " + state["usage_note"]},
            {"role": "user", "content": user_prompt},
        ],
        options={"temperature": 0.3},
    )

    raw = response["message"]["content"].strip()

    # Some models wrap the whole reply in a ```markdown fence — strip it so the
    # frontend renders real Markdown rather than one code block.
    if raw.startswith("```"):
        first_newline = raw.find("\n")
        if first_newline != -1:
            raw = raw[first_newline + 1:]
        if raw.endswith("```"):
            raw = raw[:-3]
        raw = raw.strip()

    state["findings_markdown"] = raw
    return state


def build_graph():
    workflow = StateGraph(AuditState)
    workflow.add_node("summarise_usage", _summarise_usage)
    workflow.add_node("call_local_llm", _call_local_llm)
    workflow.set_entry_point("summarise_usage")
    workflow.add_edge("summarise_usage", "call_local_llm")
    workflow.add_edge("call_local_llm", END)
    return workflow.compile()
