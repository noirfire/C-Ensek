using Newtonsoft.Json;

namespace ApiTestProjectEnsek.Tests.IntergrationTests.Login_Reset.VerifyCurrentEnergy_Orders
{

    [TestFixture, Order(3)]
    public class EnergyApiTests : ApiController
    {
        private readonly string name = ISharedResources.UserName;
        private readonly string password = ISharedResources.Password;
        private readonly string energyUrl = $"{ISharedResources.BaseUrl}{ISharedResources.EnergyUrlExtension}";
        private readonly string ordersUrl = $"{ISharedResources.BaseUrl}{ISharedResources.OrdersUrlExtension}";
        public string authToken;

        [Test]
        public async Task CurrentEnergyFigures()
        {
            HttpClient client = new HttpClient();
            var jsonPayload = JsonConvert.SerializeObject(new
            {
                username = name,
                password = password,
            });

            var request = new HttpRequestMessage(HttpMethod.Get, energyUrl)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request);
            Assert.AreEqual(200, (int)response.StatusCode, "Response status code is not 200");

            string responseBody = await response.Content.ReadAsStringAsync();
            var responseData = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(responseBody);

            foreach (var energyType in responseData.Keys)
            {
                var energyInfo = responseData[energyType];

                Environment.SetEnvironmentVariable($"price_{energyType}", energyInfo["price_per_unit"].ToString());
                Environment.SetEnvironmentVariable($"unit_type_{energyType}", energyInfo["unit_type"].ToString());
                Environment.SetEnvironmentVariable($"quantity_{energyType}", energyInfo["quantity_of_units"].ToString());
            }

            Assert.IsNotNull(Environment.GetEnvironmentVariable("price_gas"), "Gas price should not be null");
            Assert.IsNotNull(Environment.GetEnvironmentVariable("price_electric"), "Electric price should not be null");
            Assert.IsNotNull(Environment.GetEnvironmentVariable("price_oil"), "Oil price should not be null");
            Assert.IsNotNull(Environment.GetEnvironmentVariable("price_nuclear"), "Nuclear price should not be null");

            Console.WriteLine("Stored environment variables:");
            Console.WriteLine($"Gas price: {Environment.GetEnvironmentVariable("price_gas")}");
            Console.WriteLine($"Electric price: {Environment.GetEnvironmentVariable("price_electric")}");
            Console.WriteLine($"Oil price: {Environment.GetEnvironmentVariable("price_oil")}");
            Console.WriteLine($"Nuclear price: {Environment.GetEnvironmentVariable("price_nuclear")}");

        }

        [Test]
        public async Task OrdersResponseStatusCodeIs200()
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(ordersUrl);
            Assert.AreEqual(200, (int)response.StatusCode, "Response status code is not 200");
        }

        [Test]
        public async Task ResponseHasRequiredFields()
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(ordersUrl);
            string responseBody = await response.Content.ReadAsStringAsync();
            var orders = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(responseBody);

            Assert.IsInstanceOf<List<Dictionary<string, dynamic>>>(orders, "Response should be an array");
            Assert.That(orders, Is.Not.Null, "Orders list should not be null");
            Assert.That(orders.Count, Is.GreaterThan(0), "Response should contain at least one order");

            foreach (var order in orders)
            {
                List<string> missingFields = new List<string>();

                if (!order.ContainsKey("fuel")) missingFields.Add("fuel");
                if (!order.ContainsKey("id") && !order.ContainsKey("Id")) missingFields.Add("Id");
                if (!order.ContainsKey("quantity")) missingFields.Add("quantity");
                if (!order.ContainsKey("time")) missingFields.Add("time");

                if (missingFields.Count > 0)
                {
                    Console.WriteLine($"Order missing fields: {string.Join(", ", missingFields)} - {JsonConvert.SerializeObject(order)}");
                    Assert.Fail($"Order is missing required fields: {string.Join(", ", missingFields)}");
                }
            }
        }

        [Test]
        public async Task QuantityShouldBeNonNegativeInteger()
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(ordersUrl);
            string responseBody = await response.Content.ReadAsStringAsync();
            var orders = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(responseBody);

            foreach (var order in orders)
            {
                Assert.IsTrue(order["quantity"] is int || order["quantity"] is long, "Quantity should be an integer type");
                Assert.GreaterOrEqual(Convert.ToInt64(order["quantity"]), 0, "Quantity should be a non-negative integer");
            }
        }

        [Test]
        public async Task TimeShouldBeValidDateFormat()
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(ordersUrl);
            string responseBody = await response.Content.ReadAsStringAsync();
            var orders = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(responseBody);

            foreach (var order in orders)
            {
                Assert.DoesNotThrow(() => DateTime.Parse(order["time"]));
            }
        }

        [Test]
        public async Task ContentTypeShouldBeJson()
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(ordersUrl);
            Assert.IsTrue(response.Content.Headers.ContentType.MediaType.Contains("application/json"));
        }

        [Test]
        public async Task ResponseShouldContainArrayOfOrders()
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(ordersUrl);
            string responseBody = await response.Content.ReadAsStringAsync();
            var orders = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(responseBody);

            Assert.IsInstanceOf<List<Dictionary<string, dynamic>>>(orders);
            Assert.Greater(orders.Count, 0, "Orders array should contain at least one order");
        }

        [Test]
        public async Task FuelTypesShouldBeValid()
        {
            HttpClient client = new HttpClient();
            var validFuels = new HashSet<string> { "electric", "gas", "nuclear", "oil" };
            var response = await client.GetAsync(ordersUrl);
            string responseBody = await response.Content.ReadAsStringAsync();
            var orders = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(responseBody);

            foreach (var order in orders)
            {
                if (!validFuels.Contains(order["fuel"].ToString()))
                    Console.WriteLine($"Unexpected fuel type: {order["fuel"]}");
                Assert.IsTrue(validFuels.Contains(order["fuel"].ToString()));
            }
        }

        [Test]
        public async Task StoreOrderIDsAsEnvironmentVariables()
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(ordersUrl);
            string responseBody = await response.Content.ReadAsStringAsync();

            var orders = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(responseBody);

            Assert.That(orders, Is.Not.Null, "Orders list should not be null");
            Assert.That(orders.Count, Is.GreaterThan(0), "Response should contain orders");

            var storedOrderCount = 0;

            foreach (var order in orders)
            {
                // ✅ Handles both "Id" and "id" key variations
                string orderId = order.ContainsKey("id") ? order["id"].ToString() :
                                 order.ContainsKey("Id") ? order["Id"].ToString() : null;

                if (!string.IsNullOrEmpty(orderId))
                {
                    Environment.SetEnvironmentVariable($"existing_order_{orderId}", orderId);
                    storedOrderCount++;
                }
                else
                {
                    Console.WriteLine($"Skipping order without valid 'id': {JsonConvert.SerializeObject(order)}");
                }
            }

            // ✅ Verifies that at least one order was stored
            Assert.That(storedOrderCount, Is.GreaterThan(0), "At least one order ID should be stored");
        }


        [TearDown]
        public void tearDown()
        {
            this.Dispose();
        }
    }

}

