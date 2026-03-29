using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FiresideEditorial.Services;

public interface INewsletterService
{
    Task<(bool Success, string Message)> SubscribeAsync(string email);
}

public class ButtondownNewsletterService : INewsletterService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<ButtondownNewsletterService> _logger;

    public ButtondownNewsletterService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<ButtondownNewsletterService> logger)
    {
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> SubscribeAsync(string email)
    {
        var apiKey = _config["Newsletter:ButtondownApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_BUTTONDOWN_API_KEY")
        {
            _logger.LogWarning("Buttondown API key not configured. Email {Email} logged only.", email);
            // Graceful fallback — still show success to user, log the email
            return (true, "Subscribed (pending API setup)");
        }

        try
        {
            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", apiKey);

            var payload = JsonSerializer.Serialize(new { email_address = email });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.buttondown.com/v1/subscribers", content);

            if (response.IsSuccessStatusCode)
                return (true, "Welcome to the hearthside!");

            var body = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Buttondown returned {Status}: {Body}", response.StatusCode, body);

            if (body.Contains("already", StringComparison.OrdinalIgnoreCase))
                return (true, "You're already part of the family!");

            return (false, "Something went wrong. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Newsletter subscription failed for {Email}", email);
            return (false, "Connection error. Please try again later.");
        }
    }
}
