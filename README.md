# ✈️ LLMService – AI Agent Backend for Airline Operations

This project is a backend service designed to power an AI Agent that understands user messages related to airline operations such as searching flights, buying tickets, and checking in. It utilizes the **Claude 3** large language model (LLM) to extract structured intents and parameters from natural language input, and routes requests to appropriate REST APIs.

---

## 📌 Features

- Natural language processing using **Claude 3 (Opus)** via Anthropic API
- Intent detection and parameter extraction from user messages
- Dispatching to midterm project APIs:
  - `SearchFlights`
  - `BuyTicket`
  - `CheckIn`
- Built-in routing, error handling, and support for bearer token authentication
- Clean, modular design for easy integration into full-stack applications

---

## 🛠️ Technologies Used

- .NET 8 / ASP.NET Core Web API
- Claude API (Anthropic)
- RESTful communication with external APIs
- JSON serialization with `System.Text.Json`
- Swagger (for development/testing)
- CORS-enabled for frontend integration

---

## 🧠 Intent Format

The Claude prompt returns a JSON object with the following structure:

```json
{
  "intent": "BuyTicket",
  "parameters": {
    "flightNumber": "TK123",
    "date": "2024-05-20T10:00:00Z",
    "passengerNames": ["Gulce Dogruoz", "Barış Ceyhan"]
  }
}
```
---

### ✅ Supported Intents & Parameters

| Intent        | Parameters                                                                 |
|---------------|----------------------------------------------------------------------------|
| SearchFlights | dateFrom, dateTo, airportFrom, airportTo, numberOfPeople, isRoundTrip      |
| BuyTicket     | flightNumber, date, passengerNames                                         |
| CheckIn       | flightNumber, date, passengerName                                          |


---

## 📁 Project Structure

| File                | Description                                                              |
|---------------------|--------------------------------------------------------------------------|
| ClaudeService.cs    | Core service for communicating with Claude API and dispatching to APIs   |
| AiAgentController.cs| HTTP API controller to handle incoming user messages                    |
| LLMResult.cs        | DTO class for storing intent, parameters, and API response              |
| UserMessageDto.cs   | Simple DTO for capturing user input message                             |
| Program.cs          | App startup file for dependency injection and configuration             |

---

## 🔄 Example Flow

1. User sends message:  
   `"I want to book a flight from Istanbul to Berlin on June 5 for 2 people."`

2. `ClaudeService` sends message to Claude with a structured prompt

3. Claude returns intent and parameters:

```json
{
  "intent": "SearchFlights",
  "parameters": {
    "airportFrom": "Istanbul",
    "airportTo": "Berlin",
    "dateFrom": "2025-06-05",
    "dateTo": "2025-06-05",
    "numberOfPeople": 2
  }
}
```

4. `ClaudeService` dispatches this request to the API Gateway

5. The backend response is returned to the frontend in real time


---

## ⚙️ Configuration

Add your Claude API Key and JWT token to `appsettings.json` or environment variables:

```json
"Claude": {
  "ApiKey": "<your-anthropic-api-key>",
  "JwtToken": "<your-jwt-token-for-airline-api>"
}
```

---

## 📹 Demo Video

[▶️ Watch Demo Video](https://youtu.be/j5mACjZvCzA)


---

## 👨‍💻 Authors

Developed by **Gulce DOGRUOZ** as part of the SE4458 – AI Agent Assignment  




