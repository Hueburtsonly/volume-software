using System;
using System.Collections.Specialized;
using System.Configuration;

namespace Software.Configuration
{
    /// <summary>
    /// An implementation of <see cref="IConfigurationProvider"/> that retrieves configuration values from the standard .NET configuration files.
    /// </summary>
    public class AppConfigurationValueProvider : IConfigurationProvider
    {

        private readonly NameValueCollection _appSettings;


        /// <summary>
        /// Constructs an instance of <see cref="AppConfigurationValueProvider"/> using the default collections provided by
        /// ConfigurationManager.AppSettings
        /// </summary>
        public AppConfigurationValueProvider() : this(ConfigurationManager.AppSettings) { }

        /// <summary>
        /// Constructs an instance of <see cref="AppConfigurationValueProvider"/> using the provided collections.
        /// </summary>
        /// <param name="appSettings">The NameValueCollection of appSettings.</param>
        public AppConfigurationValueProvider(NameValueCollection appSettings)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        }


        /// <summary>
        /// Gets a configuration setting value.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <param name="required">A boolean value indicating if the configuration setting value is required.</param>
        /// <returns>The configuration value.</returns>
        public string GetConfigurationValue(string key, bool required = true)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("value cannot be null or empty", nameof(key));

            string result = _appSettings[key];

            if (required && result == null) throw new ConfigurationErrorsException("The configuration key is missing: " + key);

            return result;
        }
    }

    public class SoftwareConfiguration
    {
        private readonly IConfigurationProvider _configuration;

        public SoftwareConfiguration(IConfigurationProvider configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        //todo: config values
    }
    
}