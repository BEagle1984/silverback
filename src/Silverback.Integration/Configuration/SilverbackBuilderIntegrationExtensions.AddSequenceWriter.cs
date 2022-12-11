// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Configuration;

/// <content>
///     Adds the AddSequenceWriter methods to the <see cref="SilverbackBuilder" />.
/// </content>
public static partial class SilverbackBuilderIntegrationExtensions
{
    /// <summary>
    ///     Adds a transient sequence writer of the type specified in <paramref name="writerType" /> to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <param name="writerType">
    ///     The type of the writer to register and the implementation to use.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddTransientSequenceWriter(this SilverbackBuilder builder, Type writerType)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services.AddTransient(typeof(ISequenceWriter), writerType);

        return builder;
    }

    /// <summary>
    ///     Adds a transient sequence writer of the type specified in <typeparamref name="TWriter" /> to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <typeparam name="TWriter">
    ///     The type of the writer to add.
    /// </typeparam>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddTransientSequenceWriter<TWriter>(this SilverbackBuilder builder)
        where TWriter : class, ISequenceWriter =>
        AddTransientSequenceWriter(builder, typeof(TWriter));

    /// <summary>
    ///     Adds a transient sequence writer with a factory specified in <paramref name="implementationFactory" />
    ///     to
    ///     the <see cref="IServiceCollection" />.
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
    public static SilverbackBuilder AddTransientSequenceWriter(
        this SilverbackBuilder builder,
        Func<IServiceProvider, ISequenceWriter> implementationFactory)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services.AddTransient(typeof(ISequenceWriter), implementationFactory);

        return builder;
    }

    /// <summary>
    ///     Adds a singleton sequence writer of the type specified in <paramref name="writerType" /> to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <param name="writerType">
    ///     The type of the writer to register and the implementation to use.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddSingletonSequenceWriter(this SilverbackBuilder builder, Type writerType)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services.AddSingleton(typeof(ISequenceWriter), writerType);

        return builder;
    }

    /// <summary>
    ///     Adds a singleton sequence writer of the type specified in <typeparamref name="TWriter" /> to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <typeparam name="TWriter">
    ///     The type of the writer to add.
    /// </typeparam>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddSingletonSequenceWriter<TWriter>(this SilverbackBuilder builder)
        where TWriter : class, ISequenceWriter =>
        AddSingletonSequenceWriter(builder, typeof(TWriter));

    /// <summary>
    ///     Adds a singleton sequence writer with a factory specified in <paramref name="implementationFactory" /> to the
    ///     <see cref="IServiceCollection" />.
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
    public static SilverbackBuilder AddSingletonSequenceWriter(
        this SilverbackBuilder builder,
        Func<IServiceProvider, ISequenceWriter> implementationFactory)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services.AddSingleton(typeof(ISequenceWriter), implementationFactory);

        return builder;
    }

    /// <summary>
    ///     Adds a singleton sequence writer with an instance specified in <paramref name="implementationInstance" /> to the
    ///     <see cref="IServiceCollection" />.
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
    public static SilverbackBuilder AddSingletonSequenceWriter(this SilverbackBuilder builder, ISequenceWriter implementationInstance)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services.AddSingleton(typeof(ISequenceWriter), implementationInstance);

        return builder;
    }
}
