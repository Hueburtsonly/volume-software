namespace Software.Configuration
{
    /// <summary>
    /// An interface implemented by classes capable of providing configuration values from some underlying store.
    /// </summary>
    public interface IConfigurationProvider
    {
        /// <summary>Gets a configuration setting value.</summary>
        /// <param name="key">The configuration key.</param>
        /// <param name="required">A boolean value indicating if the configuration setting value is required.</param>
        /// <returns>The configuration value.</returns>
        string GetConfigurationValue(string key, bool required = true);
    }
}
