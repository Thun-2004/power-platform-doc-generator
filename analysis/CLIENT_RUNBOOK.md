# Client Runbook Replybrary Solution Analysis (Parser + RAG)

This document explains how to run the Replybrary analysis pipeline end-to-end without developer assistance.

The pipeline has two stages:
1. Deterministic parsing (no AI required)
2. Optional RAG/LLM generation (requires API key + vector store)

The system generates:
- Structured JSON chunks
- Markdown documentation
- Word (.docx)
- PDF
- Excel (.xlsx)
- Mermaid diagrams
- ERDs
- Live Q&A answers

---

## 0) Platform Notes
- Commands below are written for Mac/Linux (bash).
- On Windows, use PowerShell equivalents (environment variables use $env:VAR="value").
- All paths should be adjusted to your environment.
- If the solution was already unpacked using pac solution unpack, that is valid. The Canvas screen unpack step is still required for screen-level formula extraction.

---

## 1) One-Time Prerequisites

### 1.1 Install Required Tools
You must install:
- Power Platform CLI (pac)
- .NET SDK (same major version used by this repo)
- Pandoc (required for Word export)
- LaTeX engine (pdflatex) for PDF export

---

### 1.2 Verify Installation
Run:

```bash
pac --version
dotnet --version
pandoc --version
which pdflatex
pdflatex --version

Expected: each prints a valid version or path.

If any command fails, install the missing dependency before continuing.

⸻

2) Repository Sanity Check (Before Running Anything)

Confirm the repo builds successfully.

cd <REPO_ROOT>/test-AI
dotnet build

cd <REPO_ROOT>/test-AI-LLM
dotnet build

Expected:
	•	Both builds succeed
	•	No fatal errors

If build fails, resolve build issues before proceeding.

⸻

3) Inputs Required

You need:
	•	A Power Platform solution export folder, for example:

Replybrary_1_0_0_20/

Inside it:
	•	CanvasApps/*.msapp
	•	Workflows/*.json

You also need:
	•	A working folder for unpacking
	•	Optional: API key + vector store ID (for RAG stage)

⸻

4) Set Required Paths

Edit and run:

export REPO_ROOT="<PATH_TO_REPO>/sh38-main"
export SOLUTION_DIR="<PATH_TO_SOLUTION>/Replybrary_1_0_0_20"
export UNPACK_DIR="$HOME/pp_cli_jobs/replybrary_unpacked"
export REPORT_DIR="$HOME/Replybrary_reports"
export CHUNKS_DIR="$REPORT_DIR/chunks"
export RAG_OUT_DIR="$REPO_ROOT/test-AI-LLM/rag_outputs"


⸻

5) Clean Rerun (Optional but Recommended)

If re-running analysis:

rm -rf "$REPORT_DIR"
rm -rf "$RAG_OUT_DIR"
mkdir -p "$REPORT_DIR"
mkdir -p "$RAG_OUT_DIR"

This prevents stale output confusion.

⸻

6) Stage 1 — Unpack Canvas Apps (msapp → Screens)

mkdir -p "$UNPACK_DIR"
cd "$UNPACK_DIR"

rm -rf CanvasAppsSrc
mkdir -p CanvasAppsSrc

pac canvas unpack \
  --msapp "$SOLUTION_DIR/CanvasApps/<APP1>.msapp" \
  --sources CanvasAppsSrc

pac canvas unpack \
  --msapp "$SOLUTION_DIR/CanvasApps/<APP2>.msapp" \
  --sources CanvasAppsSrc"

Validate

find CanvasAppsSrc -name "*.fx.yaml" | head
find CanvasAppsSrc -name "*.fx.yaml" | wc -l

Expected:
	•	.fx.yaml files exist
	•	Count > 0

If 0 → msapp path incorrect or unpack failed.

⸻

7) Stage 2 — Copy Screens into Solution Folder

rm -rf "$SOLUTION_DIR/CanvasAppsSrc"
cp -R "$UNPACK_DIR/CanvasAppsSrc" "$SOLUTION_DIR/CanvasAppsSrc"
ls -la "$SOLUTION_DIR/CanvasAppsSrc" | head

Expected:
	•	CanvasAppsSrc/ exists inside solution directory.

⸻

8) Stage 3 — Run the Parser (Deterministic Stage)

cd "$REPO_ROOT/test-AI"
dotnet build

dotnet run -- \
  --input "$SOLUTION_DIR" \
  --out "$REPORT_DIR"


⸻

8.1 Expected Output Files

Inside:

$CHUNKS_DIR

You must see:
	•	workflows_detailed.json
	•	canvasapps_detailed.json
	•	envvars.json
	•	relationships.json

If any are missing, parser did not complete correctly.

⸻

8.2 Sanity Checks

grep -n "screen_to_workflow" "$CHUNKS_DIR/relationships.json" | head
grep -n "\"purpose\"" "$CHUNKS_DIR/workflows_detailed.json" | head

Expected:
	•	screen_to_workflow entries exist
	•	purpose entries exist

If not:
	•	CanvasAppsSrc likely not copied correctly
	•	Or workflows missing

⸻

9) Stage 4 — Local Output Generation (No API Required)

cd "$REPO_ROOT/test-AI-LLM"
rm -rf rag_outputs && mkdir rag_outputs

dotnet run -- generate workflows --chunks "$CHUNKS_DIR"
dotnet run -- generate screen-mapping --chunks "$CHUNKS_DIR"

Validate

cd rag_outputs
ls -la
head -n 20 screen_workflow_mapping.md
grep -n "Purpose" workflows.md | head

Expected:
	•	workflows.md
	•	screen_workflow_mapping.md

This confirms deterministic pipeline works end-to-end.

⸻

10) Stage 5 — RAG / LLM Generation (Optional)

This stage requires:
	•	API key
	•	Vector Store ID

⸻

10.1 Set API Key

If using OpenAI:

export OPENAI_API_KEY="<YOUR_KEY>"

If using Azure OpenAI, set the environment variables required by your deployment.

⸻

10.2 Vector Store Creation (One-Time Only)

The vector store is created from the chunk JSON files produced in Stage 3.

This is a one-time indexing step.
After creation, reuse the same <VECTOR_STORE_ID> for future runs.

Store the returned ID securely.

⸻

10.3 Run RAG Generation

cd "$REPO_ROOT/test-AI-LLM"

dotnet run -- generate overview --vs <VECTOR_STORE_ID>
dotnet run -- generate faq --vs <VECTOR_STORE_ID>
dotnet run -- generate diagrams --vs <VECTOR_STORE_ID>
dotnet run -- generate erd --vs <VECTOR_STORE_ID>

Expected:
	•	Overview output
	•	FAQ output
	•	Diagram files
	•	ERD files

All stored in rag_outputs/.

⸻

11) Stage 6  Export to Word / PDF / Excel

cd "$REPO_ROOT/test-AI-LLM"

dotnet run -- export word
dotnet run -- export pdf
dotnet run -- export excel --chunks "$CHUNKS_DIR"

Expected:
	•	.docx
	•	.pdf
	•	.xlsx

All in rag_outputs/.

⸻

12) Stage 7 — Live Q&A (Optional)

dotnet run -- ask \
  "Which screens trigger UploadLogo and what workflows do they call?" \
  --vs <VECTOR_STORE_ID>

Expected:
	•	Answer referencing screens + workflows.

⸻

13) Known Failure Modes

Issue	Likely Cause	Fix
pandoc: command not found	Pandoc not installed	Install Pandoc
pdflatex: command not found	LaTeX not installed	Install LaTeX
401/403 during RAG	Invalid/missing API key	Re-export key
No .fx.yaml files	Wrong msapp path	Verify CanvasApps path
Missing screen_to_workflow	Screens not copied	Re-run Stage 2
Old outputs remain	Didn’t clean folders	Run Clean Rerun step


⸻

14) Clean Rerun Procedure

If outputs look incorrect:

rm -rf "$REPORT_DIR"
rm -rf "$RAG_OUT_DIR"

Then re-run Stages 1–7 in order.

⸻

15) Support Boundary

If issues occur, provide:
	•	The exact command run
	•	The full terminal output
	•	The contents of $CHUNKS_DIR

This allows reproducible troubleshooting.

⸻

16) GitLab Placement

Best option:
	•	Add file:
analysis/Replybrary/CLIENT_RUNBOOK.md
	•	Commit to feature/AI
	•	Merge into main

This ensures anyone cloning the repository has access.

⸻

17) Quickstart (Add to Merge Request Description)

Full runbook:
analysis/Replybrary/CLIENT_RUNBOOK.md

Pipeline structure:
	1.	Deterministic parser (no API key required)
	2.	Optional RAG generation (requires VECTOR_STORE_ID + API key)

Local mode works fully without AI.


