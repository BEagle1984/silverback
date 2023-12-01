// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback
{
    internal static class Justifications
    {
        public const string MarkerInterface =
            "Intentionally a marker interface";

        public const string BaseInterface =
            "Implemented by other interfaces to group them";

        public const string ExceptionLogged =
            "The exception is logged, it is not swallowed";

        public const string CanExposeByteArray =
            "It doesn't really cause any issue and it would feel unnatural to expose a " +
            "collection instead of a byte array";

        public const string AllowedForConstants =
            "Nested types are allowed in constant classes: they are used to create a structure";

        public const string CalledBySilverback =
            "The method is called by Silverback (e.g. subscriber methods called by the publisher)";

        public const string Settings =
            "Setting classes expose a setter for their properties but it's safe to assume that " +
            "their value will not be modified after the application has been started";

        public const string NoWayToReduceTypeParameters =
            "All type parameters are needed and there's unfortunately no clean way to reduce " +
            "their number";

        public const string FireAndForget =
            "The Task is purposedly started as a fire and forget";

        public const string ExecutesSyncOrAsync =
            "The method executes either synchronously or asynchronously";

        public const string CatchAllExceptions =
            "All exception types require catching";
    }
}
