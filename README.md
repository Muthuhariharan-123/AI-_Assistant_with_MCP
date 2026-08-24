# AI Assistant with MCP

A lightweight, dockerized AI personal assistant that can answer questions and perform mathematical calculations using the Model Context Protocol (MCP).

## Tech Stack
- **Frontend:** Flutter Web (Material 3)
- **Backend:** ASP.NET Core API
- **AI Model:** Google Gemini 3.6 Flash
- **Tools:** C# MCP Server (Calculator)

## How to Run

1. Get a free Gemini API key from [Google AI Studio](https://aistudio.google.com/apikey)
2. Create a `.env` file in the `Backend/` folder (or at the root) and add your key:
   ```env
   GEMINI_API_KEY=your_actual_api_key_here
   ```
3. Run with Docker Compose:
   ```bash
   docker compose up --build
   ```
4. Open **http://localhost:8080** in your browser.

## Features
- **Modern UI:** Dark mode Flutter interface with chat bubbles.
- **Smart Tooling:** The AI automatically detects math questions and routes them to the local MCP calculator server.
- **Secure:** API keys are injected via environment variables and never exposed to the frontend.
