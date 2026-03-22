## Backend appsettings.json Guide

This guide explains how to update values in `backend/src/backend.Api/appsettings.json` and what to do after changing them.

### 1) Edit the config file

Open:

- `backend/src/backend.Api/appsettings.json`

Main sections (easy summary):

- `FileStorage`: where uploaded/parsed/generated files are stored.
- `Frontend`: UI settings such as prompt character limit and available output types.
- `Shared`: values shared with frontend/backend (URLs, CORS, allowed upload types, AI models list).
- `Backend`: backend behavior (file cleanup TTL, prompt file paths, timeout).
- `Llm`: LLM defaults, provider base URLs, env var names for keys.

### 2) Common changes

- **Change API/frontend URLs**
  - Update `Shared.FrontendUrl`, `Shared.BackendUrl`, `Shared.CorsOrigins`.
- **Change output types shown in UI**
  - Update `Frontend.GeneratedOutputTypes`.
- **Change prompt limit in UI**
  - Update `Frontend.customPromptCharacterLimit`.
- **Change file retention time**
  - Update `Backend.FileStorePeriodInMinutes`.
- **Change model list shown in UI**
  - Update `Shared.AIModels`.
- **Change LLM provider endpoints or key env var names**
  - Update `Llm.LLMUrls` and `Llm.LLMKeys`.

### 3) After you change values

Use this checklist:

1. Save `appsettings.json`.
2. Restart backend (`dotnet watch` or container) to ensure all config-dependent services reload.
3. Refresh frontend page.
4. If running Docker, rebuild/restart containers:
   - `docker compose up --build`

### 4) Quick rules (important)

- If you changed `Shared.AIModels`, always restart backend, then refresh frontend.
- If you changed `Llm.LLMUrls` / `Llm.LLMKeys`, restart backend.
- If you changed `Frontend.*` values, refresh frontend (backend restart is still recommended for consistency).
- If a key points to an environment variable (for example `AZURE_OPENAI_API_KEY`), make sure that env var exists in your runtime (`.env`/container/local shell).

### 5) Verify your change

After restart:

- Open frontend and check dropdowns/output types reflect your updates.
- Try generating one output file to confirm backend can read new config.
- If model health looks wrong, recheck `Shared.AIModels` names and provider URL/key config.