using System.Net;
using System.Net.Http.Json;

namespace GenesysForge.Api.Tests;

public class ApiDocumentationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ApiDocumentationTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task VersionedApiAlias_ExposesExistingEndpoints()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("ok", body!["status"]);
        Assert.Equal("ok", body["database"]);
    }

    [Fact]
    public async Task OpenApiDocument_UsesVersionedPaths()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"/api/v1/health\"", json);
        Assert.DoesNotContain("\"/api/health\"", json);
    }

    [Fact]
    public async Task ScalarDocs_OpenSuccessfully()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/docs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("text/html", response.Content.Headers.ContentType?.MediaType ?? "");
    }
}
