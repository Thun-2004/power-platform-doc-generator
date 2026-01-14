import argparse
import json
import os
from pathlib import Path
from collections import defaultdict

def safe_listdir(p: Path):
    if not p.exists() or not p.is_dir():
        return []
    return sorted([x for x in p.iterdir()])

def group_canvas_apps(canvas_dir: Path):
    """
    In your screenshots, CanvasApps contains items like:
      wmreply_replybrary_b320d_Background
      wmreply_replybrary_b320d_Document
      wmreply_replybrary_b320d_Additional
    We group them into logical apps by removing the final suffix (_Background/_Document/_Additional).
    """
    items = safe_listdir(canvas_dir)
    groups = defaultdict(list)

    for item in items:
        name = item.name
        # Try to strip known suffixes
        for suffix in ["_Background", "_Document", "_Additional"]:
            if name.endswith(suffix):
                app_key = name[: -len(suffix)]
                groups[app_key].append(name)
                break
        else:
            # If it doesn't match the 3-part pattern, keep it as its own thing
            groups[name].append(name)

    
    grouped = {k: sorted(v) for k, v in sorted(groups.items(), key=lambda x: x[0].lower())}
    return grouped

def list_workflows(workflows_dir: Path):
    items = safe_listdir(workflows_dir)
    
    out = []
    for item in items:
        if item.is_file():
            out.append({"name": item.name, "bytes": item.stat().st_size})
        else:
            
            out.append({"name": item.name + "/", "bytes": None})
    return out

def list_env_vars(env_dir: Path):
    items = safe_listdir(env_dir)
    out = []
    for item in items:
        if item.is_file():
            out.append({"name": item.name, "bytes": item.stat().st_size})
        else:
            out.append({"name": item.name + "/", "bytes": None})
    return out

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

def main():
    parser = argparse.ArgumentParser(description="Basic Power Platform solution folder parser (Replybrary-friendly).")
    parser.add_argument("--input", required=True, help="Path to extracted/unpacked solution folder")
    parser.add_argument("--out", required=True, help="Output folder for reports")
    args = parser.parse_args()

    root = Path(args.input).expanduser().resolve()
    out_dir = Path(args.out).expanduser().resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    # Folders
    canvas_dir = root / "CanvasApps"
    workflows_dir = root / "Workflows"
    env_dir = root / "environmentvariabledefinitions"

    report = {
        "root": str(root),
        "top_level": top_level_inventory(root),
        "canvasapps": {
            "exists": canvas_dir.exists(),
            "groups": group_canvas_apps(canvas_dir) if canvas_dir.exists() else {},
        },
        "workflows": {
            "exists": workflows_dir.exists(),
            "items": list_workflows(workflows_dir) if workflows_dir.exists() else [],
        },
        "environmentvariabledefinitions": {
            "exists": env_dir.exists(),
            "items": list_env_vars(env_dir) if env_dir.exists() else [],
        },
    }

    # Summary stats 
    canvas_groups_count = len(report["canvasapps"]["groups"])
    workflows_count = len(report["workflows"]["items"])
    env_count = len(report["environmentvariabledefinitions"]["items"])

    # Write JSON
    (out_dir / "solution_report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")

    # Write a markdown summary
    md = []
    md.append(f"# Solution Parse Summary\n")
    md.append(f"**Root:** `{root}`\n")
    md.append(f"## Key counts\n")
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
    for wf in report["workflows"]["items"]:
        md.append(f"- {wf['name']} ({wf['bytes']} bytes)\n")

    md.append("\n## Environment Variable Definitions\n")
    for ev in report["environmentvariabledefinitions"]["items"]:
        md.append(f"- {ev['name']} ({ev['bytes']} bytes)\n")

    (out_dir / "solution_summary.md").write_text("".join(md), encoding="utf-8")

    print("Parsing is complete")
    print(f"Canvas Apps (grouped): {canvas_groups_count}")
    print(f"Workflows: {workflows_count}")
    print(f"Environment variables: {env_count}")
    print(f"Reports written to: {out_dir}")

if __name__ == "__main__":
    main()
