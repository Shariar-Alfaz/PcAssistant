from __future__ import annotations

from contextlib import asynccontextmanager
import re
import warnings
from pathlib import Path
from typing import Any

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

try:
    import joblib
except ImportError:  # pragma: no cover - surfaced in /health and /predict
    joblib = None

try:
    from sklearn.exceptions import InconsistentVersionWarning
except ImportError:  # pragma: no cover - surfaced when sklearn is missing
    InconsistentVersionWarning = None


SUPPORTED_LABELS = {
    "filesystem.create_folder",
    "filesystem.delete_folder",
    "system.restart",
    "system.cancel_restart",
    "unknown",
}

CONFIRMATION_LABELS = {
    "filesystem.delete_folder",
    "system.restart",
}

DANGEROUS_PATTERNS = (
    r"\bformat\b",
    r"\bdelete\s+(everything|all|system32|windows|program files|c drive|root)\b",
    r"\bremove\s+(everything|all|system32|windows|program files|c drive|root)\b",
    r"\bsystem32\b",
    r"\bc:\\windows\b",
    r"\bc:\\program files\b",
    r"\bc:\\program files \(x86\)\b",
    r"\bc:\\\s*$",
    r"\broot drive\b",
)

MODEL_PATH = Path(__file__).resolve().parent / "model" / "pc_assistant_command_classifier_best.joblib"


class CommandRequest(BaseModel):
    text: str = Field(min_length=1, max_length=2000)


class CommandResponse(BaseModel):
    input: str
    commandLabel: str
    rawPredictedLabel: str
    source: str
    confidence: float
    requiresConfirmation: bool
    probabilities: dict[str, float]


class HealthResponse(BaseModel):
    status: str
    modelPath: str
    modelLoaded: bool
    modelMetadata: dict[str, Any] = Field(default_factory=dict)
    modelError: str | None = None


class ModelState:
    def __init__(self) -> None:
        self.model: Any | None = None
        self.metadata: dict[str, Any] = {}
        self.warnings: list[str] = []
        self.error: str | None = None

    @property
    def loaded(self) -> bool:
        return self.model is not None


model_state = ModelState()


@asynccontextmanager
async def lifespan(_: FastAPI):
    load_model()
    yield


app = FastAPI(title="PC Assistant Command Classifier", lifespan=lifespan)


def normalize_text(text: str) -> str:
    value = text.lower()
    value = re.sub(r"\bpc\b", "computer", value)
    value = re.sub(r"\bdir\b", "directory", value)
    value = re.sub(
        r"(?<!\w)(?:[a-z]:\\(?:[^\\/:*?\"<>|\r\n]+\\?)*|%[a-z_][a-z0-9_]*%(?:\\[^\\/:*?\"<>|\r\n]+)*)",
        " <path> ",
        value,
        flags=re.IGNORECASE,
    )
    value = re.sub(r"(?<!\w)(?:\.{1,2}[\\/]|~[\\/]|/[^\s]+)", " <path> ", value)
    value = re.sub(r"\b\d+\b", "<number>", value)
    value = re.sub(r"\s+", " ", value).strip()
    return value


def is_dangerous(text: str) -> bool:
    value = text.lower()
    return any(re.search(pattern, value) for pattern in DANGEROUS_PATTERNS)


def rule_override(text: str) -> tuple[str, float] | None:
    value = text.lower()
    if is_dangerous(value):
        return "unknown", 1.0

    if re.search(r"\b(cancel|abort|stop)\b.*\b(restart|reboot|shutdown)\b", value):
        return "system.cancel_restart", 0.98

    if re.search(r"\b(restart|reboot)\b", value):
        return "system.restart", 0.95

    if re.search(r"\b(delete|remove|erase)\b.*\b(folder|directory|dir)\b", value):
        return "filesystem.delete_folder", 0.96

    if re.search(r"\b(create|make|new)\b.*\b(folder|directory|dir)\b", value):
        return "filesystem.create_folder", 0.96

    return None


def load_model() -> None:
    if joblib is None:
        model_state.error = "joblib is not installed."
        return

    if not MODEL_PATH.exists():
        model_state.error = f"Model file is missing: {MODEL_PATH}"
        return

    try:
        warning_category = InconsistentVersionWarning or Warning
        with warnings.catch_warnings(record=True) as caught_warnings:
            warnings.simplefilter("always", warning_category)
            payload = joblib.load(MODEL_PATH)

        version_warnings = [
            str(warning.message)
            for warning in caught_warnings
            if InconsistentVersionWarning is not None
            and issubclass(warning.category, InconsistentVersionWarning)
        ]

        if isinstance(payload, dict):
            model = payload.get("model")
            model_state.metadata = {
                key: value
                for key, value in payload.items()
                if key != "model"
                and isinstance(value, (str, int, float, bool, list, tuple, dict, type(None)))
            }
        else:
            model = payload
            model_state.metadata = {}

        if not hasattr(model, "predict"):
            model_state.model = None
            model_state.error = "Model payload does not contain a predictor."
            return

        if version_warnings:
            model_state.metadata["model_version_warnings"] = version_warnings

        model_state.model = model
        model_state.warnings = version_warnings
        model_state.error = None
    except Exception as exc:  # pragma: no cover - depends on external model shape
        model_state.model = None
        model_state.metadata = {}
        model_state.warnings = []
        model_state.error = f"Failed to load model: {exc}"


def model_predict(normalized_text: str) -> tuple[str, float, dict[str, float]]:
    if model_state.model is None:
        raise HTTPException(status_code=503, detail=model_state.error or "Model is not loaded.")

    model = model_state.model
    if not hasattr(model, "predict"):
        raise HTTPException(status_code=503, detail="Model payload does not contain a predictor.")

    raw_label = str(model.predict([normalized_text])[0])
    probabilities: dict[str, float] = {}
    confidence = 1.0

    if hasattr(model, "predict_proba"):
        proba = model.predict_proba([normalized_text])[0]
        classes = [str(label) for label in getattr(model, "classes_", [])]
        probabilities = {
            label: round(float(probability), 4)
            for label, probability in zip(classes, proba, strict=False)
        }
        confidence = round(float(max(proba)), 4) if len(proba) else 0.0

    command_label = raw_label if raw_label in SUPPORTED_LABELS else "unknown"
    return command_label, confidence, probabilities


@app.get("/health", response_model=HealthResponse)
async def health() -> HealthResponse:
    return HealthResponse(
        status="ok" if model_state.loaded else "degraded",
        modelPath=str(MODEL_PATH),
        modelLoaded=model_state.loaded,
        modelMetadata=model_state.metadata,
        modelError=model_state.error,
    )


@app.post("/predict", response_model=CommandResponse)
async def predict(request: CommandRequest) -> CommandResponse:
    input_text = request.text.strip()
    normalized = normalize_text(input_text)

    override = rule_override(input_text)
    if override is not None:
        label, confidence = override
        return CommandResponse(
            input=input_text,
            commandLabel=label,
            rawPredictedLabel=label,
            source="rule",
            confidence=confidence,
            requiresConfirmation=label in CONFIRMATION_LABELS,
            probabilities={label: confidence},
        )

    label, confidence, probabilities = model_predict(normalized)
    return CommandResponse(
        input=input_text,
        commandLabel=label,
        rawPredictedLabel=label,
        source="model",
        confidence=confidence,
        requiresConfirmation=label in CONFIRMATION_LABELS,
        probabilities=probabilities,
    )
