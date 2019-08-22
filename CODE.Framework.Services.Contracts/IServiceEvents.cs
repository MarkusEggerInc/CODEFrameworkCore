namespace CODE.Framework.Services.Contracts
{
    /// <summary>
    /// Standard features that can be implemented on a service implementation.
    /// </summary>
    public interface IServiceEvents
    {
        /// <summary>
        /// Fires when a service is added to a host
        /// </summary>
        void OnInProcessHostLaunched();
    }
}
