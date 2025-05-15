using Newtonsoft.Json;

namespace ApiTestProjectEnsek.Tests.IntergrationTests.Confirm_new_orders
{
    [TestFixture, Order(6)]
    public class OldOrderTests : ApiController
    {
        private readonly string name = ISharedResources.UserName;
        private readonly string ordersUrl = $"{ISharedResources.BaseUrl}{ISharedResources.OrdersUrlExtension}";
        public new string? authToken;

        private static readonly HashSet<string> ExistingOrderIDs = new HashSet<string>();


        private async Task<List<Dictionary<string, dynamic>>> GetOrders()
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(ordersUrl);
            Assert.That(response.IsSuccessStatusCode, Is.True, "Request failed.");

            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.That(!string.IsNullOrEmpty(responseBody), Is.True, "Response body should not be empty");

            var responseData = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(responseBody);
            Assert.That(responseData, Is.Not.Null, "Response data should not be null");
            Assert.That(responseData.Count, Is.GreaterThan(0), "Response should contain orders");

            return responseData;
        }

        [Test]
        public async Task ResponseShouldContainOrders()
        {
            var responseData = await GetOrders();
            Assert.That(responseData.Count, Is.GreaterThan(0), "Response should contain orders");
        }

        [Test]
        public async Task OrdersShouldHaveValidIds()
        {
            var responseData = await GetOrders();

            foreach (var order in responseData)
            {
                // Handle case variations in "id" field
                string orderId = order.ContainsKey("id") ? order["id"].ToString() :
                                 order.ContainsKey("Id") ? order["Id"].ToString() : null;

                Assert.That(orderId, Is.Not.Null, $"Order object does not contain 'id'. Full object: {JsonConvert.SerializeObject(order)}");
            }
        }

        [Test]
        public async Task NewOrdersShouldNotBeInExistingOrders()
        {
            var responseData = await GetOrders();

            foreach (var order in responseData)
            {
                string orderId = order.ContainsKey("id") ? order["id"].ToString() :
                                 order.ContainsKey("Id") ? order["Id"].ToString() : null;

                Assert.That(orderId, Is.Not.Null, $"Order object does not contain 'id'. Full object: {JsonConvert.SerializeObject(order)}");

                Environment.SetEnvironmentVariable($"existing_order_{orderId}", orderId);
                Assert.That(ExistingOrderIDs.Contains(orderId), Is.False, $"Order {orderId} should be a new order");
            }
        }

        [TearDown]
        public void tearDown()
        {
            this.Dispose();
        }

    }

}