using System.Net.Http.Headers;

namespace ApiTestProjectEnsek.Tests.IntergrationTests
{
    [TestFixture, Order(2)]
    public class Api_Reset_Tests : ApiController
    {
        private readonly string name = ISharedResources.UserName;
        private readonly string password = ISharedResources.Password;
        private readonly string resetUrl = $"{ISharedResources.BaseUrl}{ISharedResources.ResetUrlExtension}";
        public string authToken;
        private readonly Api_Login_Tests loginTests = new Api_Login_Tests();

        public async Task IsTokenNull()
        {
            if (string.IsNullOrEmpty(authToken))
            {
                // If the token is null, call the login test to get a new token

                await loginTests.AuthorizationTokenShouldBeSaved();
                authToken = loginTests.AuthToken;
            }
            else
            {
                Console.WriteLine($"Token is already set: {authToken}");
            }
        }

        [Test, Order(5)]
        public async Task StatusCodeShouldBe200()
        {
            HttpClient client = new HttpClient();
            await IsTokenNull();

            var request = new HttpRequestMessage(HttpMethod.Post, resetUrl)
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", authToken) },
                Content = new StringContent(JsonSerializer.Serialize(new { username = name, password = password }), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request);
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode, "Expected status code 200");

            var responseData = JsonSerializer.Deserialize<AuthResponse>(await response.Content.ReadAsStringAsync());
            Assert.NotNull(responseData);
            authToken = responseData.AccessToken;

            Console.WriteLine($"Testing with token: {authToken}");
        }

        [Test, Order(6)]
        public async Task ResponseMessageShouldBeSuccess()
        {
            HttpClient client = new HttpClient();
            // Ensure the token is set before making the request  
            await IsTokenNull();

            var request = new HttpRequestMessage(HttpMethod.Post, resetUrl)
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", authToken) },
                Content = new StringContent(JsonSerializer.Serialize(new { username = name, password = password }), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request);
            var responseData = JsonSerializer.Deserialize<AuthResponse>(await response.Content.ReadAsStringAsync());

            Assert.AreEqual("Success", responseData.Message, "Message should be 'Success'");
        }

        [TearDown]
        public void tearDown()
        {
            this.Dispose();
        }
    }
}