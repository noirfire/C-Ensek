
namespace ApiTestProjectEnsek.API
{
    public class ApiController : IDisposable
    {
        private readonly HttpClient client = new HttpClient();
        public string authToken;

        public class AuthResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }

            [JsonPropertyName("message")]
            public string Message { get; set; }
        }

        public class JwtHeader
        {
            [JsonPropertyName("alg")]
            public string Alg { get; set; }
        }

        public class JwtPayload
        {
            [JsonPropertyName("exp")]
            public long Exp { get; set; }
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}