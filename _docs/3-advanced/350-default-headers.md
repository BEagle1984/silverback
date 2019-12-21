---
title: Default Message Headers
permalink: /docs/advanced/headers

toc: false
---

Silverback will add some headers to the produced messages. They may vary depending on the scenario.
Here is the list of the default headers that may be sent.

Header | Optional | Description
:-- | :-: | :--
`x-message-id` | yes<sup>1</sup> | A unique identifier that may be useful for tracing. It may not be present if the produced message isn't implementing `IIntegrationMessage` and no `Id` or `MessageId` property of a supported type is defined.
`x-message-type` | no | The assembly qualified name of the message type.
`x-failed-attempts` | yes | If an exception if thrown the failed attempts will be incremented and stored as header. This is necessary for the [error policies]({{ site.baseurl }}/docs/configuration/inbound#error-handling) to work.
`x-chunk-id` | yes | The unique id of the message chunk, used when [chunking]({{ site.baseurl }}/docs/advanced/chunking) is enabled.
`x-chunks-count` | yes | The total number of chunks the message was splitted into, used when [chunking]({{ site.baseurl }}/docs/advanced/chunking) is enabled.
`x-batch-id` | yes | The unique id assigned to the messages batch, used mostly for tracing, when [batch processing]({{ site.baseurl }}/docs/configuration/inbound#batch-processing) is enabled.
`x-batch-size` | yes | The total number of messages in the batch, used mostly for tracing, when [batch processing]({{ site.baseurl }}/docs/configuration/inbound#batch-processing) is enabled.
`traceparent` | no | The current `Activity.Id`, used by the `IConsumer` implementation to set the `Activity.ParentId`, thus enabling distributed tracing across the message broker. Note that an `Activity` is automatically started by the default `IProducer` implementation. See [System.Diagnostics documentation](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity?view=netcore-3.1) for details about `Activity` and distributed tracing in asp.net core and [W3C Trace Context proposal](https://www.w3.org/TR/trace-context-1) for details about the headers.
`tracestate` | yes | The `Activity.TraceStateString`. See also the [W3C Trace Context proposal](https://www.w3.org/TR/trace-context-1) for details.
`tracebaggage` | yes | The string representation of the `Activity.Baggage` dictionary. See [System.Diagnostics documentation](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity?view=netcore-3.1) for details.

_<sup>1</sup> The `x-message-id` header is always set for_