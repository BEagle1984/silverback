---
uid: producing-tracing
---

# Distributed Tracing and Producers

In .NET, distributed tracing is built around the concept of [Activity](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity), which represents a unit of work in the system. When an application handles a request or performs a specific operation, an `Activity` is started to capture timing information and correlation context. This enables end-to-end traceability across services and components.

When propagating requests, the .NET runtime and ASP.NET Core automatically forward the current trace context via standard headers such as `traceparent` and `tracestate`. For example, when using `HttpClient`, these headers are automatically added to outgoing HTTP requests, ensuring the downstream services can continue the trace.

Similarly, Silverback ensures that the current tracing context is propagated when producing messages. This means that:
* The `traceparent` and `tracestate` headers are set to maintain compatibility with the [W3C Trace Context specification](https://www.w3.org/TR/trace-context/).
* Any baggage (key-value pairs used for additional metadata) is also forwarded in the `tracebaggage` header, depending on the transport.

This transparent propagation allows all downstream consumers to continue the trace and provide full observability of the system.

[!Important]
> This works out of the box with Kafka and MQTT 5 (leveraging user properties), while previous MQTT versions don't support custom headers and therefore can't propagate the trace context.

## Additional Resources

* <xref:default-headers> guide
* <xref:consuming-tracing> guide
* [Distributed Tracing](https://www.w3.org/TR/trace-context/) specification
* [Activity](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity) class documentation
