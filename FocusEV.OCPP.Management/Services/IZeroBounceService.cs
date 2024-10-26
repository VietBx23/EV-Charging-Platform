using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public interface IZeroBounceService
{
    Task<string> VerifyEmailAsync(string email);
}

public class ZeroBounceService : IZeroBounceService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey = "5f00b99ff0c54e889c187e9604f725c6"; // Your API key

    public ZeroBounceService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<string> VerifyEmailAsync(string email)
    {
        var url = $"https://api.zerobounce.net/v2/validate?api_key={_apiKey}&email={email}";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var jsonResponse = JObject.Parse(responseBody);

        var status = jsonResponse["status"].ToString();
        var subStatus = jsonResponse["sub_status"].ToString();
        var account = jsonResponse["account"].ToString();
        var domain = jsonResponse["domain"].ToString();

        return $"Status: {status}, SubStatus: {subStatus}, Account: {account}, Domain: {domain}";
    }
}
