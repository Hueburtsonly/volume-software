using System;
using Serilog;
using Serilog.Events;

namespace Software.Logging
{
    /// <summary>
    /// An implementation of LoggingProvider that uses Serilog as the underlying logging framework.
    /// It is expected that a singleton instance of SerilogLoggingProvider will be maintained by the application.
    /// </summary>
    public class SerilogLoggingProvider : LoggingProvider
    {

        private readonly ILogger _log;

        /// <summary>
        /// Constructs an instance of SerilogLoggingProvider.
        /// It is expected that a singleton instance of SerilogLoggingProvider will be maintained by the application.
        /// </summary>
        public SerilogLoggingProvider()
        {
            //This dependency should be a singleton.
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration().ReadFrom.AppSettings();
            _log = loggerConfiguration.CreateLogger();
        }

        /// <summary>
        /// Constructs an instance of SerilogLoggingProvider.
        /// </summary>
        /// <param name="configuration">The LoggerConfiguration to use for constructing the logger.</param>
        public SerilogLoggingProvider(LoggerConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");

            _log = configuration.CreateLogger();
        }


        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="args">Any arguments to pass to the message template.</param>
        public override void Debug(string messageTemplate, params object[] args)
        {
            if (String.IsNullOrEmpty(messageTemplate)) throw new ArgumentException("messageTemplate cannot be null or empty");

            //WARN: Serilog parses and caches the message templates, so if the message template is effectively just a string message
            //use a basic template and push the message into that.
            if (args == null || args.Length == 0)
            {
                _log.Debug("{0:l}", messageTemplate);
            }
            else
            {
                _log.Debug(messageTemplate, args);
            }
        }

        /// <summary>
        /// Logs an exception
        /// </summary>
        /// <param name="ex">The Exception</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="args">Any arguments to pass to the message template.</param>
        public override void Error(Exception ex, string messageTemplate, params object[] args)
        {
            if (ex == null && String.IsNullOrEmpty(messageTemplate)) throw new ArgumentException("messageTemplate cannot be null or empty when no exception is provided");

            if (ex == null && (args == null || args.Length == 0))
            {
                _log.Error("{0:l}", messageTemplate);
            }
            else
            {
                //Serilog insists on there being a message template before it dispatches an event to sinks.
                _log.Error(ex, messageTemplate ?? "", args);
            }
        }

        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="args">Any arguments to pass to the message template.</param>
        public override void Info(string messageTemplate, params object[] args)
        {
            if (String.IsNullOrEmpty(messageTemplate)) throw new ArgumentException("messageTemplate cannot be null or empty");

            //WARN: Serilog parses and caches the message templates, so if the message template is effectively just a string message
            //use a basic template and push the message into that.
            if (args == null || args.Length == 0)
            {
                _log.Information("{0:l}", messageTemplate);
            }
            else
            {
                _log.Information(messageTemplate, args);
            }
        }

        /// <summary>
        /// Gets a boolean value indicating if the requested log level is enabled.
        /// This will return the Serilog log level, not the log level associated with any specific sink.
        /// </summary>
        /// <param name="requestedLogLevel">The requested log level.</param>
        /// <returns>true if the requested log level is enabled, otherwise false.</returns>
        public override bool IsLogLevelEnabled(LogLevel requestedLogLevel)
        {
            //Serilog doesn't have an Off level.
            return (requestedLogLevel != LogLevel.Off && _log.IsEnabled(GetSerilogLevel(requestedLogLevel)));
        }

        /// <summary>
        /// Logs a verbose message in DEBUG builds only.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="args">Any arguments to pass to the message template.</param>
        public override void Verbose(string messageTemplate, params object[] args)
        {
            if (String.IsNullOrEmpty(messageTemplate)) throw new ArgumentException("messageTemplate cannot be null or empty");

            //WARN: Serilog parses and caches the message templates, so if the message template is effectively just a string message
            //use a basic template and push the message into that.
            if (args == null || args.Length == 0)
            {
                _log.Verbose("{0:l}", messageTemplate);
            }
            else
            {
                _log.Verbose(messageTemplate, args);
            }
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="ex">The Exception</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="args">Any arguments to pass to the message template.</param>
        public override void Warn(Exception ex, string messageTemplate, params object[] args)
        {
            if (ex == null && String.IsNullOrEmpty(messageTemplate)) throw new ArgumentException("messageTemplate cannot be null or empty when no exception is provided");

            if (ex == null && (args == null || args.Length == 0))
            {
                _log.Warning("{0:l}", messageTemplate);
            }
            else
            {
                //Serilog insists on there being a message template before it dispatches an event to sinks.
                _log.Warning(ex, messageTemplate ?? "", args);
            }
        }

        /// <summary>
        /// Converts a Vulcan LogLevel into a Serilog LogEventLevel.
        /// </summary>
        /// <param name="level">The Vulcan LogLevel.</param>
        /// <returns>The corresponding Serilog LogEventLevel.</returns>
        private static LogEventLevel GetSerilogLevel(LogLevel level)
        {
            return (LogEventLevel)(level + 1);
        }

    }
}
