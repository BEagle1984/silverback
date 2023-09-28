// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using MQTTnet.Client;

namespace Silverback.Messaging.Configuration.Mqtt.Comparers
{
    internal sealed class MqttClientTlsOptionsEqualityComparer : IEqualityComparer<MqttClientTlsOptions>
    {
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

            bool result = x.CertificateValidationHandler == y.CertificateValidationHandler &&
                          x.UseTls == y.UseTls &&
                          x.IgnoreCertificateRevocationErrors == y.IgnoreCertificateRevocationErrors &&
                          x.IgnoreCertificateChainErrors == y.IgnoreCertificateChainErrors &&
                          x.AllowUntrustedCertificates == y.AllowUntrustedCertificates &&
                          x.RevocationMode == y.RevocationMode &&
                          x.ClientCertificatesProvider == y.ClientCertificatesProvider &&
                          x.SslProtocol == y.SslProtocol;

#if NETCOREAPP3_1_OR_GREATER
            result = result && x.ApplicationProtocols.SequenceEqual(y.ApplicationProtocols) &&
                              x.CipherSuitesPolicy == y.CipherSuitesPolicy &&
                              x.EncryptionPolicy == y.EncryptionPolicy &&
                              x.AllowRenegotiation == y.AllowRenegotiation;
#endif

            return result;
        }

        public int GetHashCode(MqttClientTlsOptions obj) => HashCode.Combine(
            obj.UseTls,
            obj.IgnoreCertificateRevocationErrors,
            obj.IgnoreCertificateChainErrors,
            obj.AllowUntrustedCertificates,
            obj.RevocationMode,
            (int)obj.SslProtocol);
    }
}
