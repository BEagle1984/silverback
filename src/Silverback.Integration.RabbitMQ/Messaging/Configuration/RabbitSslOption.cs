// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Security.Authentication;

namespace Silverback.Messaging.Configuration
{
    public sealed class RabbitSslOption : IEquatable<RabbitSslOption>
    {
        /// <summary>
        ///     Retrieve or set the set of ssl policy errors that are deemed acceptable.
        /// </summary>
        public SslPolicyErrors? AcceptablePolicyErrors { get; set; }

        /// <summary>Retrieve or set the path to client certificate.</summary>
        public string CertPassphrase { get; set; }

        /// <summary>Retrieve or set the path to client certificate.</summary>
        public string CertPath { get; set; }

        // /// <summary>
        // /// An optional client specified SSL certificate selection callback.  If this is not specified,
        // /// the first valid certificate found will be used.
        // /// </summary>
        // public LocalCertificateSelectionCallback CertificateSelectionCallback { get; set; }
        //
        // /// <summary>
        // /// An optional client specified SSL certificate validation callback.  If this is not specified,
        // /// the default callback will be used in conjunction with the <see cref="P:RabbitMQ.Client.SslOption.AcceptablePolicyErrors" /> property to
        // /// determine if the remote server certificate is valid.
        // /// </summary>
        // public RemoteCertificateValidationCallback CertificateValidationCallback { get; set; }

        /// <summary>
        ///     Attempts to check certificate revocation status. Default is false. True if peer certificate should be
        ///     checked for revocation, false otherwise.
        /// </summary>
        /// <remarks>Uses the built-in .NET mechanics for checking a certificate against CRLs.</remarks>
        public bool? CheckCertificateRevocation { get; set; }

        /// <summary>Flag specifying if Ssl should indeed be used.</summary>
        public bool Enabled { get; set; }

        /// <summary>
        ///     Retrieve or set server's Canonical Name.
        ///     This MUST match the CN on the Certificate else the SSL connection will fail.
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>Retrieve or set the Ssl protocol version.</summary>
        public SslProtocols Version { get; set; }

        #region Equality

        public bool Equals(RabbitSslOption other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return AcceptablePolicyErrors == other.AcceptablePolicyErrors &&
                   string.Equals(CertPassphrase, other.CertPassphrase, StringComparison.InvariantCulture) &&
                   string.Equals(CertPath, other.CertPath, StringComparison.InvariantCulture) &&
                   CheckCertificateRevocation == other.CheckCertificateRevocation && Enabled == other.Enabled &&
                   string.Equals(ServerName, other.ServerName, StringComparison.InvariantCulture) &&
                   Version == other.Version;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((RabbitSslOption) obj);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = AcceptablePolicyErrors.GetHashCode();
                hashCode = (hashCode * 397) ^ (CertPassphrase != null
                               ? StringComparer.InvariantCulture.GetHashCode(CertPassphrase)
                               : 0);
                hashCode = (hashCode * 397) ^ (CertPath != null
                               ? StringComparer.InvariantCulture.GetHashCode(CertPath)
                               : 0);
                hashCode = (hashCode * 397) ^ CheckCertificateRevocation.GetHashCode();
                hashCode = (hashCode * 397) ^ Enabled.GetHashCode();
                hashCode = (hashCode * 397) ^ (ServerName != null
                               ? StringComparer.InvariantCulture.GetHashCode(ServerName)
                               : 0);
                hashCode = (hashCode * 397) ^ (int) Version;
                return hashCode;
            }
        }

        #endregion
    }
}