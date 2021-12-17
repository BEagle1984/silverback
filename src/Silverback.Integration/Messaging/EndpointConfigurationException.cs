// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Silverback.Messaging;

/// <summary>
///     The exception that is thrown when the endpoint configuration is not valid.
/// </summary>
[Serializable]
[ExcludeFromCodeCoverage]
public class EndpointConfigurationException : SilverbackException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EndpointConfigurationException" /> class.
    /// </summary>
    public EndpointConfigurationException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EndpointConfigurationException" /> class with the
    ///     specified message.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    public EndpointConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EndpointConfigurationException" /> class with the
    ///     specified message and inner exception.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    /// <param name="innerException">
    ///     The inner exception.
    /// </param>
    public EndpointConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EndpointConfigurationException" /> class with the specified message.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    /// <param name="propertyValue">
    ///     The value of the configuration property that caused the exception.
    /// </param>
    /// <param name="propertyName">
    ///     The name of the configuration property that caused the exception.
    /// </param>
    public EndpointConfigurationException(string message, object? propertyValue, string propertyName)
        : base(GetMessage(message, propertyValue, propertyName))
    {
        ConfigurationType = new StackFrame(1).GetMethod()?.DeclaringType;
        PropertyName = propertyName;
        PropertyValue = propertyValue;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EndpointConfigurationException" /> class with the
    ///     serialized data.
    /// </summary>
    /// <param name="info">
    ///     The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being
    ///     thrown.
    /// </param>
    /// <param name="context">
    ///     The <see cref="StreamingContext" /> that contains contextual information about the source or
    ///     destination.
    /// </param>
    protected EndpointConfigurationException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    /// <summary>
    ///     Gets the <see cref="Type" /> containing the configuration property that caused the exception.
    /// </summary>
    public Type? ConfigurationType { get; }

    /// <summary>
    ///     Gets the name of the property that caused the exception.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    ///     Gets the value of the property that caused the exception.
    /// </summary>
    public object? PropertyValue { get; }

    private static string GetMessage(string message, object? propertyValue, string propertyName)
    {
        Type? configurationType = new StackFrame(2).GetMethod()?.DeclaringType;

        return configurationType == null
            ? $"{message}{Environment.NewLine}" +
              $"Configuration property: {propertyName}{Environment.NewLine}" +
              $"Value: {propertyValue ?? "null"}"
            : $"{message}{Environment.NewLine}" +
              $"Configuration property: {configurationType.Name}.{propertyName}{Environment.NewLine}" +
              $"Value: {propertyValue ?? "null"}";
    }
}
