
### Doctor Power – Docker & Local Setup

---

### Requirements

- **Docker Desktop** installed  
- **Docker** running and available on your PATH  
- A valid **LLM** endpoint and API key

---

### 1. Create your `.env` file (project root)

In the project root folder (same level as `docker-compose.yml`), create a file named `.env` and add:

```bash
AZURE_OPENAI_API_KEY=your-azure-openai-key
```

- **`AZURE_OPENAI_API_KEY`**: The API key for that Azure OpenAI resource  
- the name must match the one you set in backend/src/backend.Api/appsettings.json, LLMKeys

> Do **not** commit the `.env` file to source control.

---

### 2. Start the application with Docker

From the project root:

```bash
docker compose up --build
```

Wait until you see log lines like:

```text
Application started
Now listening on...
```

Then open:

- **Frontend**: `http://localhost:5173`  
- **Backend API**: `http://localhost:5280`

To stop the application:

- Press `CTRL + C` in the terminal running `docker compose up`
- Optionally clean up containers and networks:

```bash
docker compose down
```

---

### 3. Run the application locally (without Docker)

If you prefer running each piece directly:

1. **Create `.env` in the project root** (same as above):

   ```bash
   AZURE_OPENAI_API_KEY=your-azure-openai-key
   ```

2. **Start the frontend**

   ```bash
   cd frontend/doctor-power
   npm install
   npm run dev
   ```

   The frontend will usually run at `http://localhost:5173`.

3. **Start the backend**

   ```bash
   cd backend/src/backend.Api
   dotnet watch
   ```

   The backend API will usually run at `http://localhost:5280`.

---

### 4. Where outputs are stored (backend)

Generated files are written under the backend’s file storage root
(`backend/src/backend.Infrastructure/FileStorages`):

- **`UploadedFile`**: stores the original uploaded solution zip  
- **`PPCliJobs`**: temporarily stores output from `pac` CLI jobs  
- **`ParsedOutput`**: stores parsed/intermediate artifacts  
- **`RAGOutput`**: stores the final generated outputs

Processing order:

1. `UploadedFile` → original upload  
2. `PPCliJobs` → unpacked/temporary CLI artifacts  
3. `ParsedOutput` → structured parse results  
4. `RAGOutput` → final documents returned to the frontend

---

### 5. Editing backend settings

To adjust backend configuration (file paths, timeouts, model settings, etc.), see:

- `backend/README.md` – contains details on backend options and how to change them.
