using Foody.Tests.DTOs;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;

namespace Foody
{
    public class FoodyTests
    {
        private RestClient client;
        private static string foodId;

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = GetJwtToken("tester", "tester123");
            RestClientOptions options = new RestClientOptions("http://144.91.123.158:81")
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };
            this.client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            RestClient client = new RestClient("http://144.91.123.158:81");
            RestRequest request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { username, password });
            RestResponse response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                string token = null;
                if (content.TryGetProperty("accessToken", out var accessTokenProp))
                {
                    token = accessTokenProp.GetString();
                }
                else if (content.TryGetProperty("token", out var tokenProp))
                {
                    token = tokenProp.GetString();
                }

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve token");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException("Failed to authenticate user");
            }
        }

        [Order(1)]
        [Test]
        public void CreateFood_WithReuiredFields_ShouldBeSuccessful()
        {
            FoodDTO food = new FoodDTO
            {
                Name = "Soup",
                Description = "Chicken soup",
                Url = ""
            };

            RestRequest request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);
            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            foodId = readyResponse.FoodId;
        }

        [Order(2)]
        [Test]
        public void EditFoodTitle_ShouldChangeTitle()
        {
            RestRequest request = new RestRequest($"/api/Food/Edit/{foodId}", Method.Patch);
            request.AddBody(new[]
            {
                new
                {
                    path = "/name",
                    op = "replace",
                    value = "New Soup"
                }
            });
            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(readyResponse.Msg, Is.EqualTo("Successfully edited"));
        }

        [Order(3)]
        [Test]
        public void GetAllFoods_ShouldReturnNonEmptyArray()
        {
            RestRequest request = new RestRequest("/api/Food/All", Method.Get);
            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            List<FoodDTO> readyResponse = JsonSerializer.Deserialize<List<FoodDTO>>(response.Content);
            Assert.That(readyResponse, Is.Not.Empty);
            Assert.That(readyResponse, Is.Not.Null);
            Assert.That(readyResponse.Count, Is.GreaterThanOrEqualTo(1));
        }

        [Order(4)]
        [Test]
        public void DeleteExistingFood_ShouldSuccees()
        {
            RestRequest request = new RestRequest($"/api/Food/Delete/{foodId}", Method.Delete);
            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(readyResponse.Msg, Is.EqualTo("Deleted successfully!"));
        }

        [Order(5)]
        [Test]
        public void CreateFoodWithoutRequiredFields_ShouldFail()
        {
            FoodDTO food = new FoodDTO
            {
                Name = " ",
                Description = " ",
                Url = ""
            };

            RestRequest request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);
            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Order(6)]
        [Test]
        public void EditTitleOfNonExistingFood_ShouldFail()
        {
            string nonExistingFoodId = "123456";
            RestRequest request = new RestRequest($"/api/Food/Edit/{nonExistingFoodId}", Method.Patch);
            request.AddBody(new[]
            {
                new
                {
                    path = "/name",
                    op = "replace",
                    value = "New Soup"
                }
            });
            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(readyResponse.Msg, Is.EqualTo("No food revues..."));
        }

        [Order(7)]
        [Test]
        public void DeleteNonExistingFood_ShouldFail()
        {
            string nonExistingFoodId = "123456";
            RestRequest request = new RestRequest($"/api/Food/Delete/{nonExistingFoodId}", Method.Delete);
            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(readyResponse.Msg, Is.EqualTo("Unable to delete this food revue!"));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}
