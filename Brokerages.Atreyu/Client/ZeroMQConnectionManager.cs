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
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using QuantConnect.Brokerages.Atreyu.Client.Messages;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Atreyu
{
    public class ZeroMQConnectionManager : IDisposable
    {
        private readonly SubscriberSocket _subscribeSocket;
        private CancellationTokenSource _cancellationTokenSource;

        private readonly string _host;
        private readonly int _reqPort;
        private readonly int _subPort;

        private bool _connected;

        public event EventHandler<string> MessageRecieved;

        public bool IsConnected => _connected && !_subscribeSocket.IsDisposed;

        public ZeroMQConnectionManager(string host, int reqPort, int subPort)
        {
            _subscribeSocket = new SubscriberSocket();
            _cancellationTokenSource = new CancellationTokenSource();

            _host = host;
            _reqPort = reqPort;
            _subPort = subPort;
        }

        public void Connect()
        {
            // subscriber
            var token = _cancellationTokenSource.Token;
            Task.Factory.StartNew(() =>
            {
                _subscribeSocket.Connect(_host + $":{_subPort}");
                _subscribeSocket.Subscribe(string.Empty); // subscribe to everything

                Log.Trace("Subscriber socket connecting...");
                while (true)
                {
                    string messageReceived = _subscribeSocket.ReceiveFrameString();
                    OnMessageRecieved(messageReceived);

                    if (token.IsCancellationRequested) break;
                }
                Log.Trace($"ZeroMQConnectionManager: stopped polling messages");
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            var response = Send<ResponseMessage>(new LogonMessage());
            _connected = true;
        }

        public void Disconnect()
        {
            _subscribeSocket?.Disconnect(_host + $":{_subPort}");
        }

        public string Send(RequestMessage message)
        {
            try
            {
                // request, Unit Of Work pattern
                // it can be less efficient that single client forever, but more reliable.
                using (var requestSocket = new RequestSocket())
                {
                    requestSocket.Connect(_host + $":{_reqPort}");
                    requestSocket.SendFrame(JsonConvert.SerializeObject(message));
                    var response = requestSocket.ReceiveFrameString();
                    return response;
                }
            }
            catch (Exception e)
            {
                throw new Exception($"AtreyuBrokerage: request failed: ErrorMessage: {e.Message}");
            }
        }

        public T Send<T>(RequestMessage message)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(Send(message));
            }
            catch (Exception e)
            {
                throw new Exception($"AtreyuBrokerage: request failed: ErrorMessage: {e.Message}");
            }
        }

        private void OnMessageRecieved(string message)
        {
            MessageRecieved?.Invoke(this, message);
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _subscribeSocket?.DisposeSafely();
        }
    }
}
