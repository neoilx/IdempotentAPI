using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace IdempotentAPI.IntegrationTests;

/// <summary>
/// Tests for Content-Type header handling in cached responses.
/// See: https://github.com/ikyriak/IdempotentAPI/issues/78
///
/// When IdempotentAPI caches a response, it stores headers including Content-Type
/// with charset (e.g., "application/json; charset=utf-8"). When the cached response
/// is returned, this stored Content-Type can conflict with ASP.NET Core's content
/// negotiation, potentially causing 406 NotAcceptable errors.
/// </summary>
public class ContentTypeTests : IClassFixture<WebApi1ApplicationFactory>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;

    public ContentTypeTests(
        WebApi1ApplicationFactory api1ApplicationFactory,
        ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _httpClient = api1ApplicationFactory.CreateClient();
    }

    [Fact]
    public async Task CachedResponse_ShouldNotCause406_WhenContentTypeHasCharset()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("IdempotencyKey", idempotencyKey);
        // Request JSON specifically
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Act - First request (will be cached)
        var response1 = await _httpClient.PostAsync("v6/TestingIdempotentAPI/testobject", null);

        var contentType1 = response1.Content.Headers.ContentType?.ToString();
        _testOutputHelper.WriteLine($"Response 1 Content-Type: {contentType1}");
        _testOutputHelper.WriteLine($"Response 1 Status: {response1.StatusCode}");

        // Act - Second request (should return cached response)
        var response2 = await _httpClient.PostAsync("v6/TestingIdempotentAPI/testobject", null);

        var contentType2 = response2.Content.Headers.ContentType?.ToString();
        _testOutputHelper.WriteLine($"Response 2 Content-Type: {contentType2}");
        _testOutputHelper.WriteLine($"Response 2 Status: {response2.StatusCode}");

        // Assert - Both responses should succeed (not 406 NotAcceptable)
        response1.StatusCode.Should().Be(HttpStatusCode.OK,
            "First request should succeed");

        response2.StatusCode.Should().Be(HttpStatusCode.OK,
            "Cached response should not cause 406 NotAcceptable due to Content-Type charset conflict");

        // Both should have valid JSON content type
        contentType1.Should().Contain("application/json",
            "First response should be JSON");
        contentType2.Should().Contain("application/json",
            "Cached response should be JSON");
    }

    [Fact]
    public async Task CachedResponse_WithStrictAcceptHeader_ShouldNotCause406()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("IdempotencyKey", idempotencyKey);
        // Request JSON without charset - this is more strict
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        // Act - First request (will be cached with Content-Type: application/json; charset=utf-8)
        var response1 = await _httpClient.PostAsync("v6/TestingIdempotentAPI/testobject", null);

        var content1 = await response1.Content.ReadAsStringAsync();
        var contentType1 = response1.Content.Headers.ContentType?.ToString();
        _testOutputHelper.WriteLine($"Response 1 Content-Type: {contentType1}");
        _testOutputHelper.WriteLine($"Response 1 Status: {response1.StatusCode}");
        _testOutputHelper.WriteLine($"Response 1 Body: {content1}");

        // Act - Second request with same strict Accept header
        var response2 = await _httpClient.PostAsync("v6/TestingIdempotentAPI/testobject", null);

        var content2 = await response2.Content.ReadAsStringAsync();
        var contentType2 = response2.Content.Headers.ContentType?.ToString();
        _testOutputHelper.WriteLine($"Response 2 Content-Type: {contentType2}");
        _testOutputHelper.WriteLine($"Response 2 Status: {response2.StatusCode}");
        _testOutputHelper.WriteLine($"Response 2 Body: {content2}");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK,
            $"First request should succeed. Body: {content1}");

        // This is the key assertion - cached response should not fail due to Content-Type mismatch
        response2.StatusCode.Should().NotBe(HttpStatusCode.NotAcceptable,
            "Cached response should not return 406 NotAcceptable. " +
            "If this fails, it means the cached Content-Type header (with charset) " +
            "is conflicting with content negotiation.");

        response2.StatusCode.Should().Be(HttpStatusCode.OK,
            $"Cached response should succeed. Body: {content2}");

        // Content should be the same (cached)
        content1.Should().Be(content2, "Cached response should have same content");
    }

    /// <summary>
    /// This test verifies the fix for issue #78 by checking that Content-Type
    /// is NOT duplicated in the cached response headers.
    ///
    /// Before the fix: Content-Type was stored in cached headers, causing potential
    /// conflicts when the cached response was returned.
    ///
    /// After the fix: Content-Type is excluded from cached headers, letting ASP.NET
    /// Core's output formatters set it correctly.
    /// </summary>
    [Fact]
    public async Task CachedResponse_ShouldNotHaveDuplicateContentTypeHeaders()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("IdempotencyKey", idempotencyKey);

        // Act - First request (will be cached)
        var response1 = await _httpClient.PostAsync("v6/TestingIdempotentAPI/testobject", null);

        // Act - Second request (cached response)
        var response2 = await _httpClient.PostAsync("v6/TestingIdempotentAPI/testobject", null);

        // Get all Content-Type values from headers
        var contentTypeValues1 = response1.Content.Headers.GetValues("Content-Type").ToList();
        var contentTypeValues2 = response2.Content.Headers.GetValues("Content-Type").ToList();

        _testOutputHelper.WriteLine($"Response 1 Content-Type count: {contentTypeValues1.Count}");
        foreach (var ct in contentTypeValues1)
            _testOutputHelper.WriteLine($"  - {ct}");

        _testOutputHelper.WriteLine($"Response 2 Content-Type count: {contentTypeValues2.Count}");
        foreach (var ct in contentTypeValues2)
            _testOutputHelper.WriteLine($"  - {ct}");

        // Assert - Should have exactly one Content-Type header
        contentTypeValues1.Should().HaveCount(1,
            "First response should have exactly one Content-Type header");

        // This is the key assertion for the fix:
        // Before fix: Could have multiple Content-Type values (one from cache, one from formatter)
        // After fix: Should have exactly one Content-Type value (only from formatter)
        contentTypeValues2.Should().HaveCount(1,
            "Cached response should have exactly one Content-Type header. " +
            "If this fails with multiple values, the Content-Type from the cached headers " +
            "is being added alongside the formatter's Content-Type.");
    }
}
