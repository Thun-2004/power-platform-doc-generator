import argparse
import json
from pathlib import Path
from collections import defaultdict


# ----------------------------
# Helpers 
# ----------------------------

def is_ignored(name: str) -> bool:
    """Ignore macOS/hidden junk and common noise files."""
    return name.startswith(".") or name in {"Thumbs.db"}


def safe_listdir(p: Path):
    """List directory contents safely, filtering ignored items, sorted case-insensitively."""
    if not p.exists() or not p.is_dir():
        return []
    items = []
    for x in p.iterdir():
        if is_ignored(x.name):
            continue
        items.append(x)
    return sorted(items, key=lambda x: x.name.lower())


def find_dir_case_insensitive(root: Path, target_name: str) -> Path | None:
    """
    Find a directory directly under root, matching target_name case-insensitively.
    Returns Path or None.
    """
    target_lower = target_name.lower()
    for item in safe_listdir(root):
        if item.is_dir() and item.name.lower() == target_lower:
            return item
    return None


def top_level_inventory(root: Path):
    inv = []
    for item in safe_listdir(root):
        entry = {
            "name": item.name + ("/" if item.is_dir() else ""),
            "type": "dir" if item.is_dir() else "file",
        }
        if item.is_file():
            entry["bytes"] = item.stat().st_size
        inv.append(entry)
    return inv


def list_files(folder: Path, suffix: str | None = None):
    """List files (optionally by suffix) with sizes."""
    out = []
    for item in safe_listdir(folder):
        if not item.is_file():
            continue
        if suffix and item.suffix.lower() != suffix.lower():
            continue
        out.append({"name": item.name, "bytes": item.stat().st_size})
    return out


def list_dirs(folder: Path):
    """List immediate subdirectories (used for env vars where each env var is a folder)."""
    out = []
    for item in safe_listdir(folder):
        if item.is_dir():
            out.append({"name": item.name + "/"})
    return out


# ----------------------------
# Canvas Apps grouping
# ----------------------------

def group_canvas_apps(canvas_dir: Path):
    """
    In the Replybrary export, CanvasApps contains items like:
      wmreply_replybrary_b320d_BackgroundImageUri
      wmreply_replybrary_b320d_DocumentUri.msapp
      wmreply_replybrary_b320d_AdditionalUris0_identity.json

    We group by the "base" app name:
      wmreply_replybrary_b320d
      wmreply_replybraryv2_c933c
    """
    groups = defaultdict(list)

    
    known_suffixes = [
        "_BackgroundImageUri",
        "_DocumentUri.msapp",
        "_AdditionalUris0_identity.json",
    ]

    for item in safe_listdir(canvas_dir):
        name = item.name
        base = name

        for sfx in known_suffixes:
            if name.endswith(sfx):
                base = name[: -len(sfx)]
                break

        groups[base].append(name)

    # sort keys + values so output is stable
    return {k: sorted(v) for k, v in sorted(groups.items(), key=lambda x: x[0].lower())}


# ----------------------------
# Main
# ----------------------------

def main():
    parser = argparse.ArgumentParser(
        description="Power Platform solution folder parser (Replybrary-friendly)."
    )
    parser.add_argument("--input", required=True, help="Path to extracted/unpacked solution folder")
    parser.add_argument("--out", required=True, help="Output folder for reports")
    args = parser.parse_args()

    root = Path(args.input).expanduser().resolve()
    out_dir = Path(args.out).expanduser().resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    # Find key folders (case-insensitive to avoid 'Workflows' vs 'workflows' issues)
    canvas_dir = find_dir_case_insensitive(root, "CanvasApps")
    workflows_dir = find_dir_case_insensitive(root, "Workflows")
    env_dir = find_dir_case_insensitive(root, "environmentvariabledefinitions")

    report = {
        "root": str(root),
        "top_level": top_level_inventory(root),
        "canvasapps": {
            "exists": canvas_dir is not None,
            "groups": group_canvas_apps(canvas_dir) if canvas_dir else {},
        },
        "workflows": {
            "exists": workflows_dir is not None,
            # only JSON workflow files
            "items": list_files(workflows_dir, suffix=".json") if workflows_dir else [],
        },
        "environmentvariabledefinitions": {
            "exists": env_dir is not None,
            # env vars are folders in export (count folders, not junk files)
            "items": list_dirs(env_dir) if env_dir else [],
        },
    }

    # Summary counts
    canvas_groups_count = len(report["canvasapps"]["groups"])
    workflows_count = len(report["workflows"]["items"])
    env_count = len(report["environmentvariabledefinitions"]["items"])

    # Write JSON
    (out_dir / "solution_report.json").write_text(
        json.dumps(report, indent=2), encoding="utf-8"
    )

    # Write markdown summary
    md = []
    md.append("# Solution Parse Summary\n")
    md.append(f"**Root:** `{root}`\n")
    md.append("## Key counts\n")
    md.append(f"- Canvas Apps (grouped): **{canvas_groups_count}**\n")
    md.append(f"- Workflows: **{workflows_count}**\n")
    md.append(f"- Environment variables: **{env_count}**\n")

    md.append("\n## Canvas Apps (grouped)\n")
    if canvas_groups_count == 0:
        md.append("- None found (CanvasApps folder missing or empty)\n")
    else:
        for app, parts in report["canvasapps"]["groups"].items():
            md.append(f"- **{app}**\n")
            for part in parts:
                md.append(f"  - {part}\n")

    md.append("\n## Workflows\n")
    if workflows_count == 0:
        md.append("- None found (Workflows folder missing or empty)\n")
    else:
        for wf in report["workflows"]["items"]:
            md.append(f"- {wf['name']} ({wf['bytes']} bytes)\n")

    md.append("\n## Environment Variable Definitions\n")
    if env_count == 0:
        md.append("- None found (environmentvariabledefinitions missing or empty)\n")
    else:
        for ev in report["environmentvariabledefinitions"]["items"]:
            md.append(f"- {ev['name']}\n")

    (out_dir / "solution_summary.md").write_text("".join(md), encoding="utf-8")

    # ----------------------------
    # Chunk outputs for AI + UI
    # ----------------------------
    chunks_dir = out_dir / "chunks"
    chunks_dir.mkdir(parents=True, exist_ok=True)

    # overview chunk
    (chunks_dir / "overview.json").write_text(
        json.dumps(
            {
                "root": report["root"],
                "counts": {
                    "canvasapps_groups": canvas_groups_count,
                    "workflows": workflows_count,
                    "envvars": env_count,
                },
                "top_level": report["top_level"],
            },
            indent=2,
        ),
        encoding="utf-8",
    )

    # section chunks
    (chunks_dir / "canvasapps.json").write_text(
        json.dumps(report["canvasapps"], indent=2),
        encoding="utf-8",
    )

    (chunks_dir / "envvars.json").write_text(
        json.dumps(report["environmentvariabledefinitions"], indent=2),
        encoding="utf-8",
    )

    (chunks_dir / "workflows.json").write_text(
        json.dumps(report["workflows"], indent=2),
        encoding="utf-8",
    )

    # per-workflow chunk files (helps “regenerate just one flow section”)
    per_flow_dir = chunks_dir / "workflows"
    per_flow_dir.mkdir(parents=True, exist_ok=True)
    for wf in report["workflows"]["items"]:
        safe_name = wf["name"]
        (per_flow_dir / f"{safe_name}.json").write_text(
            json.dumps(wf, indent=2),
            encoding="utf-8",
        )

    print("Parsing is complete")
    print(f"Canvas Apps (grouped): {canvas_groups_count}")
    print(f"Workflows: {workflows_count}")
    print(f"Environment variables: {env_count}")
    print(f"Reports written to: {out_dir}")
    print(f"Chunks written to: {chunks_dir}")


if __name__ == "__main__":
    main()
