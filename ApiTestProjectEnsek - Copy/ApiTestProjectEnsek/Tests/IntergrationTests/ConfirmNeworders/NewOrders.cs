using Newtonsoft.Json;

namespace ApiTestProjectEnsek.Tests.IntergrationTests.Confirm_new_orders
{
    [TestFixture, Order(5)]
    public class OrderTests : ApiController
    {
        private readonly string name = ISharedResources.UserName;
        private readonly string ordersUrl = $"{ISharedResources.BaseUrl}{ISharedResources.OrdersUrlExtension}";
        public new string? authToken;
        private static readonly HashSet<string> ExistingOrderIDs = new HashSet<string>();
        private static string? editOrder1;
        private static string? editOrder2;

        [Test, Order(1)]
        public async Task ValidateNewOrdersAreNotInExistingOrders()
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(ordersUrl);
            var responseBody = await response.Content.ReadAsStringAsync();

            Assert.That(!string.IsNullOrEmpty(responseBody), Is.True, "Response body should not be empty");

            var responseData = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(responseBody);
            Assert.That(responseData, Is.Not.Null, "Response data should not be null");
            Assert.That(responseData.Count, Is.GreaterThan(0), "Response should contain orders");

            Console.WriteLine($"Raw Response: {JsonConvert.SerializeObject(responseData, Formatting.Indented)}");

            int assigned = 0;
            foreach (var order in responseData)
            {
                if (order.TryGetValue("Id",out var orderId) || order.TryGetValue("id", out orderId)) 
                {            
                    Environment.SetEnvironmentVariable($"existing_order_{orderId}", orderId);
                    Assert.That(ExistingOrderIDs.Contains(orderId), Is.False, $"Order {orderId} should be a new order");

                    // Assign to editOrder1 and editOrder2
                    if (assigned == 0) { editOrder1 = orderId; assigned++; }
                    else if (assigned == 1) { editOrder2 = orderId; assigned++; }
                }
                else
                {
                    Console.WriteLine($"Order object does not contain 'id' or 'Id'. Full object: {JsonConvert.SerializeObject(order)}");
                    Assert.Fail("Order dictionary does not contain a valid 'id' key.");
                }
            }
            Assert.IsFalse(string.IsNullOrEmpty(editOrder1), "editOrder1 should be assigned");
            Assert.IsFalse(string.IsNullOrEmpty(editOrder2), "editOrder2 should be assigned");
        }


        [Test, Order(2)]
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

        [Test, Order(3)]
        public async Task OrdersShouldBeUpdatedSuccessfully()
        {
            HttpClient client = new HttpClient();
            var orderIds = new[] { editOrder1, editOrder2 };

            foreach (var orderId in orderIds)
            {
                Assert.IsFalse(string.IsNullOrEmpty(orderId), $"Order ID {orderId} should not be null or empty");

                var updateRequestUrl = $"{ordersUrl}/{orderId}";
                var updateRequest = new HttpRequestMessage(HttpMethod.Get, updateRequestUrl);
                updateRequest.Headers.Add("Accept", "application/json");

                var response = await client.SendAsync(updateRequest);
                Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode, $"Order {orderId} should be successfully updated");
            }
        }

        [Test, Order(4)]
        public async Task OrdersShouldReflectUpdatedValues()
        {
            var orderIds = new[] { editOrder1, editOrder2 };

            foreach (var orderId in orderIds)
            {
                Assert.IsFalse(string.IsNullOrEmpty(orderId), $"Order ID {orderId} should not be null or empty");
                
                var updateRequestUrl = $"{ordersUrl}/{orderId}";
                var updatedOrderData = await GetOrder(updateRequestUrl);

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
            return System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseBody);
        }

        [TearDown]
        public void tearDown()
        {
            this.Dispose();
        }
    }
}
