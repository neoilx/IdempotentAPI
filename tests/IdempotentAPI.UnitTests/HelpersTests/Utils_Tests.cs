using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using IdempotentAPI.Helpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace IdempotentAPI.UnitTests.HelpersTests
{
    public class Utils_Tests
    {
        [Fact]
        public void DederializeSerializedData_ShoultResultToTheOriginalData()
        {
            // Arrange
            Dictionary<string, object> cacheData = new Dictionary<string, object>();

            // Cache string, int, etc.
            cacheData.Add("Request.Method", "POST");
            cacheData.Add("Response.StatusCode", 200);

            // Cache a Dictionary containing a List
            Dictionary<string, List<string>> headers = new Dictionary<string, List<string>>();
            headers.Add("myHeader1", new List<string>() { "value1-1", "value1-2" });
            headers.Add("myHeader2", new List<string>() { "value2-1", "value2-1" });
            cacheData.Add("Response.Headers", headers);

            // Cache a Dictionary containing an object
            Dictionary<string, object> resultObjects = new Dictionary<string, object>();
            CreatedAtRouteResult createdAtRouteResult = new CreatedAtRouteResult("myRoute", new { id = 1 }, new { prop1 = 1, prop2 = "2" });
            resultObjects.Add("ResultType", "ResultType");
            resultObjects.Add("ResultValue", createdAtRouteResult.Value);

            // Cache a Dictionary containing string
            Dictionary<string, string> routeValues = new Dictionary<string, string>();
            routeValues.Add("route1", "routeValue1");
            routeValues.Add("route2", "routeValue2");
            resultObjects.Add("ResultRouteValues", routeValues);

            cacheData.Add("Context.Result", resultObjects);


            // Act

            // Step 1. Serialize data:
            byte[] serializedData = cacheData.Serialize();

            // Step 2. Deserialize the serialized data:
            Dictionary<string, object> cacheDataAfterSerialization =
                serializedData.DeSerialize<Dictionary<string, object>>();


            // Assert
            // With System.Text.Json, deserialized values are JsonElement when target type is object.
            // We need to verify the data can be correctly extracted using our helper methods.

            // Verify primitive values
            cacheDataAfterSerialization["Request.Method"].GetStringValue().Should().Be("POST");
            cacheDataAfterSerialization["Response.StatusCode"].GetInt32().Should().Be(200);

            // Verify headers dictionary
            var deserializedHeaders = cacheDataAfterSerialization["Response.Headers"].ToDictionaryStringListString();
            deserializedHeaders.Should().ContainKey("myHeader1");
            deserializedHeaders["myHeader1"].Should().BeEquivalentTo(new List<string> { "value1-1", "value1-2" });
            deserializedHeaders.Should().ContainKey("myHeader2");
            deserializedHeaders["myHeader2"].Should().BeEquivalentTo(new List<string> { "value2-1", "value2-1" });

            // Verify context result dictionary
            var deserializedResultObjects = cacheDataAfterSerialization["Context.Result"].ToDictionaryStringObject();
            deserializedResultObjects["ResultType"].GetStringValue().Should().Be("ResultType");

            // Verify route values
            var deserializedRouteValues = deserializedResultObjects["ResultRouteValues"].ToDictionaryStringString();
            deserializedRouteValues["route1"].Should().Be("routeValue1");
            deserializedRouteValues["route2"].Should().Be("routeValue2");

            // Verify ResultValue is preserved as JsonElement (can be serialized to response)
            // Dictionary keys are preserved as-is, but object property names use camelCase
            deserializedResultObjects["ResultValue"].Should().BeOfType<JsonElement>();
            var resultValueElement = (JsonElement)deserializedResultObjects["ResultValue"];
            resultValueElement.GetProperty("prop1").GetInt32().Should().Be(1);
            resultValueElement.GetProperty("prop2").GetString().Should().Be("2");
        }
    }
}
