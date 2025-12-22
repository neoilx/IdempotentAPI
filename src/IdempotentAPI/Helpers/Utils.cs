using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IdempotentAPI.Helpers
{
    public static class Utils
    {
        private static readonly JsonSerializerOptions _defaultSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Match ASP.NET Core's default
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {
            return GetHash(hashAlgorithm, Encoding.UTF8.GetBytes(input));
        }

        public static string GetHash(HashAlgorithm hashAlgorithm, byte[] input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(input);

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        /// <summary>
        /// Serialize and Compress object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[]? Serialize(this object obj, JsonSerializerOptions? serializerOptions = null)
        {
            if (obj is null)
            {
                return null;
            }

            var options = serializerOptions ?? _defaultSerializerOptions;
            string jsonString = JsonSerializer.Serialize(obj, obj.GetType(), options);

            byte[] encodedData = Encoding.UTF8.GetBytes(jsonString);

            return Compress(encodedData);
        }

        /// <summary>
        /// DeSerialize Compressed data.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="compressedBytes"></param>
        /// <returns></returns>
        public static T? DeSerialize<T>(this byte[]? compressedBytes, JsonSerializerOptions? serializerOptions = null)
        {
            if (compressedBytes is null)
            {
                return default;
            }

            byte[]? encodedData = Decompress(compressedBytes);

            string jsonString = Encoding.UTF8.GetString(encodedData);
            var options = serializerOptions ?? _defaultSerializerOptions;
            return JsonSerializer.Deserialize<T>(jsonString, options);
        }

        /// <summary>
        /// Serialize object to JSON string.
        /// </summary>
        public static string SerializeToJson(this object obj, JsonSerializerOptions? serializerOptions = null)
        {
            if (obj is null)
            {
                return "null";
            }

            var options = serializerOptions ?? _defaultSerializerOptions;
            return JsonSerializer.Serialize(obj, obj.GetType(), options);
        }


        public static byte[]? Compress(byte[] input)
        {
            if (input is null)
            {
                return null;
            }

            byte[] compressesData;

            using (var outputStream = new MemoryStream())
            {
                using (var zip = new GZipStream(outputStream, CompressionMode.Compress))
                {
                    zip.Write(input, 0, input.Length);
                }

                compressesData = outputStream.ToArray();
            }

            return compressesData;
        }

        public static byte[]? Decompress(byte[]? input)
        {
            if (input is null)
            {
                return null;
            }

            byte[] decompressedData;

            using (var outputStream = new MemoryStream())
            {
                using (var inputStream = new MemoryStream(input))
                {
                    using (var zip = new GZipStream(inputStream, CompressionMode.Decompress))
                    {
                        zip.CopyTo(outputStream);
                    }
                }

                decompressedData = outputStream.ToArray();
            }

            return decompressedData;
        }

        public static IDictionary<string, T> AnonymousObjectToDictionary<T>(
            object obj,
            Func<object, T> valueSelect)
        {
            return TypeDescriptor.GetProperties(obj)
                .OfType<PropertyDescriptor>()
                .ToDictionary(
                    prop => prop.Name,
                    prop => valueSelect(prop.GetValue(obj))
                );
        }

        public static bool IsAnonymousType(this object obj)
        {
            if (obj is null)
            {
                return false;
            }

            // HACK: The only way to detect anonymous types right now.
            Type type = obj.GetType();
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                       && type.IsGenericType && type.Name.Contains("AnonymousType")
                       && (type.Name.StartsWith("<>", StringComparison.OrdinalIgnoreCase) ||
                           type.Name.StartsWith("VB$", StringComparison.OrdinalIgnoreCase))
                       && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        /// <summary>
        /// Gets an integer value from an object that may be a JsonElement or a boxed int.
        /// </summary>
        public static int GetInt32(this object obj)
        {
            if (obj is JsonElement jsonElement)
            {
                return jsonElement.GetInt32();
            }
            return Convert.ToInt32(obj);
        }

        /// <summary>
        /// Gets a string value from an object that may be a JsonElement or a string.
        /// </summary>
        public static string? GetStringValue(this object obj)
        {
            if (obj is JsonElement jsonElement)
            {
                return jsonElement.GetString();
            }
            return obj?.ToString();
        }

        /// <summary>
        /// Converts a JsonElement or Dictionary to Dictionary&lt;string, object&gt;.
        /// </summary>
        public static Dictionary<string, object> ToDictionaryStringObject(this object obj)
        {
            if (obj is JsonElement jsonElement)
            {
                var result = new Dictionary<string, object>();
                foreach (var property in jsonElement.EnumerateObject())
                {
                    result[property.Name] = property.Value;
                }
                return result;
            }
            if (obj is Dictionary<string, object> dict)
            {
                return dict;
            }
            throw new InvalidCastException($"Cannot convert {obj?.GetType().Name} to Dictionary<string, object>");
        }

        /// <summary>
        /// Converts a JsonElement or Dictionary to Dictionary&lt;string, string&gt;.
        /// </summary>
        public static Dictionary<string, string> ToDictionaryStringString(this object obj)
        {
            if (obj is JsonElement jsonElement)
            {
                var result = new Dictionary<string, string>();
                foreach (var property in jsonElement.EnumerateObject())
                {
                    result[property.Name] = property.Value.GetString() ?? string.Empty;
                }
                return result;
            }
            if (obj is Dictionary<string, string> dict)
            {
                return dict;
            }
            throw new InvalidCastException($"Cannot convert {obj?.GetType().Name} to Dictionary<string, string>");
        }

        /// <summary>
        /// Converts a JsonElement or Dictionary to Dictionary&lt;string, List&lt;string&gt;&gt;.
        /// </summary>
        public static Dictionary<string, List<string>>? ToDictionaryStringListString(this object? obj)
        {
            if (obj is null)
            {
                return null;
            }
            if (obj is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Null)
                {
                    return null;
                }
                var result = new Dictionary<string, List<string>>();
                foreach (var property in jsonElement.EnumerateObject())
                {
                    var list = new List<string>();
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        list.Add(item.GetString() ?? string.Empty);
                    }
                    result[property.Name] = list;
                }
                return result;
            }
            if (obj is Dictionary<string, List<string>> dict)
            {
                return dict;
            }
            throw new InvalidCastException($"Cannot convert {obj?.GetType().Name} to Dictionary<string, List<string>>");
        }
    }
}