
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

        OPENAI_API_KEY=your_openai_api_key_here

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
