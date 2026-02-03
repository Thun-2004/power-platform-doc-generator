# Replybrary – RAG Doc Generator (Vector Store + Ask/Generate CLI)

This tool indexes the parser “chunks” into an OpenAI Vector Store, then generates Markdown + Mermaid docs (overview, workflows, FAQ, diagrams, ERD, screen→workflow mapping) using **file_search** over those uploaded chunks.

---

## Prerequisites

- .NET SDK (same version as the RAG project)
- An OpenAI API key in an env var:

```bash
export OPENAI_API_KEY="YOUR_KEY"

	•	Parser output already generated (you need the chunks/ folder):

<OUTPUT_FOLDER>/chunks/
  overview.json
  canvasapps.json
  canvasapps_detailed.json
  workflows.json
  workflows_detailed.json
  envvars.json
  relationships.json
  erd_schema.json


⸻

Where to run commands

Run all dotnet run commands from the folder containing the RAG Program.cs / .csproj (the RAG project root).

⸻

Step 1 — Create a NEW Vector Store (one-time per chunk set)

This uploads every *.json file from your chunks folder.

dotnet run -- index \
  --chunks "<OUTPUT_FOLDER>/chunks" \
  --name "replybrary_chunks"

Output will include a vector store id like:

Vector store: vs_XXXXXXXXXXXXXXXXXXXXXXXXXXXX

Save it. You reuse it for ask/generate until chunks change.

When do I need a new vector store?

Create a new vector store only when the chunk JSON files changed (new parser run, new solution export, new relationships/screens, etc.).

⸻

Step 2 — Ask a question (uses existing vector store)

dotnet run -- ask "Which screens call UploadLogo?" \
  --vs "vs_XXXXXXXXXXXXXXXXXXXXXXXXXXXX"

More examples:

dotnet run -- ask "List all environment variables." --vs "vs_XXXXXXXXXXXXXXXXXXXXXXXXXXXX"
dotnet run -- ask "Show workflow to connector relationships." --vs "vs_XXXXXXXXXXXXXXXXXXXXXXXXXXXX"
dotnet run -- ask "What tables are in the ERD schema?" --vs "vs_XXXXXXXXXXXXXXXXXXXXXXXXXXXX"


⸻

Step 3 — Generate docs (writes files to an output folder)

Set an output folder (optional). Default is ./rag_outputs.

dotnet run -- generate overview \
  --vs "vs_XXXXXXXXXXXXXXXXXXXXXXXXXXXX" \
  --out "<RAG_OUTPUT_FOLDER>"

dotnet run -- generate workflows \
  --vs "vs_XXXXXXXXXXXXXXXXXXXXXXXXXXXX" \
  --out "<RAG_OUTPUT_FOLDER>"

dotnet run -- generate faq \
  --vs "vs_XXXXXXXXXXXXXXXXXXXXXXXXXXXX" \
  --out "<RAG_OUTPUT_FOLDER>"

dotnet run -- generate diagrams \
  --vs "vs_XXXXXXXXXXXXXXXXXXXXXXXXXXXX" \
  --out "<RAG_OUTPUT_FOLDER>"

dotnet run -- generate screen-mapping \
  --vs "vs_XXXXXXXXXXXXXXXXXXXXXXXXXXXX" \
  --out "<RAG_OUTPUT_FOLDER>"

dotnet run -- generate erd \
  --vs "vs_XXXXXXXXXXXXXXXXXXXXXXXXXXXX" \
  --out "<RAG_OUTPUT_FOLDER>"

Output files created

<RAG_OUTPUT_FOLDER>/
  overview.md
  workflows.md
  faq.md
  architecture.mmd
  screen_workflow_mapping.md
  erd.mmd


⸻

Step 4 — Export to Word/PDF (optional)

Requires pandoc installed.

# macOS
brew install pandoc

Export:

dotnet run -- export word --out "<RAG_OUTPUT_FOLDER>"
dotnet run -- export pdf  --out "<RAG_OUTPUT_FOLDER>"


⸻

Demo Script (copy/paste)

Use this in order:

# 1) Index (only if you do NOT already have a vector store for this chunk set)
dotnet run -- index --chunks "<OUTPUT_FOLDER>/chunks" --name "replybrary_chunks"

# 2) Ask (quick proof it can retrieve)
dotnet run -- ask "How many screens and relationships are present?" --vs "vs_XXXXXXXXXXXXXXXXXXXXXXXXXXXX"

# 3) Generate everything
dotnet run -- generate overview       --vs "vs_XXXXXXXXXXXXXXXXXXXXXXXXXXXX"
dotnet run -- generate workflows      --vs "vs_XXXXXXXXXXXXXXXXXXXXXXXXXXXX"
dotnet run -- generate faq            --vs "vs_XXXXXXXXXXXXXXXXXXXXXXXXXXXX"
dotnet run -- generate diagrams       --vs "vs_XXXXXXXXXXXXXXXXXXXXXXXXXXXX"
dotnet run -- generate screen-mapping --vs "vs_XXXXXXXXXXXXXXXXXXXXXXXXXXXX"
dotnet run -- generate erd            --vs "vs_XXXXXXXXXXXXXXXXXXXXXXXXXXXX"


⸻

Common Issues

“Not found in uploaded files”
	•	The info isn’t in the chunks folder you indexed
	•	OR you indexed an older chunks folder
Fix: re-run index on the correct <OUTPUT_FOLDER>/chunks.

screen-mapping says not found
	•	There are zero type == "screen_to_workflow" edges in relationships.json
Fix: ensure CanvasAppsSrc exists before parsing so screen calls can be detected.

ERD is empty
	•	erd_schema.json has no tables/relationships
Fix: only possible if parser didn’t extract schema data, or the solution has no schema captured in that file.

⸻

What to commit to Git

 Commit:
	•	RAG project code (Program.cs, .csproj, etc.)
	•	README instructions
	•	(Optional) example output files if your team wants sample docs

 Do NOT commit:
	•	Your API key
	•	Generated vector store ids as “required” config
	•	Large client exports unless allowed by client policy

