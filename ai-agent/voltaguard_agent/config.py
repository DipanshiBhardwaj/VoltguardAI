"""Centralised config loader. Reads from .env via python-dotenv."""

import os
from dataclasses import dataclass
from dotenv import load_dotenv

# Load .env once when this module is imported.
load_dotenv()


@dataclass
class Settings:
    db_host: str
    db_port: int
    db_name: str
    db_user: str
    db_password: str
    ollama_host: str
    ollama_model: str
    poll_interval_seconds: int
    batch_size: int


def load_settings() -> Settings:
    return Settings(
        db_host=os.getenv("DB_HOST", "localhost"),
        db_port=int(os.getenv("DB_PORT", "5432")),
        db_name=os.getenv("DB_NAME", "voltaguard"),
        db_user=os.getenv("DB_USER", "voltaguard"),
        db_password=os.getenv("DB_PASSWORD", "voltaguard_dev_pw"),
        ollama_host=os.getenv("OLLAMA_HOST", "http://localhost:11434"),
        ollama_model=os.getenv("OLLAMA_MODEL", "mistral"),
        poll_interval_seconds=int(os.getenv("POLL_INTERVAL_SECONDS", "30")),
        batch_size=int(os.getenv("BATCH_SIZE", "5")),
    )
