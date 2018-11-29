namespace Silverback.Messaging.ErrorHandling
{
    public enum ErrorAction
    {
        SkipMessage,
        RetryMessage,
        StopConsuming
    }
}