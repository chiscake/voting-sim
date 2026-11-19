"""Decide whether FBX export is required for a bake manifest.

Run from repo root:
  python check_fbx_gate.py path/to/bake_manifest.yaml

Prints: NEED_EXPORT or SKIP_EXPORT (and reasons). Exit 0 always on successful read;
exit 2 on usage/load errors. Agents treat NEED_EXPORT as a hard cue to run export_fbx.py.
"""

from __future__ import annotations

import sys
from pathlib import Path

try:
    import yaml
except ImportError:
    yaml = None


def _load(path: Path) -> dict:
    text = path.read_text(encoding="utf-8")
    if yaml is None:
        raise SystemExit("PyYAML is required: pip install pyyaml")
    data = yaml.safe_load(text)
    if not isinstance(data, dict):
        raise SystemExit("Manifest root must be a mapping")
    return data


def decide(repo: Path, data: dict) -> tuple[str, str]:
    mode = data.get("export_fbx", "auto")
    fbx_rel = data.get("unity_fbx")
    blend_rel = data.get("source_blend")
    if not fbx_rel or not blend_rel:
        return "NEED_EXPORT", "missing unity_fbx or source_blend"

    fbx = repo / Path(str(fbx_rel).replace("\\", "/"))
    blend = repo / Path(str(blend_rel).replace("\\", "/"))

    if mode == "never":
        return "SKIP_EXPORT", "export_fbx=never"
    if mode == "always":
        return "NEED_EXPORT", "export_fbx=always"
    # auto
    if not fbx.is_file():
        return "NEED_EXPORT", f"missing FBX: {fbx_rel}"
    if not blend.is_file():
        return "NEED_EXPORT", f"blend missing (cannot compare mtime): {blend_rel}"
    if blend.stat().st_mtime > fbx.stat().st_mtime:
        return "NEED_EXPORT", "blend newer than FBX"
    return "SKIP_EXPORT", "FBX up to date vs blend"


def _find_repo(start: Path) -> Path:
    cur = start.resolve()
    for _ in range(8):
        if (cur / "Assets").is_dir() and (cur / "Art").is_dir():
            return cur
        if (cur / "Assets").is_dir() and (cur / "ProjectSettings").is_dir():
            return cur
        if cur.parent == cur:
            break
        cur = cur.parent
    return start.resolve()


def main(argv: list[str]) -> int:
    if len(argv) < 2:
        print("Usage: check_fbx_gate.py <bake_manifest.yaml>")
        return 2
    path = Path(argv[1])
    if not path.is_file():
        print(f"ERROR: file not found: {path}")
        return 2
    data = _load(path)
    repo = _find_repo(Path.cwd())
    decision, reason = decide(repo, data)
    print(f"{decision}\trepo={repo}\t{reason}")
    print(f"unity_fbx={data.get('unity_fbx')}")
    print(f"source_blend={data.get('source_blend')}")
    print(f"export_fbx={data.get('export_fbx', 'auto')}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
