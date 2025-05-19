using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class ClaudeService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public ClaudeService(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://api.anthropic.com");
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<LLMResult> GetIntentFromMessage(string userMessage)
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
            throw new Exception($"Claude API hatası: {response.StatusCode} - {error}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var message = JsonDocument.Parse(responseContent).RootElement
            .GetProperty("content")[0]
            .GetProperty("text").GetString();

        var result = JsonSerializer.Deserialize<LLMResult>(message);
        return result;
    }
}
