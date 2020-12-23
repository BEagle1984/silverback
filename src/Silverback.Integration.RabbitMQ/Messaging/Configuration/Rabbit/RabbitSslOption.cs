// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Security.Authentication;

namespace Silverback.Messaging.Configuration.Rabbit
{
    /// <summary>
    ///     The RabbitMQ SSL options configuration.
    /// </summary>
    public sealed class RabbitSslOption : IEquatable<RabbitSslOption>
    {
        /// <summary>
        ///     Gets or sets the SSL policy errors that are deemed acceptable.
        /// </summary>
        public SslPolicyErrors? AcceptablePolicyErrors { get; set; }

        /// <summary>
        ///     Gets or sets the path to client certificate.
        /// </summary>
        public string? CertPassphrase { get; set; }

        /// <summary>
        ///     Gets or sets the path to client certificate.
        /// </summary>
        public string? CertPath { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the peer certificate should be checked for revocation.
        ///     The default is <c>false</c>.
        /// </summary>
        /// <remarks>
        ///     Uses the built-in .NET mechanics for checking a certificate against CRLs.
        /// </remarks>
        public bool? CheckCertificateRevocation { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether SSL should indeed be used.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        ///     Gets or sets the Canonical Name of the server. This MUST match the CN on the certificate otherwise
        ///     the SSL connection will fail.
        /// </summary>
        public string? ServerName { get; set; }

        /// <summary>
        ///     Gets or sets the SSL protocol version.
        /// </summary>
        public SslProtocols Version { get; set; }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(RabbitSslOption? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return AcceptablePolicyErrors == other.AcceptablePolicyErrors &&
                   string.Equals(CertPassphrase, other.CertPassphrase, StringComparison.Ordinal) &&
                   string.Equals(CertPath, other.CertPath, StringComparison.Ordinal) &&
                   CheckCertificateRevocation == other.CheckCertificateRevocation && Enabled == other.Enabled &&
                   string.Equals(ServerName, other.ServerName, StringComparison.Ordinal) &&
                   Version == other.Version;
        }

        /// <inheritdoc cref="object.Equals(object)" />
        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((RabbitSslOption)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = Justifications.Settings)]
        public override int GetHashCode()
        {
            var hashCode = default(HashCode);

            hashCode.Add(AcceptablePolicyErrors);
            hashCode.Add(CertPassphrase);
            hashCode.Add(CertPath);
            hashCode.Add(CheckCertificateRevocation);
            hashCode.Add(Enabled);
            hashCode.Add(ServerName);
            hashCode.Add(Version);

            return hashCode.ToHashCode();
        }
    }
}
