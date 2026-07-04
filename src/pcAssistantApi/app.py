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
except ImportError:
    joblib = None

try:
    from sklearn.exceptions import InconsistentVersionWarning
except ImportError:
    InconsistentVersionWarning = None


SUPPORTED_LABELS = {
    "filesystem.create_folder",
    "filesystem.delete_folder",
    "filesystem.open_folder",
    "filesystem.get_file_details",
    "system.restart",
    "system.cancel_restart",
    "unknown",
}

CONFIRMATION_LABELS = {
    "filesystem.delete_folder",
    "system.restart",
}

LOW_CONFIDENCE_THRESHOLD = 0.60

DANGEROUS_PATTERNS = (
    r"\bfactory\s+reset\b",
    r"\bformat\b",
    r"\bwipe\b",
    r"\bdelete\s+(everything|all|system32|windows|program files|c drive|root)\b",
    r"\bremove\s+(everything|all|system32|windows|program files|c drive|root)\b",
    r"\berase\s+(everything|all|system32|windows|program files|c drive|root)\b",
    r"\bsystem32\b",
    r"\bc:\\windows\b",
    r"\bc:\\program files\b",
    r"\bc:\\program files \(x86\)\b",
    r"\bc:\\\s*$",
    r"\broot drive\b",
    r"\bclear entire disk\b",
    r"\bdestroy all data\b",
)

MODEL_PATH = Path(__file__).resolve().parent / "model" / "pc_assistant_command_classifier_best.joblib"


class CommandRequest(BaseModel):
    text: str = Field(min_length=1, max_length=2000)


class CommandResponse(BaseModel):
    input: str
    normalizedText: str
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
    supportedLabels: list[str]
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


app = FastAPI(
    title="PC Assistant Command Classifier",
    version="2.0.0",
    lifespan=lifespan,
)


def normalize_text(text: str) -> str:
    """
    IMPORTANT:
    Keep this normalization close to your Colab training script.
    If training normalization and runtime normalization are different,
    prediction accuracy will drop.
    """

    value = str(text).strip().lower()

    replacements = {
        "pls": "please",
        "plz": "please",
        "pc": "computer",
        "re-start": "restart",
        "re boot": "reboot",
        "dir": "directory",
        "directories": "directory",
        "folders": "folder",
        "files": "file",
        "details": "detail",
        "properties": "property",
        "infos": "info",
    }

    for old, new in replacements.items():
        value = re.sub(rf"\b{re.escape(old)}\b", new, value)

    # Normalize Windows absolute paths.
    value = re.sub(
        r"[a-zA-Z]:\\[^\s]+(?:\\[^\s]+)*",
        " <windows_path> ",
        value,
    )

    # Normalize environment variable paths.
    value = re.sub(
        r"%userprofile%\\[^\s]+",
        " <user_path> ",
        value,
        flags=re.IGNORECASE,
    )

    # Normalize file extensions but keep file-extension signal.
    value = re.sub(
        r"\.(txt|pdf|docx|xlsx|pptx|csv|json|xml|log|png|jpg|jpeg|mp4|mp3|wav|cs|py|js|ts|html|css|sql|zip|rar|md|yml|yaml)\b",
        r" <file_ext_\1> ",
        value,
    )

    # Normalize numbers.
    value = re.sub(r"\b\d+\b", " <number> ", value)

    value = re.sub(r"\s+", " ", value).strip()
    return value


def is_dangerous(text: str) -> bool:
    value = text.lower()
    return any(re.search(pattern, value) for pattern in DANGEROUS_PATTERNS)


def contains_windows_path(text: str) -> bool:
    value = text.lower()

    return bool(
        re.search(r"[a-zA-Z]:\\", value)
        or re.search(r"%userprofile%\\", value, flags=re.IGNORECASE)
    )


def rule_override(text: str) -> tuple[str, float] | None:
    """
    Rule layer for obvious commands.

    This improves accuracy for direct commands and protects dangerous requests.
    The ML model is still used when the rule layer is not confident.
    """

    value = text.lower()

    if is_dangerous(value):
        return "unknown", 1.0

    # Cancel restart/reboot/shutdown.
    if re.search(r"\b(cancel|abort|stop)\b.*\b(restart|reboot|shutdown)\b", value):
        return "system.cancel_restart", 0.98

    # File details / file properties / file metadata.
    file_detail_words = (
        "detail",
        "details",
        "property",
        "properties",
        "metadata",
        "information",
        "info",
        "size",
        "modified",
        "created",
        "creation",
        "extension",
        "format",
        "type",
        "path",
        "location",
        "attribute",
        "attributes",
        "permission",
        "permissions",
        "read only",
        "last updated",
        "last write",
    )

    file_words = (
        "file",
        "document",
        "pdf",
        "docx",
        "xlsx",
        "csv",
        "json",
        "image",
        "photo",
        "video",
        "audio",
        "script",
        "log",
        ".txt",
        ".pdf",
        ".docx",
        ".xlsx",
        ".pptx",
        ".csv",
        ".json",
        ".xml",
        ".log",
        ".png",
        ".jpg",
        ".jpeg",
        ".mp4",
        ".mp3",
        ".cs",
        ".py",
        ".js",
        ".ts",
        ".html",
        ".css",
        ".sql",
        ".zip",
        ".md",
    )

    if any(word in value for word in file_detail_words) and any(word in value for word in file_words):
        return "filesystem.get_file_details", 0.97

    # Restart/reboot.
    if re.search(r"\b(restart|reboot|soft reset)\b", value):
        return "system.restart", 0.95

    # Treat reset as restart only when it clearly means soft reset/reboot.
    if "reset" in value:
        reset_targets = (
            "my computer",
            "the computer",
            "my laptop",
            "the laptop",
            "this system",
            "the system",
            "windows",
            "this machine",
            "my device",
            "this device",
        )

        if any(target in value for target in reset_targets):
            return "system.restart", 0.95

    # Delete folder.
    if re.search(r"\b(delete|remove|erase|trash)\b.*\b(folder|directory|dir)\b", value):
        return "filesystem.delete_folder", 0.96

    if re.search(r"\b(recycle bin|get rid of|clean up)\b.*\b(folder|directory|dir)\b", value):
        return "filesystem.delete_folder", 0.96

    # Create folder.
    if re.search(r"\b(create|make|add|generate|prepare|initialize)\b.*\b(folder|directory|dir)\b", value):
        return "filesystem.create_folder", 0.96

    if re.search(r"\b(new folder|new directory)\b", value):
        return "filesystem.create_folder", 0.96

    # Open folder.
    open_folder_words = (
        "open",
        "show",
        "view",
        "browse",
        "navigate",
        "go to",
        "launch",
        "access",
        "bring up",
        "pull up",
        "open explorer",
        "open file explorer",
        "show contents",
        "display",
    )

    folder_words = (
        "folder",
        "directory",
        "dir",
    )

    if any(word in value for word in open_folder_words) and any(word in value for word in folder_words):
        return "filesystem.open_folder", 0.96

    # Example: "go to D:\Work\CSharp Practice"
    if any(word in value for word in open_folder_words) and contains_windows_path(value):
        return "filesystem.open_folder", 0.95

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

    except Exception as exc:
        model_state.model = None
        model_state.metadata = {}
        model_state.warnings = []
        model_state.error = f"Failed to load model: {exc}"


def model_predict(normalized_text: str) -> tuple[str, str, float, dict[str, float]]:
    if model_state.model is None:
        raise HTTPException(
            status_code=503,
            detail=model_state.error or "Model is not loaded.",
        )

    model = model_state.model

    if not hasattr(model, "predict"):
        raise HTTPException(
            status_code=503,
            detail="Model payload does not contain a predictor.",
        )

    raw_label = str(model.predict([normalized_text])[0])
    probabilities: dict[str, float] = {}
    confidence = 1.0

    if hasattr(model, "predict_proba"):
        proba = model.predict_proba([normalized_text])[0]
        classes = [str(label) for label in getattr(model, "classes_", [])]

        probabilities = {
            label: round(float(probability), 4)
            for label, probability in zip(classes, proba)
        }

        confidence = round(float(max(proba)), 4) if len(proba) else 0.0

    if raw_label not in SUPPORTED_LABELS:
        command_label = "unknown"
    elif confidence < LOW_CONFIDENCE_THRESHOLD:
        command_label = "unknown"
    else:
        command_label = raw_label

    return command_label, raw_label, confidence, probabilities


@app.get("/health", response_model=HealthResponse)
async def health() -> HealthResponse:
    return HealthResponse(
        status="ok" if model_state.loaded else "degraded",
        modelPath=str(MODEL_PATH),
        modelLoaded=model_state.loaded,
        supportedLabels=sorted(SUPPORTED_LABELS),
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
            normalizedText=normalized,
            commandLabel=label,
            rawPredictedLabel=label,
            source="rule",
            confidence=confidence,
            requiresConfirmation=label in CONFIRMATION_LABELS,
            probabilities={label: confidence},
        )

    label, raw_label, confidence, probabilities = model_predict(normalized)

    return CommandResponse(
        input=input_text,
        normalizedText=normalized,
        commandLabel=label,
        rawPredictedLabel=raw_label,
        source="model",
        confidence=confidence,
        requiresConfirmation=label in CONFIRMATION_LABELS,
        probabilities=probabilities,
    )