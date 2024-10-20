using Microsoft.Extensions.Logging;

using System;
using System.Collections;
using System.Collections.Generic;

using Willow.Common;

namespace Willow.Platform.Mocks
{
    public class FakeLoggerFactory : ILoggerFactory
    {
        private readonly FakeLogger _fakeLogger = new();

        public void AddProvider(ILoggerProvider provider)
        {
            // Not supported
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _fakeLogger;
        }

        public FakeLogger GetFakeLogger()
        {
            return _fakeLogger;
        }

        public void Dispose()
        {
            // Nothing to do
        }
    }

    public class FakeLogger : ILogger
    {
        public List<LogEntry> Entries { get; set; } = new List<LogEntry>();
        public Stack<IDictionary<string, object>> Stack { get; set; } = new Stack<IDictionary<string, object>>();

        public FakeLogger()
        {
            this.Stack.Push(new Dictionary<string, object>());
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            if (state is IDictionary<string, object> props)
                this.Stack.Push(props);

            return new FakeDisposable();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var props = state?.ToDictionary() ?? new Dictionary<string, object>();
            var scope = this.Stack.Peek() as IDictionary;

            if(scope == null)
                scope = props as IDictionary;
             else
                scope.Merge(props as IDictionary);

            var entry = new LogEntry { Message = formatter(state, exception) };

            (entry.Properties as IDictionary).Merge(scope);

            this.Entries.Add(entry);
        }

        public class LogEntry
        {
            public string Message { get; set; } = "";
            public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        }
    }

    public class FakeDisposable : IDisposable
    {
        public void Dispose()
        {
            // Do nothing
        }
    }
}
