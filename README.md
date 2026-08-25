# AI Assistant with MCP

A lightweight, dockerized AI personal assistant that can answer questions and perform mathematical calculations using the Model Context Protocol (MCP).

## Tech Stack
- **Frontend:** Flutter Web (Material 3)
- **Backend:** ASP.NET Core API
- **AI Model:** Google Gemini 3.6 Flash
- **Tools:** C# MCP Server (Calculator)

## How to Run

1. Obtain your Gemini API key.
2. Set up your environment variables (e.g., create a `.env` file) with the required keys.
3. Run with Docker Compose:
   ```bash
   docker compose up --build
   ```
4. Open **http://localhost:8080** in your browser.

## Features
- **Modern UI:** Dark mode Flutter interface with chat bubbles.
- **Smart Tooling:** The AI automatically detects math questions and routes them to the local MCP calculator server.
- **Secure:** API keys are injected via environment variables and never exposed to the frontend.
