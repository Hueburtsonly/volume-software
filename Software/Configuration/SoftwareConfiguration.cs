using System;

namespace Software.Configuration
{
    public class SoftwareConfiguration
    {
        private readonly IConfigurationProvider _configuration;

        public SoftwareConfiguration(IConfigurationProvider configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public string ConfigFilePath => _configuration.GetConfigurationValue("LuaConfigFilePath");

    }
}