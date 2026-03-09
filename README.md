
⸻

Doctor Power – Docker Setup Guide

Requirements
	•	Install Docker Desktop
	•	Ensure Docker is running

⸻

## 1. Create Environment File

    In the project root folder, create a file named:

    .env

    Add the following:
        AZURE_OPENAI_ENDPOINT=your endpoint
        AZURE_OPENAI_API_KEY=your key

    Replace your_openai_api_key_here with your actual OpenAI API key.

⸻

## 2. Start the Application

    From the project root:

        docker compose up --build

    Wait until you see:

    Application started
    Now listening on...


⸻

    Access the Application

    Frontend:

        http://localhost:5173

    Backend API:

        http://localhost:5280


⸻

    To Stop the Application

    Press:

        CTRL + C

    Then optionally:

        docker compose down


⸻

If you want to run it locally, 

Add .env to the root directory

Add the following:
        AZURE_OPENAI_ENDPOINT=your endpoint
        AZURE_OPENAI_API_KEY=your key

Frontend: 
cd frontend/doctor-power/src
npm run dev

Backend:
cd backend/src/backend.Api
dotnet watch


the output should be in backend/src/backend.Infrastructure/FileStorages

Here is the order of storing output: 
UploadedFile -> store uploaded document 
PPCliJobs -> temporarily store output from pac cli
ParsedOutput -> store output being parsed
RAGOutput -> store fina output 


