# Replybrary AI Documentation Generator

## What this is

This tool automatically generates clear, up-to-date documentation and diagrams for the Replybrary Power Platform solution.

Instead of manually writing docs, it:
- Reads the parsed solution output (JSON chunks)
- Understands workflows, canvas apps, and environment variables
- Generates:
  - Markdown documentation (overview, workflows, FAQ)
  - Architecture diagrams (Mermaid)
  - Optional Word / PDF exports

Everything is generated **only** from the parsed solution files.  
There is no guessing and no external knowledge.

---

## Important: required workflow (read this first)

**Before any AI / RAG functionality works, the solution MUST be parsed locally.**

Each user must:
1. Run the Power Platform parser on their own machine
2. Generate the JSON chunk outputs

Without this step, there is nothing for the AI to read, index, or generate from.

**Flow:**
Solution files → Parser → JSON chunks → Vector store → AI outputs

---

## Setup

### Prerequisites
- .NET 8+
- OpenAI API key
- Parsed solution output (JSON chunks)

### Environment variable
Set your API key locally:
```bash
export OPENAI_API_KEY="your_key_here"

## First-time indexing (one-time cost)

This uploads the parsed JSON chunks and creates embeddings.

- (Optional) pandoc for Word / PDF exports

 dotnet run -- index --chunks "<path_to_chunks_folder>"

Save the vector store ID that is printed.
This is reused for all future queries and generation.

Generating documentation

All of the following reuse the same vector store and are low cost.

dotnet run -- generate overview
dotnet run -- generate workflows
dotnet run -- generate faq
dotnet run -- generate diagrams

Outputs are written to:

/rag_outputs

Diagrams

The generate diagrams command outputs Mermaid code (.mmd).

To view visually:
	•	Copy the Mermaid code
	•	Paste it into https://mermaid.live or Mermaid-enabled tools
	•	Export PNG / SVG if needed



Exports (optional)

Requires pandoc:

brew install pandoc

Then:

dotnet run -- export word
dotnet run -- export pdf

Asking Questions 
dotnet run -- ask "How many workflows are in this solution?"

Answers are based only on the uploaded solution files.

Team usage & safety
	•	The API key is never committed to git
	•	Each user sets their own OPENAI_API_KEY locally
	•	Team members do not have access to anyone else’s API usage or spend
	•	Vector store IDs can be shared, but cost is tied to the API key used to query it

To make this a shared team project, each member will need:
	•	Their own API key
	•	Their own local environment variable set

export OPENAI_API_KEY="your_key_here"