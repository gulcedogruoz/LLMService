using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class ClaudeService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _token;

    public ClaudeService(string apiKey, string jwtToken)
    {
        _apiKey = apiKey;
        _token = jwtToken;

        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://api.anthropic.com");
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<string> GetIntentFromMessage(string userMessage)
    {
        string prompt = @"
        You will receive a user message related to airline ticket operations (such as flight query, ticket purchase, check-in, registration, etc).

        Please extract the intent and available parameters and return ONLY a valid JSON object with the following format:
        {
        ""intent"": ""BuyTicket"",
        ""parameters"": {
            ""flightNumber"": ""TK123"",
            ""date"": ""2024-05-20T10:00:00Z"",
            ""passengerNames"": [""Ali Veli"", ""Ayşe Yılmaz""]
        }
        }

        Here are the expected parameters for common intents:

        - SearchFlights → dateFrom, dateTo, airportFrom, airportTo, numberOfPeople, isRoundTrip
        - BuyTicket → flightNumber, date, passengerNames
        - CheckIn → flightNumber, date, passengerName

        Only include parameters if you are confident the user mentioned them. Do NOT include placeholder or invented data.

        IMPORTANT:
        - If the intent is 'SearchFlights' and the user does not specify a return date, then use the same value for both 'dateFrom' and 'dateTo'.
        - This ensures that round-trip searches without a return date assume a same-day return.
        - If the user does not specify a year, assume the current year is 2025

        User message:
        ";

        var payload = new
        {
            model = "claude-3-opus-20240229",
            max_tokens = 512,
            temperature = 0.2,
            messages = new[]
            {
                new {
                    role = "user",
                    content = prompt + userMessage
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/v1/messages", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Claude API error: {response.StatusCode} - {error}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();


        // Console.WriteLine("Claude Raw Response:");
        // Console.WriteLine(responseContent);

        //  var message = JsonDocument.Parse(responseContent).RootElement
        //     .GetProperty("content")[0]
        //     .GetProperty("text").GetString();

        // var result = JsonSerializer.Deserialize<LLMResult>(message);
        var jsonDoc = JsonDocument.Parse(responseContent);
        var contentArray = jsonDoc.RootElement.GetProperty("content");
        var text = contentArray[0].GetProperty("text").GetString();
        var llmResult = JsonSerializer.Deserialize<LLMResult>(text, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new Exception("Failed to deserialize Claude result into LLMResult.");
        
        Console.WriteLine($"Parsed Intent: {llmResult.Intent}");
        Console.WriteLine($"Parsed Parameters: {JsonSerializer.Serialize(llmResult.Parameters)}");

        return await DispatchIntentAsync(llmResult);
    }

    private async Task<string> DispatchIntentAsync(LLMResult result)
    {
        var intent = result.Intent;
        var parameters = result.Parameters;

        using var client = new HttpClient();
        client.BaseAddress = new Uri("https://gd-airline-ticket-api.azurewebsites.net/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        if (intent == "SearchFlights")
        {
            var queryParams = new Dictionary<string, string?>
            {
                ["DateFrom"] = parameters["dateFrom"]?.ToString(),
                ["DateTo"] = parameters["dateTo"]?.ToString(),
                ["AirportFrom"] = parameters["airportFrom"]?.ToString(),
                ["AirportTo"] = parameters["airportTo"]?.ToString(),
                ["NumberOfPeople"] = parameters["numberOfPeople"]?.ToString(),
                ["IsRoundTrip"] = parameters["isRoundTrip"]?.ToString(),
                ["Page"] = "1"
            };

            var queryString = string.Join("&", queryParams
                .Where(p => !string.IsNullOrWhiteSpace(p.Value))
                .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}"));

            var url = $"api/v1/flights?{queryString}";
            var apiRes = await client.GetAsync(url);
            return await apiRes.Content.ReadAsStringAsync();
        }

        if (intent == "BuyTicket")
        {
            var postContent = new StringContent(
                JsonSerializer.Serialize(new
                {
                    flightNumber = parameters["flightNumber"],
                    date = parameters["date"],
                    passengerNames = parameters["passengerNames"]
                }),
                Encoding.UTF8,
                "application/json"
            );

            var apiRes = await client.PostAsync("api/v1/tickets/buy", postContent);
            return await apiRes.Content.ReadAsStringAsync();
        }

        if (intent == "CheckIn")
        {
            var postContent = new StringContent(
                JsonSerializer.Serialize(new
                {
                    flightNumber = parameters["flightNumber"],
                    date = parameters["date"],
                    passengerName = parameters["passengerName"]
                }),
                Encoding.UTF8,
                "application/json"
            );

            var apiRes = await client.PostAsync("api/v1/tickets/checkin", postContent);
            return await apiRes.Content.ReadAsStringAsync();
        }

        return "Intent not recognized by dispatcher.";
    }
}
