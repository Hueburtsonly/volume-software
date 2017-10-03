using System;

namespace Software.Logging
{
    /// <summary>
    /// An abstract base class for logging providers.
    /// </summary>
    public abstract class LoggingProvider
    {
        #region Properties

        /// <summary>
        /// Gets a boolean value indicating if the log level is enabled.
        /// </summary>
        public bool IsDebugEnabled
        {
            [DebuggerStepThrough]
            get { return IsLogLevelEnabled(LogLevel.Debug); }
        }

        /// <summary>
        /// Gets a boolean value indicating if the log level is enabled.
        /// </summary>
        public bool IsErrorEnabled
        {
            [DebuggerStepThrough]
            get { return IsLogLevelEnabled(LogLevel.Error); }
        }

        /// <summary>
        /// Gets a boolean value indicating if the log level is enabled.
        /// </summary>
        public bool IsInfoEnabled
        {
            [DebuggerStepThrough]
            get { return IsLogLevelEnabled(LogLevel.Info); }
        }

        /// <summary>
        /// Gets a boolean value indicating if the log level is enabled.
        /// </summary>
        public bool IsWarnEnabled
        {
            [DebuggerStepThrough]
            get { return IsLogLevelEnabled(LogLevel.Warn); }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="args">Any arguments to pass to the message template.</param>
        public abstract void Debug(string messageTemplate, params object[] args);

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="args">Any arguments to pass to the message template.</param>
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
        public void Error(string messageTemplate, params object[] args)
        {
            Error(null, messageTemplate, args);
        }

        /// <summary>
        /// Logs an exception
        /// </summary>
        /// <param name="ex">The Exception</param>
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
        public void Error(Exception ex)
        {
            Error(ex, null, null);
        }

        /// <summary>
        /// Logs an exception
        /// </summary>
        /// <param name="ex">The Exception</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="args">Any arguments to pass to the message template.</param>
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
        public abstract void Error(Exception ex, string messageTemplate, params object[] args);

        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="args">Any arguments to pass to the message template.</param>
        public abstract void Info(string messageTemplate, params object[] args);

        /// <summary>
        /// Gets a boolean value indicating if the requested log level is enabled.
        /// </summary>
        /// <param name="requestedLogLevel">The requested log level.</param>
        /// <returns>true if the requested log level is enabled, otherwise false.</returns>
        public abstract bool IsLogLevelEnabled(LogLevel requestedLogLevel);

        /// <summary>
        /// Creates and returns a string representation of the current instance.
        /// </summary>
        /// <returns>a string representation of the current instance</returns>
        public override string ToString()
        {
            return String.Format("Debug: {0}, Info: {1}, Warn: {2}, Error: {3}",
                IsLogLevelEnabled(LogLevel.Debug),
                IsLogLevelEnabled(LogLevel.Info),
                IsLogLevelEnabled(LogLevel.Warn),
                IsLogLevelEnabled(LogLevel.Error));
        }

        /// <summary>
        /// Logs a verbose message in DEBUG builds only.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="args">Any arguments to pass to the message template.</param>
        [Conditional("DEBUG")]
        public abstract void Verbose(string messageTemplate, params object[] args);

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="args">Any arguments to pass to the message template.</param>
        public void Warn(string messageTemplate, params object[] args)
        {
            Warn(null, messageTemplate, args);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="ex">The Exception</param>
        public void Warn(Exception ex)
        {
            Warn(ex, null, null);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="ex">The Exception</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="args">Any arguments to pass to the message template.</param>
        public abstract void Warn(Exception ex, string messageTemplate, params object[] args);

        #endregion
    }
}