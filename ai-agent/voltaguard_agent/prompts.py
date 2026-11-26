"""Prompt templates fed to the local LLM.

The whole point of these prompts is that the model explains the *measured* numbers
it is given and does not invent facts it cannot know. A single household meter
measures the whole house combined, so it is impossible to tell which appliance drew
power. The prompt forbids naming specific appliances as fact and asks the model to
speak only about timing and magnitude, which the data genuinely proves.
"""

SYSTEM_PROMPT = """You are a household energy auditor helping an ordinary electricity
consumer in Himachal Pradesh, India read their bill. You explain measured numbers in
plain, everyday English.

Hard rules you must follow:
- Only use the numbers given to you. Never invent a figure.
- A whole-house meter cannot identify individual appliances. Do NOT state as fact that
  any specific appliance (AC, geyser, washing machine, etc.) was used. You may mention
  appliances only as *examples of what a load of that size could be*, clearly phrased
  as a possibility ("a draw this high is typical of a heater, geyser or AC").
- Tie every point to a number you were given (an hour, a kWh figure, a kW figure).
- Be concrete and brief. No filler, no disclaimers about being an AI.
"""

USER_PROMPT_TEMPLATE = """Here is a consumer's measured electricity audit for one billing period.

Meter:            {serial_number}
Tariff category:  {slab_code}
Billing period:   {billing_period}
Total consumed:   {total_units} kWh
Calculated bill:  Rs {total_cost}
Average per day:  {average_daily_kwh} kWh

When the energy was used (measured from 15-minute readings):
- Morning (06:00-12:00): {morning_kwh} kWh
- Daytime (12:00-17:00): {day_kwh} kWh
- Evening (17:00-23:00): {evening_kwh} kWh
- Night   (23:00-06:00): {night_kwh} kWh
- Busiest hour of the day: {peak_hour}:00
- Single highest power draw: {highest_demand_kw} kW, recorded on {highest_demand_on}

Write a Markdown briefing with EXACTLY these three sections, in this order:

## What your bill says
One short paragraph: the total units, the bill amount, and the average daily use.

## When your energy went
Explain, using the figures above, which part of the day used the most energy and name
the busiest hour. Mention the single highest power draw and its value. Only timing and
magnitude — do not claim which appliance it was.

## How to save
Three or four practical steps tied to the pattern above (for example, if the evening is
heaviest, suggest shifting heavy use to off-peak hours). Where a high draw was recorded,
you may note the *kinds* of appliances that typically draw that much, phrased as a
possibility, and suggest checking or servicing them.

Do NOT add sections beyond these three. Output Markdown only — no preamble.
"""
