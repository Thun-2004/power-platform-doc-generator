# Replybrary – Power Platform Solution Analysis & Parser (Task 6 Foundations)

This folder contains a structured reverse-engineering + automated parsing workflow for the client’s **Replybrary** Power Platform solution.  
It supports **Task 6** by producing repeatable, evidence-based outputs (JSON chunks + docs) from a real solution export.

---

## What this repo now does (current state)

We upgraded from a “manual summary + static diagram” approach to a **working parser pipeline** that:

- Unpacks the solution export (Power Platform CLI)
- Unpacks Canvas Apps into source (`.fx.yaml`) so screens and formulas can be inspected
- Parses the solution into structured outputs:
  - Canvas Apps (grouped + detailed)
  - Power Automate workflows (list + detailed)
  - Environment variables
  - Screens (from Canvas Apps source)
  - Relationship edges inferred from evidence:
    - `screen_to_workflow` (screen formula calls like `FlowName.Run(...)`)
    - `workflow_to_env` (env var usage)
    - `workflow_to_connector` (connector usage)


---

## Key outputs (what to show in Task 6 demo)

### Parser outputs (ground truth)
Generated into:

```text
/Users/daraling/Downloads/Replybrary_reports/
└─ chunks/
   ├─ overview.json
   ├─ canvasapps.json
   ├─ canvasapps_detailed.json
   ├─ workflows.json
   ├─ workflows_detailed.json
   ├─ envvars.json
   ├─ relationships.json
   └─ erd_schema.json

Example successful parse:
	•	Canvas Apps (grouped): 2
	•	Workflows: 10
	•	Environment variables: 16
	•	Screens found: ~39
	•	Relationship edges inferred: ~95

    ---

Prerequisites

- .NET SDK installed
- Power Platform CLI (`pac`) installed
- Power Platform solution ZIP
- Canvas Apps unpacked to source (required for screens)

Step 1 – Unpack Power Platform Solution

Run once per solution version.

After unpacking, your solution folder **must look like this**:

```text
Replybrary_1_0_0_20/
├─ CanvasApps/
│  ├─ *.msapp
├─ CanvasAppsSrc/                ← REQUIRED for screens
│  └─ <app_name>/
│     └─ Src/
│        └─ *.fx.yaml
├─ Workflows/
├─ environmentvariabledefinitions.xml
└─ other solution files

If CanvasAppsSrc is missing, screens = 0 and screen→workflow links will NOT be found.

## What this analysis includes

Step 1 — Unpack the solution (once)

pac solution unpack \
  --zipfile "<PATH_TO_SOLUTION_ZIP>" \
  --folder "<SOLUTION_FOLDER>" \
  --processCanvasApps


⸻

Step 2 — Unpack Canvas Apps to source (REQUIRED)

Run for each .msapp file:

pac canvas unpack \
  --msapp "<SOLUTION_FOLDER>/CanvasApps/<app>.msapp" \
  --sources "<SOLUTION_FOLDER>/CanvasAppsSrc/<app>"

Verify screens exist:

find "<SOLUTION_FOLDER>/CanvasAppsSrc" -type f -iname "*.fx.yaml" | head


⸻

Step 3 — Run the parser

From the folder containing NewSolution_Parser.csproj:

cd "<PARSER_PROJECT_PATH>"

dotnet run -- \
  --input "<SOLUTION_FOLDER>" \
  --out "<OUTPUT_FOLDER>"


⸻

Expected Output (Example)

Canvas Apps (grouped): 2
Workflows: 10
Environment variables: 16
Screens found: ~40
Relationship edges inferred: ~90+


⸻

Output Files

Generated under:

<OUTPUT_FOLDER>/chunks/
├─ overview.json
├─ canvasapps.json
├─ canvasapps_detailed.json
├─ workflows.json
├─ workflows_detailed.json
├─ envvars.json
├─ relationships.json
└─ erd_schema.json

These files are the authoritative parsed representation of the solution.

⸻

Common Issues
	•	Screens = 0
→ Canvas Apps not unpacked to source
	•	Few relationships
→ Screens or workflow source missing
	•	Parser won’t run
→ Command not run from folder containing .csproj

⸻

What the Parser Extracts
	•	Canvas Apps
	•	Power Automate workflows
	•	Environment variables
	•	Screens (*.fx.yaml)
	•	Screen → Workflow calls
	•	Workflow → Env var usage
	•	Workflow → Connector usage
	•	Optional ERD schema

---

### 2. `dependency-diagram.png`
A visual diagram showing all critical dependencies:

