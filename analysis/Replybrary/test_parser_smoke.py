import subprocess
from pathlib import Path

def main():
    project_dir = Path("/Users/daraling/replybrary_parser")
    input_dir = Path("/Users/daraling/Downloads/Replybrary_1_0_0_20")
    out_dir = Path("/Users/daraling/Downloads/Replybrary_reports_test")

    # Run parser
    result = subprocess.run(
        ["python3", str(project_dir / "parse_solution.py"),
         "--input", str(input_dir),
         "--out", str(out_dir)],
        capture_output=True,
        text=True
    )

    print("=== parser stdout ===")
    print(result.stdout)
    print("=== parser stderr ===")
    print(result.stderr)

    if result.returncode != 0:
        raise SystemExit(f"FAIL: parser exited with code {result.returncode}")

    # Check expected outputs exist
    must_exist = [
        out_dir / "solution_report.json",
        out_dir / "solution_summary.md",
        out_dir / "chunks" / "overview.json",
        out_dir / "chunks" / "canvasapps.json",
        out_dir / "chunks" / "envvars.json",
        out_dir / "chunks" / "workflows.json",
    ]

    missing = [str(p) for p in must_exist if not p.exists()]
    if missing:
        raise SystemExit("FAIL: missing expected files:\n" + "\n".join(missing))

    print("PASS: Parser ran and produced expected outputs.")
    print(f"Test output folder: {out_dir}")

if __name__ == "__main__":
    main()
