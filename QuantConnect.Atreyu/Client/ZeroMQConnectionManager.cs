/*
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
using Newtonsoft.Json.Linq;
using QuantConnect.Atreyu.Client.Messages;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Atreyu.Client
{
    /// <summary>
    /// ZeroMQ client wrapper
    /// </summary>
    public class ZeroMQConnectionManager : IDisposable
    {
        private SubscriberSocket _subscribeSocket;
        private static TimeSpan _timeoutRequestResponse = TimeSpan.FromSeconds(20);
        private static TimeSpan _timeoutPublishSubscribe = TimeSpan.FromSeconds(40);

        private readonly string _host;
        private int _heartBeatMonitor;
        private readonly int _requestPort;
        private readonly int _subscribePort;
        private readonly string _username;
        private readonly string _password;

        private SecurityExchangeHours _securityExchangeHours;
        private CancellationTokenSource _cancellationTokenSource;
        private volatile bool _connected;
        private string _sessionId;

        // MaxValue allows to prevent previous messages
        private int _lastMsgSeqNum = int.MaxValue;
        private int _resetMsgSeqNum = int.MaxValue;
        private bool _resetting = false;

        public event EventHandler<JObject> MessageRecieved;

        public bool IsConnected => _connected && !string.IsNullOrEmpty(_sessionId);

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

            _host = host;
            _requestPort = requestPort;
            _subscribePort = subscribePort;
            _username = username;
            _password = password;

            _cancellationTokenSource = new CancellationTokenSource();
            _securityExchangeHours = MarketHoursDatabase.FromDataFolder()
                .GetExchangeHours(Market.USA, null, SecurityType.Equity);
        }

        /// <summary>
        /// Connects to Atreyu PUBSUB communication channel; allocate session id
        /// </summary>
        public void Connect()
        {
            // subscriber
            Log.Debug("Subscriber socket connecting...");

            _subscribeSocket.Connect(_host + $":{_subscribePort}");
            _subscribeSocket.SubscribeToAnyTopic();

            Log.Debug("Subscriber socket connected");

            var token = _cancellationTokenSource.Token;
            _connected = true;

            // we start a task to consume messages
            Task.Factory.StartNew(() =>
            {
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
                        // if we are reconnecting allow some time for the 'subscribeSocket' instance to be refreshed
                        Thread.Sleep(250);
                    }
                }

                Log.Trace("ZeroMQConnectionManager: stopped polling messages");
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            // we start a stask that will be in charge of expiring and refreshing our session id
            Task.Factory.StartNew(() =>
            {
                var timeoutLoop = TimeSpan.FromMinutes(1);
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    _cancellationTokenSource.Token.WaitHandle.WaitOne(timeoutLoop);

                    try
                    {
                        if (_connected && IsExchangeOpen())
                        {
                            if (Interlocked.Increment(ref _heartBeatMonitor) > 5 || _sessionId == null)
                            {
                                Log.Error($"ZeroMQConnectionManager(): last heart beat {_heartBeatMonitor * timeoutLoop}, resetting connection...",
                                    overrideMessageFloodProtection:true);

                                try
                                {
                                    _subscribeSocket.Disconnect(_host + $":{_subscribePort}");
                                    _subscribeSocket.DisposeSafely();
                                }
                                catch
                                {
                                    // don't let it stop us from reconnecting
                                }
                                Thread.Sleep(100);

                                // create a new instance
                                _subscribeSocket = new SubscriberSocket();
                                _lastMsgSeqNum = int.MaxValue;
                                _subscribeSocket.Connect(_host + $":{_subscribePort}");
                                _subscribeSocket.SubscribeToAnyTopic();

                                // refresh our session Id
                                Logon(int.MaxValue);

                                // clear
                                Interlocked.Exchange(ref _heartBeatMonitor, 0);
                            }
                        }
                        else
                        {
                            // our session Id expires each day
                            _sessionId = null;
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
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
        /// FLIRT Logon,  initiates trading.
        /// The state information returned will include all current open orders as well as position information by account, symbol for thecurrent trading day
        /// </summary>
        /// <returns>state information on your trading for the current trading day</returns>
        public LogonResponseMessage Logon(int? start = null)
        {
            var response = Send<LogonResponseMessage>(new LogonMessage(_username, _password) { MsgSeqNum = start ?? _lastMsgSeqNum });

            if (response == null)
            {
                Log.Error("ZeroMQConnectionManager.Logon(): got null response");
            }

            Log.Trace($"ZeroMQConnectionManager.Logon(): Response {response.Text}. Status {response.Status}");
            // only throw if the exchange is open
            if (IsExchangeOpen() && response.Status != 0)
            {
                throw new Exception(
                    $"ZeroMQConnectionManager.Logon(): could not authenticate. Error {response.Text}. Status {response.Status}");
            }

            _sessionId = response.SessionId;
            return response;
        }

        /// <summary>
        /// Send a request message from a RequestSocket; is blocking
        /// </summary>
        /// <param name="message">request message</param>
        /// <returns>message from the ResponseSocket</returns>
        private string Send(RequestMessage message)
        {
            try
            {
                // request, Unit Of Work pattern
                // it can be less efficient that single client forever, but more reliable.
                using (var requestSocket = new RequestSocket())
                {
                    requestSocket.Connect(_host + $":{_requestPort}");

                    if (message is SignedMessage signedMessage)
                    {
                        if (string.IsNullOrEmpty(_sessionId))
                        {
                            Log.Error($"ZeroMQConnectionManager.Send(): Atreyu session cannot be null or empty for this request.");
                            return null;
                        }

                        signedMessage.SessionId = _sessionId;
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

        /// <summary>
        /// Destroys connection to Atreyu. After this operation we can't reconnect
        /// </summary>
        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            // forcibly close the connection
            _subscribeSocket?.DisposeSafely();
        }

        private void OnMessageRecieved(string message)
        {
            if (Log.DebuggingEnabled)
            {
                Log.Debug(message);
            }

            var token = JObject.Parse(message);

            var msgType = token.GetValue("MsgType", StringComparison.OrdinalIgnoreCase)?.Value<string>();
            if (string.IsNullOrEmpty(msgType))
            {
                throw new ArgumentException("Message type is not specified.");
            }

            if (string.Equals(msgType, "Heartbeat", StringComparison.InvariantCultureIgnoreCase))
            {
                Interlocked.Exchange(ref _heartBeatMonitor, 0);
            }

            // Atreyu Message Gateway (AMG)
            // adds a message sequence number to each message is sends on the PUB/SUB channel
            var newMsgSeqNum = token.Value<int>("MsgSeqNum");
            if (_lastMsgSeqNum == int.MaxValue || (_lastMsgSeqNum + 1 == newMsgSeqNum) || _resetMsgSeqNum <= newMsgSeqNum)
            {
                // checks that the sequence number of the next message to be processed is one greater than the last message
                _resetting = false;
                _lastMsgSeqNum = token.Value<int>("MsgSeqNum");

                if (_resetMsgSeqNum <= newMsgSeqNum)
                {
                    Log.Error($"ZeroMQConnectionManager.OnMessageRecieved(): unexpected replay sequence number, expected {_lastMsgSeqNum + 1} but was {newMsgSeqNum}");
                }
                // refresh
                _resetMsgSeqNum = int.MaxValue;
            }
            else
            {
                //If not then a Logon must re - issued re - synchronise the engine states
                if (!_resetting && (_lastMsgSeqNum + 1 < newMsgSeqNum))
                {
                    Log.Error($"ZeroMQConnectionManager.OnMessageRecieved(): unexpected sequence number, expected {_lastMsgSeqNum + 1}" +
                              $" but was {newMsgSeqNum}. Restarting session. Message: {token.ToString(Formatting.None)}");

                    // we relogin with the last sequence number we got so that any missing message is replayed
                    var response = Logon(_lastMsgSeqNum);
                    if (response == null || response.Status != 0)
                    {
                        throw new Exception("Could not re-login to Atreyu.");
                    }

                    // we keep the sequence number that caused us to reset as a safe guard in case replay doesn't work as we expect it to
                    _resetMsgSeqNum = newMsgSeqNum;
                    _resetting = true;
                }

                // drop repeated messages
                return;
            }

            MessageRecieved?.Invoke(this, token);
        }

        private bool IsExchangeOpen()
        {
            var localTime = DateTime.UtcNow.ConvertFromUtc(_securityExchangeHours.TimeZone);
            return _securityExchangeHours.IsOpen(localTime, true);
        }
    }
}
