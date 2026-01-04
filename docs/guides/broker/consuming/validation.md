---
uid: consuming-validation
---

# Validating the Consumed Messages

The messages are validated using the same mechanism implemented in the asp.net controllers.

You can either decorate the message model with the [System.ComponentModel.DataAnnotations.ValidationAttribute](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.validationattribute) standard implementations, create your own attributes (extending [ValidationAttribute](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.validationattribute)) or otherwise you can implement the [IValidatableObject](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.ivalidatableobject) interface in your message model.

## Logging Invalid Messages

By default, the validation is enabled and the <xref:Silverback.Messaging.Validation.MessageValidationMode> is set to `LogWarning`. This means that a warning is logged if the consumed message is not valid.

## Preventing Invalid Messages to be Processed

If you want to prevent invalid messages from being processed, you can set the <xref:Silverback.Messaging.Validation.MessageValidationMode> to `ThrowException`. This way, an exception will be thrown when the message is received and the processing will fail. Silverback will then handle the exception according to the specified error policy (see <xref:consuming#error-handling>).

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic")
                .ValidateMessage(throwException: true))));

```

## Disabling Validation

Validation can affect performance, with the impact varying based on object size, the number of validations, and their complexity. If performance is a critical concern for your use case, you may consider disabling validation.

If your models don't have any validation attributes, no validation will be performed by default. However, you can explicitly disable the validation using the `DisableMessageValidation` method.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic")
                .DisableMessageValidation())));

```

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:producing-validation>
- <xref:consuming>
