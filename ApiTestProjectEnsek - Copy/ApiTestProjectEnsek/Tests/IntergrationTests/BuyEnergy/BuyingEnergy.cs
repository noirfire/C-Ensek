using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace ApiTestProjectEnsek.Tests.IntergrationTests.BuyEnergy
{
    [TestFixture, Order(4)]
    public class EnergyPurchaseTests : ApiController
    {
        private readonly string buyUrl = $"{ISharedResources.BaseUrl}{ISharedResources.BuyEnergyUrlExtension}";
        private const int energyId = 3;
        public string authToken;

        private static readonly Dictionary<string, (string energyType, int energyId, string name)> EnergyTypeMap = new()
            {
                { "kWh", ("kWh", 3, "Electric") },
                { "m³", ("m³", 1, "Gas") },
                { "MW", ("MW", 2, "Nuclear") },
                { "Litres", ("Litres", 4, "Oil") }
            };

        [Test]
        public async Task ResponseStatusCodeIs200()
        {
            HttpClient client = new HttpClient();
            var energyId = 3; // Example energy type ID
            var purchaseUrl = $"{buyUrl}/{energyId}/1";
            var request = new HttpRequestMessage(HttpMethod.Put, purchaseUrl);
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);
            Assert.AreEqual(200, (int)response.StatusCode, "Response status code is not 200");
        }

        [Test]
        public async Task ResponseTimeIsLessThan200ms()
        {
            HttpClient client = new HttpClient();
            var startTime = DateTime.UtcNow;
            var response = await client.GetAsync(buyUrl);
            var duration = DateTime.UtcNow - startTime;

            Assert.Less(duration.TotalMilliseconds, 200, "Response time exceeded 200ms");
        }

        [Test, TestCaseSource(nameof(GetEnergyTypeMap))]
        public async Task PurchaseEnergyTypes(string energyType, int energyId, string name)
        {
            HttpClient client = new HttpClient();
            var purchaseUrl = $"{buyUrl}/{energyId}/1";
            var request = new HttpRequestMessage(HttpMethod.Put, purchaseUrl);
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);
            Assert.AreEqual(200, (int)response.StatusCode, $"Failed to purchase {name}");

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseData = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(responseBody);

            Console.WriteLine($"Response message for {name}: {responseData["message"]}");

            Assert.IsTrue(responseData.ContainsKey("message"), $"Response for {name} should contain a purchase message");
        }


        [Test]
        public async Task ResponseShouldContainValidPurchaseMessage()
        {
            var responseData = await SendPurchaseRequest();
            Assert.IsTrue(responseData.TryGetProperty("message", out var message), "Response should contain a purchase confirmation message");
            Assert.IsFalse(string.IsNullOrEmpty(message.ToString()), "Message should not be empty");
        }

        [Test]
        public async Task ValidateExtractedEnergyTypeAndQuantity()
        {
            var responseData = await SendPurchaseRequest();
            string message = responseData.GetProperty("message").ToString();

            var match = Regex.Match(message, @"You have purchased (\d+) (\w+) at a cost");

            Assert.IsTrue(match.Success, "Failed to extract energy type and quantity from message");

            int purchasedQuantity = int.Parse(match.Groups[1].Value);
            string purchasedEnergyType = match.Groups[2].Value;

            Assert.Greater(purchasedQuantity, 0, "Purchased quantity should be a positive number");
        }

        [Test]
        public async Task ValidateEnergyTypeAgainstExpected()
        {
            var responseData = await SendPurchaseRequest();
            string message = responseData.GetProperty("message").ToString();

            var match = Regex.Match(message, @"You have purchased \d+ (\w+) at a cost");
            Assert.IsTrue(match.Success, "Failed to extract energy type from message");

            string purchasedEnergyType = match.Groups[1].Value;
            Assert.IsTrue(EnergyTypeMap.ContainsKey(purchasedEnergyType), $"Unexpected energy type: {purchasedEnergyType}");

            var expectedData = EnergyTypeMap[purchasedEnergyType];
            Assert.That(expectedData.energyId, Is.EqualTo(energyId), "Energy ID should match the requested fuel type");
            Assert.That(purchasedEnergyType, Is.EqualTo(expectedData.energyType), "Energy type should match the expected type");
        }

        [Test]
        public async Task ContentTypeShouldBeJson()
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(buyUrl);
            Assert.IsTrue(response.Content.Headers.ContentType.MediaType.Contains("application/json"), "Content-Type should be JSON");
        }

        [Test, TestCaseSource(nameof(GetEnergyTypeMap))]
        public async Task EachEnergyTypeShouldBeSuccessfullyPurchased(string _, int energyId, string name)
        {
            HttpClient client = new HttpClient();
            var purchaseUrl = $"{buyUrl}/{energyId}/1";
            var response = await client.PutAsync(purchaseUrl, null);
            var responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Raw Response Body for {name}: {responseBody}");
            Assert.IsFalse(string.IsNullOrEmpty(responseBody), "Response body should not be empty");

            var responseData = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(responseBody);
            Assert.IsNotNull(responseData, "Response data should not be null");

            Console.WriteLine($"Response structure for {name}: {JsonConvert.SerializeObject(responseData, Formatting.Indented)}");

            Assert.IsTrue(responseData.ContainsKey("message"), $"Purchase confirmation for {name} should exist");
            Console.WriteLine($"Purchase message: {responseData["message"]}");
        }

        private async Task<JsonElement> SendPurchaseRequest()
        {
            HttpClient client = new HttpClient();
            var purchaseUrl = $"{buyUrl}/{energyId}/1";
            var response = await client.PutAsync(purchaseUrl, null);
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode, "Expected status code 200");

            string responseBody = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(responseBody), "Response body should not be empty");

            return System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseBody);
        }

        public static IEnumerable<object[]> GetEnergyTypeMap()
        {
            yield return new object[] { EnergyTypeMap["kWh"].energyType, EnergyTypeMap["kWh"].energyId, EnergyTypeMap["kWh"].name };
            yield return new object[] { EnergyTypeMap["m³"].energyType, EnergyTypeMap["m³"].energyId, EnergyTypeMap["m³"].name };
            yield return new object[] { EnergyTypeMap["MW"].energyType, EnergyTypeMap["MW"].energyId, EnergyTypeMap["MW"].name };
            yield return new object[] { EnergyTypeMap["Litres"].energyType, EnergyTypeMap["Litres"].energyId, EnergyTypeMap["Litres"].name };
        }

        [TearDown]
        public void tearDown()
        {
            this.Dispose();
        }
    }
}
