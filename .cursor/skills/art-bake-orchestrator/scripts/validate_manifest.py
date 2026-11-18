"""Validate a bake_manifest.yaml for voting-sim art orchestration.

Run from repo root (or set REPO_ROOT). Does not require Blender.
  python validate_manifest.py path/to/bake_manifest.yaml
"""

from __future__ import annotations

import sys
from pathlib import Path

try:
    import yaml
except ImportError:
    yaml = None

REQUIRED_TOP = ("source_blend", "unity_fbx", "materials")
MODES = ("tile", "atlas", "flat")
EXPORT_MODES = ("auto", "always", "never")


def _load(path: Path) -> dict:
    text = path.read_text(encoding="utf-8")
    if yaml is not None:
        data = yaml.safe_load(text)
    else:
        # Minimal fallback: require PyYAML for real use
        raise SystemExit(
            "PyYAML is required: pip install pyyaml  (or validate via agent reading the file)"
        )
    if not isinstance(data, dict):
        raise SystemExit("Manifest root must be a mapping")
    return data


def validate(data: dict) -> list[str]:
    errors: list[str] = []
    for key in REQUIRED_TOP:
        if key not in data:
            errors.append(f"missing top-level field: {key}")

    export = data.get("export_fbx", "auto")
    if export not in EXPORT_MODES:
        errors.append(f"export_fbx must be one of {EXPORT_MODES}, got {export!r}")

    mats = data.get("materials")
    if not isinstance(mats, list) or not mats:
        errors.append("materials must be a non-empty list")
        return errors

    for i, entry in enumerate(mats):
        prefix = f"materials[{i}]"
        if not isinstance(entry, dict):
            errors.append(f"{prefix}: must be a mapping")
            continue
        for req in ("assembly", "mode", "unity_mat"):
            if req not in entry:
                errors.append(f"{prefix}: missing {req}")
        mode = entry.get("mode")
        if mode not in MODES:
            errors.append(f"{prefix}: mode must be one of {MODES}")
            continue
        if mode == "tile":
            if "texture" not in entry:
                errors.append(f"{prefix}: tile requires texture")
            if "density" not in entry:
                errors.append(f"{prefix}: tile requires density")
            size = entry.get("size_m")
            if not isinstance(size, (list, tuple)) or len(size) != 2:
                errors.append(f"{prefix}: tile requires size_m: [w, h]")
        elif mode == "atlas":
            if "texture" not in entry:
                errors.append(f"{prefix}: atlas requires texture")
            if "object" not in entry:
                errors.append(f"{prefix}: atlas requires object")
    return errors


def main(argv: list[str]) -> int:
    if len(argv) < 2:
        print("Usage: validate_manifest.py <bake_manifest.yaml>")
        return 2
    path = Path(argv[1])
    if not path.is_file():
        print(f"ERROR: file not found: {path}")
        return 2
    data = _load(path)
    errors = validate(data)
    if errors:
        print("INVALID")
        for e in errors:
            print(f" - {e}")
        return 1
    print("OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
