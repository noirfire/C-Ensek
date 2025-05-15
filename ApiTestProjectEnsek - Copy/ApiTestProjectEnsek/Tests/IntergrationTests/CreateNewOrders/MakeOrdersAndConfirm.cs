
namespace ApiTestProjectEnsek.Tests.IntergrationTests.CreateNewOrders
{
    [TestFixture, Order(7)]
    internal class MakeOrdersAndConfirm : ApiController
    {
        private readonly string ordersUrl = $"{ISharedResources.BaseUrl}{ISharedResources.OrdersUrlExtension}";
        public new string? authToken;
        private static string editOrder1;
        private static string editOrder2;

        public class Order
        {
            public string Id { get; set; } 
        }

        [SetUp]
        public async Task SetupAsync()
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync($"{ordersUrl}/getOrders");

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Assert.Fail($"Failed to fetch orders. Status Code: {response.StatusCode}, Response: {errorMessage}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Raw API Response: {responseBody}");

            var responseData = JsonSerializer.Deserialize<List<Order>>(responseBody);

            if (responseData == null || responseData.Count == 0)
            {
                Assert.Fail("No valid orders found in the API response.");
            }

            // Ensure order ID exists before assigning
            editOrder1 = responseData.FirstOrDefault()?.Id;
            editOrder2 = responseData.Skip(1).FirstOrDefault()?.Id;

            if (string.IsNullOrEmpty(editOrder1) || string.IsNullOrEmpty(editOrder2))
            {
                Assert.Fail("One or both order IDs are missing. Cannot proceed with tests.");
            }

            Console.WriteLine($"Retrieved Order IDs: {editOrder1}, {editOrder2}");
        }

        [Test, Order(1)]
        public async Task ResponseStatusCodeShouldBe200()
        {
            HttpClient client = new HttpClient();
            var orderIds = new[] { editOrder1, editOrder2 };

            foreach (var id in orderIds)
            {
                if (string.IsNullOrEmpty(id))
                {
                    Assert.Fail("Order ID cannot be empty.");
                    return;
                }

                var requestUrl = $"{ordersUrl}/{id}?quantity=0&energy_id=0";

                var response = await client.GetAsync(requestUrl);
                Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode, "Expected status code 200");
            }
        }

        [Test, Order(2)]
        public async Task ResponseShouldHaveValidJsonContentType()
        {
            HttpClient client = new HttpClient();
            Console.WriteLine($"Environment Variable Check: edit_order_1 = {Environment.GetEnvironmentVariable("edit_order_1")}");
            Console.WriteLine($"Environment Variable Check: edit_order_2 = {Environment.GetEnvironmentVariable("edit_order_2")}");

            var response = await client.GetAsync(ordersUrl);
            Assert.IsTrue(response.Content.Headers.ContentType?.MediaType.Contains("application/json"), "Response should be JSON");
        }

        [Test, Order(3)]
        public async Task UpdateOrders_Should_Return_200()
        {
            HttpClient client = new HttpClient();
            var orderIds = new[] { editOrder1, editOrder2 };

            foreach (var orderId in orderIds)
            {
                if (string.IsNullOrEmpty(orderId))
                {
                    Console.Error.WriteLine($"Missing order ID: {orderId}");
                    continue; // Prevents premature test failure while logging the issue
                }

                var requestUrl = $"{ordersUrl}/{orderId}?quantity=2&energy_id=2";
                var request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
                request.Headers.Add("Accept", "application/json");

                var response = await client.SendAsync(request);
                Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode, $"Order {orderId} should be successfully updated");

                var responseBody = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseBody);

                Assert.IsTrue(responseData.TryGetProperty("message", out var message), "Response should confirm update");
                Assert.IsTrue(message.ToString().Contains("updated"), "Update confirmation message should be present");
            }
        }

        [Test, Order(1)]
        public async Task OrdersShouldBeRetrievable()
        {
            var orderIds = new[] { editOrder1, editOrder2 };

            foreach (var orderId in orderIds)
            {
                Assert.IsFalse(string.IsNullOrEmpty(orderId), $"Order ID {orderId} should not be null or empty");

                var orderData = await GetOrder(orderId);
                Assert.IsTrue(orderData.TryGetProperty("quantity", out _), $"Order {orderId} should contain 'quantity'");
                Assert.IsTrue(orderData.TryGetProperty("energy_id", out _), $"Order {orderId} should contain 'energy_id'");
            }
        }

        [Test, Order(2)]
        public async Task OrdersShouldBeUpdatedSuccessfully()
        {
            HttpClient client = new HttpClient();
            var orderIds = new[] { editOrder1, editOrder2 };

            foreach (var orderId in orderIds)
            {
                Assert.IsFalse(string.IsNullOrEmpty(orderId), $"Order ID {orderId} should not be null or empty");

                var updateRequestUrl = $"{ordersUrl}/{orderId}?quantity=7&energy_id=3";
                var updateRequest = new HttpRequestMessage(HttpMethod.Put, updateRequestUrl);
                updateRequest.Headers.Add("Accept", "application/json");

                var response = await client.SendAsync(updateRequest);
                Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode, $"Order {orderId} should be successfully updated");
            }
        }

        [Test, Order(3)]
        public async Task OrdersShouldReflectUpdatedValues()
        {
            var orderIds = new[] { editOrder1, editOrder2 };

            foreach (var orderId in orderIds)
            {
                Assert.IsFalse(string.IsNullOrEmpty(orderId), $"Order ID {orderId} should not be null or empty");

                var updatedOrderData = await GetOrder(orderId);

                int updatedQuantity = updatedOrderData.GetProperty("quantity").GetInt32();
                int updatedEnergyId = updatedOrderData.GetProperty("energy_id").GetInt32();

                Assert.AreEqual(7, updatedQuantity, $"Order {orderId} should have updated quantity = 7");
                Assert.AreEqual(3, updatedEnergyId, $"Order {orderId} should have updated energy_id = 3");
            }
        }

        private async Task<JsonElement> GetOrder(string orderId)
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync($"{ordersUrl}/{orderId}");
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode, $"Order {orderId} should be retrievable");

            string responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonElement>(responseBody);
        }

        [TearDown]
        public void tearDown()
        {
            this.Dispose();
        }
    }
}

