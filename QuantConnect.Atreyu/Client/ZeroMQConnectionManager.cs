﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using QuantConnect.Atreyu.Client.Messages;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Atreyu.Client
{
    /// <summary>
    /// ZeroMQ client wrapper
    /// </summary>
    public class ZeroMQConnectionManager : IDisposable
    {
        private readonly SubscriberSocket _subscribeSocket;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private static TimeSpan _timeoutRequestResponse = TimeSpan.FromSeconds(10);
        private static TimeSpan _timeoutPublishSubscribe = TimeSpan.FromSeconds(30);

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        private readonly string _host;
        private readonly int _requestPort;
        private readonly int _subscribePort;
        private readonly string _username;
        private readonly string _password;

        private volatile bool _connected;
        private string _sessionId;

        public event EventHandler<string> MessageRecieved;

        public bool IsConnected => _connected;

        /// <summary>
        /// Creates ZeroMQ client
        /// </summary>
        /// <param name="host">Instance url</param>
        /// <param name="requestPort">Port for request/reply (REQREP) messaging pattern</param>
        /// <param name="subscribePort">Port for publish/subscribe (PUBSUB) messaging pattern</param>
        /// <param name="username">The login user name</param>
        /// <param name="password">The login password</param>
        public ZeroMQConnectionManager(string host, int requestPort, int subscribePort, string username, string password)
        {
            _subscribeSocket = new SubscriberSocket();
            _cancellationTokenSource = new CancellationTokenSource();

            _host = host;
            _requestPort = requestPort;
            _subscribePort = subscribePort;
            _username = username;
            _password = password;
        }

        /// <summary>
        /// Connects to Atreyu PUBSUB communication channel; allocate session id
        /// </summary>
        public void Connect()
        {
            // subscriber
            var token = _cancellationTokenSource.Token;
            Task.Factory.StartNew(() =>
            {
                _subscribeSocket.Connect(_host + $":{_subscribePort}");
                _subscribeSocket.SubscribeToAnyTopic();
                _resetEvent.Set();

                if (Log.DebuggingEnabled)
                {
                    Log.Debug("Subscriber socket connecting...");
                }
                while (true)
                {
                    try
                    {
                        if (token.IsCancellationRequested || !_connected)
                            break;

                        if (_subscribeSocket.TryReceiveFrameString(_timeoutPublishSubscribe, out var messageReceived))
                        {
                            OnMessageRecieved(messageReceived);
                            continue;
                        }

                        if (Log.DebuggingEnabled)
                        {
                            Log.Debug($"NetMQ.PUB-SUB: No message was received within timeout {_timeoutPublishSubscribe.ToString("c", CultureInfo.InvariantCulture)}");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"ZeroMQConnectionManager.PUBSUB(): error occurs. Message: {e.Message}");
                    }
                }
                if (Log.DebuggingEnabled)
                {
                    Log.Debug("ZeroMQConnectionManager: stopped polling messages");
                }
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            if (_resetEvent.WaitOne(_timeoutPublishSubscribe))
            {
                _connected = true;

                var response = Send<LogonResponseMessage>(new LogonMessage(_username, _password));
                if (response.Status != 0)
                {
                    throw new Exception(
                        $"AtreyuBrokerage: ZeroMQConnectionManager.Connect() could not authenticate. Error {response.Text}");
                }

                _sessionId = response.SessionId;
            }
            else
            {
                throw new Exception(
                    $"AtreyuBrokerage: ZeroMQConnectionManager.Connect() could not connect to PUBSUB communication channel. Port: {_subscribePort}");
            }
        }

        /// <summary>
        /// Disconnect from PUBSUB channel
        /// Connection is still alive, but no messages (hang).
        /// </summary>
        public void Disconnect()
        {
            _subscribeSocket?.Disconnect(_host + $":{_subscribePort}");
            _connected = false;
        }

        /// <summary>
        /// Send a request message from a RequestSocket; is blocking
        /// </summary>
        /// <param name="message">request message</param>
        /// <returns>message from the ResponseSocket</returns>
        public string Send(RequestMessage message)
        {
            if (!IsConnected)
            {
                Log.Error($"ZeroMQConnectionManager.Send(): connection has been disposed.");
                return null;
            }

            try
            {
                // request, Unit Of Work pattern
                // it can be less efficient that single client forever, but more reliable.
                using (var requestSocket = new RequestSocket())
                {
                    requestSocket.Connect(_host + $":{_requestPort}");

                    if (message is SignedMessage)
                    {
                        (message as SignedMessage).SessionId = _sessionId;
                    }

                    if (!requestSocket.TrySendFrame(
                        _timeoutRequestResponse,
                        JsonConvert.SerializeObject(message)))
                    {
                        Log.Error($"ZeroMQConnectionManager.Send(): could not send message. Content: {JsonConvert.SerializeObject(message)}");
                        return null;
                    }

                    if (!requestSocket.TryReceiveFrameString(_timeoutRequestResponse, out string response))
                    {
                        Log.Error($"ZeroMQConnectionManager.Send(): could not receive response within specified time. Timeout: {_timeoutRequestResponse.ToString("c", CultureInfo.InvariantCulture)}");
                        return null;
                    }
                    return response;
                }
            }
            catch (Exception e)
            {
                throw new Exception($"AtreyuBrokerage: request failed: ErrorMessage: {e.Message}");
            }
        }

        /// <summary>
        /// Send a request message from a RequestSocket; is blocking
        /// </summary>
        /// <typeparam name="T">expected type of response message</typeparam>
        /// <param name="message">request message</param>
        /// <returns>message from the ResponseSocket</returns>
        public T Send<T>(RequestMessage message) where T : ResponseMessage
        {
            try
            {
                var response = Send(message);
                if (!string.IsNullOrEmpty(response))
                {
                    return JsonConvert.DeserializeObject<T>(response);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"AtreyuBrokerage: request failed: ErrorMessage: {e.Message}");
            }

            return default;
        }

        private void OnMessageRecieved(string message)
        {
            MessageRecieved?.Invoke(this, message);
        }

        /// <summary>
        /// Destroys connection to Atreyu. After this operation we can't reconnect
        /// </summary>
        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _resetEvent.DisposeSafely();
            // forcibly close the connection
            _subscribeSocket?.DisposeSafely();
        }
    }
}