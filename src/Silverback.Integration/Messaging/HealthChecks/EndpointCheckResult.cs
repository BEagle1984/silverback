// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.HealthChecks
{
    public class EndpointCheckResult
    {
        public EndpointCheckResult(string endpointName, bool isSuccessful, string errorMessage = null)
        {
            EndpointName = endpointName;
            IsSuccessful = isSuccessful;
            ErrorMessage = errorMessage;
        }

        public string EndpointName { get; }
        public bool IsSuccessful { get; }
        public string ErrorMessage { get; }
    }
}