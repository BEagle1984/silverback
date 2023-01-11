// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Sequences
{
    /// <summary>
    ///     Encapsulates the result of the <see cref="ISequence.AddAsync" /> method.
    /// </summary>
    public record AddToSequenceResult
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="AddToSequenceResult" /> class.
        /// </summary>
        /// <param name="isSuccess">
        ///     A value indicating whether the operation was successful or not.
        /// </param>
        /// <param name="pushedStreamsCount">
        ///     The number of streams that have been actually pushed.
        /// </param>
        public AddToSequenceResult(bool isSuccess, int pushedStreamsCount)
        {
            IsSuccess = isSuccess;
            PushedStreamsCount = pushedStreamsCount;
        }

        /// <summary>
        ///     Gets a static instance representing a failed call to <see cref="ISequence.AddAsync" />.
        /// </summary>
        public static AddToSequenceResult Failed { get; } = new(false, -1);

        /// <summary>
        ///     Gets a value indicating whether the operation was successful or not.
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        ///     Gets the number of streams that have been actually pushed.
        /// </summary>
        public int PushedStreamsCount { get; init; }

        /// <summary>
        ///     Returns a new instance representing a successful call to <see cref="ISequence.AddAsync" />.
        /// </summary>
        /// <param name="pushedStreams">
        ///     The number of streams that have been actually pushed.
        /// </param>
        /// <returns>
        ///     The <see cref="AddToSequenceResult" />.
        /// </returns>
        public static AddToSequenceResult Success(int pushedStreams) => new(true, pushedStreams);
    }
}
