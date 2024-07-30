using Newtonsoft.Json;

namespace Silverback.Samples.Kafka.JsonSchemaRegistry.Common;

public class SampleMessage
{
    [JsonIgnore]
    public const string Schema =
        """
        {
          "$schema": "http://json-schema.org/draft-04/schema#",
          "description": "",
          "type": "object",
          "properties": {
            "number": {
              "type": "number"
            }
          },
          "required": [
            "number"
          ]
        }
        """;

    public int Number { get; set; }
}
