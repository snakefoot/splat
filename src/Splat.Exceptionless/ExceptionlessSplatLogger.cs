﻿// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Exceptionless;

namespace Splat.Exceptionless
{
    /// <summary>
    /// Exceptionless Logger into Splat.
    /// </summary>
    [DebuggerDisplay("Name={_sourceType} Level={Level}")]
    public sealed class ExceptionlessSplatLogger : ILogger
    {
        private static readonly KeyValuePair<LogLevel, global::Exceptionless.Logging.LogLevel>[] _mappings = new[]
        {
            new KeyValuePair<LogLevel, global::Exceptionless.Logging.LogLevel>(LogLevel.Debug, global::Exceptionless.Logging.LogLevel.Debug),
            new KeyValuePair<LogLevel, global::Exceptionless.Logging.LogLevel>(LogLevel.Info, global::Exceptionless.Logging.LogLevel.Info),
            new KeyValuePair<LogLevel, global::Exceptionless.Logging.LogLevel>(LogLevel.Warn, global::Exceptionless.Logging.LogLevel.Warn),
            new KeyValuePair<LogLevel, global::Exceptionless.Logging.LogLevel>(LogLevel.Error, global::Exceptionless.Logging.LogLevel.Error),
            new KeyValuePair<LogLevel, global::Exceptionless.Logging.LogLevel>(LogLevel.Fatal, global::Exceptionless.Logging.LogLevel.Fatal)
        };

        private static readonly ImmutableDictionary<LogLevel, global::Exceptionless.Logging.LogLevel> _mappingsDictionary = _mappings.ToImmutableDictionary();

        private readonly string _sourceType;
        private readonly ExceptionlessClient _exceptionlessClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionlessSplatLogger"/> class.
        /// </summary>
        /// <param name="sourceType">The type being tracked.</param>
        /// <param name="exceptionlessClient">The exceptionless client instance to use.</param>
        public ExceptionlessSplatLogger(
            Type sourceType,
            ExceptionlessClient exceptionlessClient)
        {
            _sourceType = sourceType.FullName;
            _exceptionlessClient = exceptionlessClient ?? throw new ArgumentNullException(nameof(exceptionlessClient));
            _exceptionlessClient.Configuration.Changed += OnInnerLoggerReconfigured;

            if (_exceptionlessClient.Configuration.Settings.TryGetValue("@@log:*", out var logLevel))
            {
                var l = global::Exceptionless.Logging.LogLevel.FromString(logLevel);
                Level = _mappingsDictionary.First(x => x.Value == l).Key;
            }
        }

        /// <inheritdoc />
        public LogLevel Level { get; private set; }

        /// <inheritdoc />
        public void Write(string message, LogLevel logLevel)
        {
            if ((int)logLevel < (int)Level)
            {
                return;
            }

            CreateLog(message, _mappingsDictionary[logLevel]);
        }

        /// <inheritdoc />
        public void Write(Exception exception, string message, LogLevel logLevel)
        {
            if ((int)logLevel < (int)Level)
            {
                return;
            }

            CreateLog(exception, message, _mappingsDictionary[logLevel]);
        }

        /// <inheritdoc />
        public void Write(string message, Type type, LogLevel logLevel)
        {
            if ((int)logLevel < (int)Level)
            {
                return;
            }

            CreateLog(type.FullName, message, _mappingsDictionary[logLevel]);
        }

        /// <inheritdoc />
        public void Write(Exception exception, string message, Type type, LogLevel logLevel)
        {
            if ((int)logLevel < (int)Level)
            {
                return;
            }

            CreateLog(exception, type.FullName, message, _mappingsDictionary[logLevel]);
        }

        private void CreateLog(string message, global::Exceptionless.Logging.LogLevel level)
        {
            CreateLog(_sourceType, message, level);
        }

        private void CreateLog(string type, string message, global::Exceptionless.Logging.LogLevel level)
        {
            _exceptionlessClient.SubmitLog(type, message, level);
            _exceptionlessClient.ProcessQueue();
        }

        private void CreateLog(Exception exception, string message, global::Exceptionless.Logging.LogLevel level)
        {
            CreateLog(exception, _sourceType, message, level);
        }

        private void CreateLog(Exception exception, string type, string message, global::Exceptionless.Logging.LogLevel level)
        {
            _exceptionlessClient.CreateLog(
                type,
                message,
                level)
                .SetException(exception)
                .Submit();

            _exceptionlessClient.ProcessQueue();
        }

        /// <summary>
        /// Works out the log level.
        /// </summary>
        /// <remarks>
        /// This was done so the Level property doesn't keep getting re-evaluated each time a Write method is called.
        /// </remarks>
        private void SetLogLevel()
        {
            /*
            if (_inner.IsDebugEnabled)
            {
                Level = LogLevel.Debug;
                return;
            }

            if (_inner.IsInfoEnabled)
            {
                Level = LogLevel.Info;
                return;
            }

            if (_inner.IsWarnEnabled)
            {
                Level = LogLevel.Warn;
                return;
            }

            if (_inner.IsErrorEnabled)
            {
                Level = LogLevel.Error;
                return;
            }
            */

            Level = LogLevel.Fatal;
        }

        private void OnInnerLoggerReconfigured(object sender, EventArgs e)
        {
            SetLogLevel();
        }
    }
}
