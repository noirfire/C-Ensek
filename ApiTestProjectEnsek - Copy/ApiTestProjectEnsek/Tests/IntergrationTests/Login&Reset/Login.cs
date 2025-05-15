
namespace ApiTestProjectEnsek.Tests.IntergrationTests
{
    [TestFixture, Order(1)]
    public class Api_Login_Tests : ApiController
    {
        private readonly string name = ISharedResources.UserName;
        private readonly string password = ISharedResources.Password;
        private string loginUrl = $"{ISharedResources.BaseUrl}{ISharedResources.LoginUrlExtension}";
        private string? authToken;

        public string? AuthToken => authToken;

        [Test, Order(1)]
        public async Task ResponseTimeShouldBeWithinAcceptableRange()
        {
            HttpClient client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, loginUrl);
            request.Content = new StringContent(JsonSerializer.Serialize(new { username = name, password }), Encoding.UTF8, "application/json");

            var startTime = DateTime.UtcNow;
            var response = await client.SendAsync(request);
            var elapsed = DateTime.UtcNow - startTime;

            Assert.Less(elapsed.TotalMilliseconds, 200, "Response time should be below 200ms");
        }

        [Test, Order(2)]
        public async Task StatusCodeShouldBe200()
        {
            HttpClient client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, loginUrl);
            request.Content = new StringContent(JsonSerializer.Serialize(new { username = name, password }), Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request); // No more CS1061 or CS8602
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode, "Expected status code 200");
        }

        [Test, Order(3)]
        public async Task AuthorizationTokenShouldBeSaved()
        {
            HttpClient client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, loginUrl);
            request.Content = new StringContent(JsonSerializer.Serialize(new { username = name, password }), Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request); // No more CS1061 or CS8602
            var responseData = JsonSerializer.Deserialize<AuthResponse>(await response.Content.ReadAsStringAsync());

            Assert.NotNull(responseData);
            Assert.IsFalse(string.IsNullOrEmpty(responseData.AccessToken), "Authorization token should be set");

            authToken = responseData.AccessToken;
            Console.WriteLine($"Testing with token: {authToken}");
        }

        [Test, Order(4)]
        public async Task ValidateTokenStructureAndClaims()
        {
            await AuthorizationTokenShouldBeSaved();

            var tokenParts = authToken.Split(".");
            Assert.AreEqual(3, tokenParts.Length, "Token should follow JWT format");

            var tokenHeader = JsonSerializer.Deserialize<JwtHeader>(Base64Decode(tokenParts[0]));
            Assert.AreEqual("HS256", tokenHeader.Alg, "JWT signing algorithm should be HS256");

            var tokenPayload = JsonSerializer.Deserialize<JwtPayload>(Base64Decode(tokenParts[1]));
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Assert.Greater(tokenPayload.Exp, currentTime, "Token should not be expired");
        }

        private static string Base64Decode(string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }

        [TearDown]
        public void tearDown()
        {
            this.Dispose();
        }
    }
}