// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Amazon.Runtime;

namespace OpenTelemetry.Instrumentation.AWS.Implementation;

internal class AWSLlmModelProcessor
{
    internal static void ProcessRequestModelAttributes(Activity activity, AmazonWebServiceRequest request, string model)
    {
        var requestBodyProperty = request.GetType().GetProperty("Body");
        if (requestBodyProperty != null)
        {
            var body = requestBodyProperty.GetValue(request) as MemoryStream;
            if (body != null)
            {
                try
                {
                    var jsonString = Encoding.UTF8.GetString(body.ToArray());
                    var jsonObject = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);

                    // extract model specific attributes based on model name
                    switch (model)
                    {
                        case "amazon.titan":
                            ProcessTitanModelAttributes(activity, jsonObject);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex.Message);
                }
            }
        }
    }

    private static void ProcessTitanModelAttributes(Activity activity, Dictionary<string, object> jsonObject)
    {
        try
        {
            if (jsonObject.TryGetValue("textGenerationConfig", out var textGenerationConfigObj))
            {
                if (textGenerationConfigObj is JsonElement jsonElement)
                {
                    if (jsonElement.TryGetProperty("topP", out var topP))
                    {
                        activity.SetTag("gen_ai.request.top_p", topP.GetDouble());
                    }

                    if (jsonElement.TryGetProperty("temperature", out var temperature))
                    {
                        activity.SetTag("gen_ai.request.temperature", temperature.GetDouble());
                    }

                    if (jsonElement.TryGetProperty("maxTokenCount", out var maxTokens))
                    {
                        activity.SetTag("gen_ai.request.max_tokens", maxTokens.GetInt32());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: " + ex.Message);
        }
    }
}
