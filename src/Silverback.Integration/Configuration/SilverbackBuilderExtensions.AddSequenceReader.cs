// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Configuration;

/// <content>
///     Adds the AddSequenceReader methods to the <see cref="SilverbackBuilder" />.
/// </content>
public static partial class SilverbackBuilderExtensions
{
    /// <summary>
    ///     Adds a transient sequence reader of the type specified in <paramref name="readerType" /> to the
    ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <param name="readerType">
    ///     The type of the reader to register and the implementation to use.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddTransientSequenceReader(this SilverbackBuilder builder, Type readerType)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services.AddTransient(typeof(ISequenceReader), readerType);

        return builder;
    }

    /// <summary>
    ///     Adds a transient sequence reader of the type specified in <typeparamref name="TReader" /> to the
    ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <typeparam name="TReader">
    ///     The type of the reader to add.
    /// </typeparam>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddTransientSequenceReader<TReader>(this SilverbackBuilder builder)
        where TReader : class, ISequenceReader =>
        AddTransientSequenceReader(builder, typeof(TReader));

    /// <summary>
    ///     Adds a transient sequence reader with a factory specified in <paramref name="implementationFactory" />
    ///     to
    ///     the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <param name="implementationFactory">
    ///     The factory that creates the service.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddTransientSequenceReader(
        this SilverbackBuilder builder,
        Func<IServiceProvider, ISequenceReader> implementationFactory)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services.AddTransient(typeof(ISequenceReader), implementationFactory);

        return builder;
    }

    /// <summary>
    ///     Adds a singleton sequence reader of the type specified in <paramref name="readerType" /> to the
    ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <param name="readerType">
    ///     The type of the reader to register and the implementation to use.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddSingletonSequenceReader(this SilverbackBuilder builder, Type readerType)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services.AddSingleton(typeof(ISequenceReader), readerType);

        return builder;
    }

    /// <summary>
    ///     Adds a singleton sequence reader of the type specified in <typeparamref name="TReader" /> to the
    ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <typeparam name="TReader">
    ///     The type of the reader to add.
    /// </typeparam>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddSingletonSequenceReader<TReader>(this SilverbackBuilder builder)
        where TReader : class, ISequenceReader =>
        AddSingletonSequenceReader(builder, typeof(TReader));

    /// <summary>
    ///     Adds a singleton sequence reader with a factory specified in <paramref name="implementationFactory" /> to
    ///     the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <param name="implementationFactory">
    ///     The factory that creates the service.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddSingletonSequenceReader(
        this SilverbackBuilder builder,
        Func<IServiceProvider, ISequenceReader> implementationFactory)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services.AddSingleton(typeof(ISequenceReader), implementationFactory);

        return builder;
    }

    /// <summary>
    ///     Adds a singleton sequence reader with an instance specified in <paramref name="implementationInstance" />
    ///     to the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <param name="implementationInstance">
    ///     The instance of the service.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddSingletonSequenceReader(this SilverbackBuilder builder, ISequenceReader implementationInstance)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services.AddSingleton(typeof(ISequenceReader), implementationInstance);

        return builder;
    }
}
