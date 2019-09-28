// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Silverback.Messaging.Configuration.Common;
using Silverback.Messaging.Configuration.Reflection;
using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Configuration.Inbound
{
    public class ErrorPoliciesSectionReader
    {
        private readonly CustomActivator _customActivator;

        public ErrorPoliciesSectionReader(CustomActivator customActivator)
        {
            _customActivator = customActivator;
        }

        public IEnumerable<ErrorPolicyBase> GetErrorPolicies(IConfigurationSection configSection) =>
            configSection.GetChildren().Select(GetErrorPolicy).ToList();

        public ErrorPolicyBase GetErrorPolicy(IConfigurationSection configSection)
        {
            var policyType = configSection.GetSection("Type").Value;

            if (string.IsNullOrWhiteSpace(policyType))
                throw new InvalidOperationException($"Missing Type in section {configSection.Path}.");

            var errorPolicy = _customActivator.Activate<ErrorPolicyBase>(
                configSection,
                policyType,
                policyType + "Policy",
                policyType + "ErrorPolicy",
                policyType + "MessageErrorPolicy");

            configSection.Bind(errorPolicy);

            SetApplyTo(errorPolicy, configSection.GetSection("ApplyTo"));
            SetExclude(errorPolicy, configSection.GetSection("Exclude"));
            SetMaxFailedAttempts(errorPolicy, configSection.GetSection("MaxFailedAttempts"));

            return errorPolicy;
        }

        private void SetApplyTo(ErrorPolicyBase errorPolicy, IConfigurationSection configSection)
        {
            foreach (var typeName in configSection.GetChildren().Select(c => c.Value))
            {
                errorPolicy.ApplyTo(Type.GetType(typeName));
            }
        }

        private void SetExclude(ErrorPolicyBase errorPolicy, IConfigurationSection configSection)
        {
            foreach (var typeName in configSection.GetChildren().Select(c => c.Value))
            {
                errorPolicy.Exclude(Type.GetType(typeName));
            }
        }

        private void SetMaxFailedAttempts(ErrorPolicyBase errorPolicy, IConfigurationSection configSection)
        {
            if (string.IsNullOrEmpty(configSection.Value))
                return;

            errorPolicy.MaxFailedAttempts(int.Parse(configSection.Value));
        }
    }
}