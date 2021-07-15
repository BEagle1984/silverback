---
uid: validation
---

# Message Validation

Both the consumed and produced messages are being validated using the same mechanism implemented in the asp.net controllers.

You can either decorate the message model with the [System.ComponentModel.DataAnnotations.ValidationAttribute](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.validationattribute) standard implementations, create your own attributes (extending [ValidationAttribute](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.validationattribute)) or otherwise you can implement the [IValidatableObject](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.ivalidatableobject) interface in your message model. 

## Configuration

The <xref:Silverback.Messaging.Validation.MessageValidationMode> can be configured per endpoint and has 3 values:
* `LogWarning` (default): a warning is logged if the message is not valid
* `ThrowException`: an exception is thrown if the message is not valid
* `None`: the validation is completely disabled

> [!Note]
> If an invalid message is produced, the <xref:Silverback.Messaging.Validation.MessageValidationException> will be rethrown by the `Produce`/`Publish` method.
>
> In the consumer it will instead be handled like any other exception, according to the configured policies, or leading to the consumer being stopped.

> [!Warning]
> The validation might have a - relatively speaking - big impact on the performance, depending on the object size, the number of validations to be performed and their complexity.
>
> You might want to consider disabling the validation, if performance is a critical concern in your use case.

# [Fluent](#tab/fluent)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddOutbound<InventoryEvent>(endpoint => endpoint
                    .ProduceTo("inventory-events")
                    .DisableMessageValidation()
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("order-events")
                    .Configure(config => 
                        {
                            config.GroupId = "my-consumer";
                        })
                    .ValidateMessage(throwException: true)));
}
```
# [Legacy](#tab/legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddOutbound<InventoryEvent>(
                new KafkaProducerEndpoint("inventory-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    },
                    MessageValidationMode = MessageValidationMode.None 
                })
            .AddInbound(
                new KafkaConsumerEndpoint("order-events")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        GroupId = "my-consumer"
                    },
                    MessageValidationMode = MessageValidationMode.ThrowException
                });
}
```
***

## Validated models examples

### Using annotations

```csharp
public class CreateUserCommand
{
    [Required]
    [MinLength(3)]
    [MaxLength(100)]
    public string Username { get; set; }
}
```

### Implementing IValidatableObject

```csharp
public class CreateUserCommand : IValidatableObject
{
    public string Username { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (Username == null || 
            Username.Length < 3 || 
            Username.Length > 100)
        {
            yield return new ValidationResult(
                "Invalid username.",
                new[] { nameof(Username) });
        }
    }
}
```
