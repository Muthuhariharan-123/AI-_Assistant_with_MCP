# AI Personal Assistant

A minimal, dockerized AI assistant demo that wires up:

**Flutter (web) → ASP.NET Core API → Gemini LLM → MCP → Calculator tool**

## Architecture

```
┌─────────────────────┐      HTTP       ┌──────────────────────┐
│  Flutter Web (nginx) │ ──────────────▶│  ASP.NET Core API    │
│  container: frontend │                │  container: backend  │
│  port: 8080          │                │  port: 5000          │
└─────────────────────┘                └──────────┬───────────┘
                                                   │
                                        ┌──────────▼───────────┐
                                        │    Gemini API        │
                                        │    (Google AI)       │
                                        └──────────┬───────────┘
                                                   │ tool call
                                        ┌──────────▼───────────┐
                                        │    MCP Server        │
                                        │  container: mcp      │
                                        │  tool: calculate()   │
                                        │  port: 3001          │
                                        └──────────────────────┘
```

## Quick Start

### Prerequisites
- Docker & Docker Compose
- A [Gemini API key](https://aistudio.google.com/apikey) (free tier)

### Run

```bash
# 1. Set your Gemini API key
export GEMINI_API_KEY=your-api-key-here

# 2. Start all containers
docker compose up --build

# 3. Open the app
# → http://localhost:8080
```

### Try it out
- Ask: **"What is 42 × 17?"** → The AI uses the calculator tool and responds
- Ask: **"Hello! What can you do?"** → Plain AI response (no tool used)

## Project Structure

```
AI/
├── frontend/           # Flutter Web (Dart + Riverpod)
│   ├── lib/
│   │   ├── core/
│   │   │   └── api_client.dart
│   │   ├── features/chat/
│   │   │   ├── chat_message.dart
│   │   │   ├── chat_provider.dart
│   │   │   └── chat_screen.dart
│   │   └── main.dart
│   └── Dockerfile
│
├── Backend/            # ASP.NET Core Minimal API
│   ├── Services/
│   │   ├── ChatService.cs
│   │   ├── GeminiClient.cs
│   │   └── McpClient.cs
│   ├── Models/
│   │   ├── ChatRequest.cs
│   │   └── ChatResponse.cs
│   ├── Program.cs
│   └── Dockerfile
│
├── mcp-server/         # C# MCP Calculator Server
│   ├── Services/
│   │   └── Calculator.cs
│   ├── Models/
│   │   ├── ToolRequest.cs
│   │   └── ToolResponse.cs
│   ├── Program.cs
│   └── Dockerfile
│
├── docker-compose.yml
└── README.md
```

## API

### POST /api/chat
```json
// Request
{ "message": "what is 12 * 7?" }

// Response
{ "reply": "12 × 7 = 84", "toolUsed": "calculate" }
```

### MCP Server

```json
// POST /tools/list → returns available tools
// POST /tools/call
{ "name": "calculate", "arguments": { "expression": "12 * 7" } }
// → { "result": 84.0, "error": null }
```

## Security

- API key stored only in environment variables (never committed)
- Input validation on all endpoints (message length, expression characters)
- Safe math expression evaluator (no `eval()` or shell execution)
- CORS restricted to frontend origin
- Rate limiting on `/api/chat` (20 req/min)
- Security headers on all responses
- `.env` files excluded from git

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Frontend | Flutter Web, Dart, Riverpod |
| Backend | ASP.NET Core 10, C# |
| MCP Server | ASP.NET Core 10, C# |
| LLM | Google Gemini 2.0 Flash (free tier) |
| Containers | Docker, Docker Compose |
| Frontend Server | nginx |

## License

MIT
