// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using MQTTnet.Client.Options;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt.Comparers
{
    internal sealed class MqttClientTlsOptionsEqualityComparer : IEqualityComparer<MqttClientTlsOptions>
    {
        private static readonly CollectionEqualityComparer<X509Certificate> CertificatesComparer = new();

        public static MqttClientTlsOptionsEqualityComparer Instance { get; } = new();

        public bool Equals(MqttClientTlsOptions? x, MqttClientTlsOptions? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;

            return x.UseTls == y.UseTls &&
                   x.IgnoreCertificateRevocationErrors == y.IgnoreCertificateRevocationErrors &&
                   x.IgnoreCertificateChainErrors == y.IgnoreCertificateChainErrors &&
                   x.AllowUntrustedCertificates == y.AllowUntrustedCertificates &&
                   CertificatesComparer.Equals(x.Certificates, y.Certificates) &&
                   x.SslProtocol == y.SslProtocol;
        }

        public int GetHashCode(MqttClientTlsOptions obj) => HashCode.Combine(
            obj.UseTls,
            obj.IgnoreCertificateRevocationErrors,
            obj.IgnoreCertificateChainErrors,
            obj.AllowUntrustedCertificates,
            obj.Certificates,
            (int)obj.SslProtocol);
    }
}
