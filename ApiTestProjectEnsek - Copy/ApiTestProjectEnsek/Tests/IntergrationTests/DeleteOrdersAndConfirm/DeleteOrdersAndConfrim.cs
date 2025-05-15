using Newtonsoft.Json;

namespace ApiTestProjectEnsek.Tests.IntergrationTests.DeleteOrdersAndConfirm
{
    internal class DeleteOrdersAndConfrim : ApiController
    {
        private readonly string ordersUrl = $"{ISharedResources.BaseUrl}{ISharedResources.OrdersUrlExtension}";
        public new string? authToken;

        private async Task<List<string>> GetOrderIds()
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(ordersUrl + "/:orderId");
            Assert.That(response.IsSuccessStatusCode, Is.True, "Request failed");

            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.That(!string.IsNullOrEmpty(responseBody), Is.True, "Response body should not be empty");

            var responseData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(responseBody);
            Assert.That(responseData, Is.Not.Null, "Response data should not be null");
            Assert.That(responseData.Count, Is.GreaterThan(0), "Response should contain orders");

            return responseData
                .Where(order => order.ContainsKey("id") && order["id"] != null)
                .Select(order => order["id"].ToString())
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList();
        }

        [Test]
        public async Task ExtractOrderIDs_Should_NotBeEmpty()
        {
            var orderIDs = await GetOrderIds();
            Assert.That(orderIDs.Count, Is.GreaterThan(0), "Extracted order IDs should not be empty");
        }

        [Test]
        public async Task StoreOrderIDsForDeletion()
        {
            var orderIDs = await GetOrderIds();
            Assert.That(orderIDs.Count, Is.GreaterThan(3), "Extracted order IDs should have at least 4 for selection");

            var orderIDsToDelete = orderIDs.Take(2).ToList();
            Environment.SetEnvironmentVariable("delete_order_1", orderIDsToDelete.ElementAtOrDefault(0) ?? "");
            Environment.SetEnvironmentVariable("delete_order_2", orderIDsToDelete.ElementAtOrDefault(1) ?? "");

            Assert.That(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("delete_order_1")), Is.True, "delete_order_1 should not be empty");
            Assert.That(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("delete_order_2")), Is.True, "delete_order_2 should not be empty");
        }

        [Test]
        public async Task StoreOrderIDsForEditing()
        {
            var orderIDs = await GetOrderIds();
            Assert.That(orderIDs.Count, Is.GreaterThan(3), "Extracted order IDs should have at least 4 for selection");

            var orderIDsToEdit = orderIDs.Skip(3).Take(2).ToList();
            Environment.SetEnvironmentVariable("edit_order_1", orderIDsToEdit.ElementAtOrDefault(0) ?? "");
            Environment.SetEnvironmentVariable("edit_order_2", orderIDsToEdit.ElementAtOrDefault(1) ?? "");

            Assert.That(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("edit_order_1")), Is.True, "edit_order_1 should not be empty");
            Assert.That(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("edit_order_2")), Is.True, "edit_order_2 should not be empty");
        }
    }
}