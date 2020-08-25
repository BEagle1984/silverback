// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Diagnostics
{
    /// <summary>
    ///     Extends the <see cref="ISilverbackLogger{TCategoryName}" /> adding some methods used to consistently
    ///     enrich the log entry with the information about the message(s) being produced or consumed.
    /// </summary>
    /// <typeparam name="TCategoryName">The type who's name is used for the logger category name.</typeparam>
    public interface ISilverbackIntegrationLogger<out TCategoryName>
        : ISilverbackIntegrationLogger, ISilverbackLogger<TCategoryName>
    {
    }
}
