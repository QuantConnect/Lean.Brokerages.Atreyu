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

using QuantConnect.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Api;
using QuantConnect.Atreyu.Client;
using QuantConnect.Atreyu.Client.Messages;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Util;
using HistoryRequest = QuantConnect.Data.HistoryRequest;
using Order = QuantConnect.Orders.Order;
using QuantConnect.Packets;
using QuantConnect.Securities.Equity;
using RestSharp;

namespace QuantConnect.Atreyu
{
    [BrokerageFactory(typeof(AtreyuBrokerageFactory))]
    public partial class AtreyuBrokerage : Brokerage
    {
        private readonly IOrderProvider _orderProvider;
        private readonly ZeroMQConnectionManager _zeroMQ;
        private readonly ISymbolMapper _symbolMapper;
        private readonly ISecurityProvider _securityProvider;
        private readonly LiveNodePacket _job;

        // Atreyu inputs
        private readonly string _clientId;
        private readonly string _brokerMPID;    //required for short sale transactions
        private readonly string _locateRqd;     //flag used in combination with BROKERID(5700) to indicate that Shared have located

        // Atreyu State Information
        private List<Client.Messages.Order> _orders;

        /// <summary>
        /// Checks if the ZeroMQ is connected
        /// </summary>
        public override bool IsConnected => _zeroMQ.IsConnected;

        /// <summary>
        /// Creates a new <see cref="AtreyuBrokerage"/> from the specified values retrieving data from configuration file
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="job">The job packet</param>
        public AtreyuBrokerage(IAlgorithm algorithm, LiveNodePacket job)
            : this(algorithm.Transactions, algorithm.Portfolio, job)
        { }

        /// <summary>
        /// Creates a new <see cref="AtreyuBrokerage"/> from the specified values retrieving data from configuration file
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        public AtreyuBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider, LiveNodePacket job)
            : this(Config.Get("atreyu-host"),
                Config.GetInt("atreyu-req-port", 0),
                Config.GetInt("atreyu-sub-port", 0),
                Config.Get("atreyu-username"),
                Config.Get("atreyu-password"),
                Config.Get("atreyu-client-id"),
                job.BrokerageData.ContainsKey("atreyu-broker-mpid") ? job.BrokerageData["atreyu-broker-mpid"] : Config.Get("atreyu-broker-mpid"),
                job.BrokerageData.ContainsKey("atreyu-locate-rqd") ? job.BrokerageData["atreyu-locate-rqd"] : Config.Get("atreyu-locate-rqd"),
                orderProvider,
                securityProvider,
                job)
        { }

        /// <summary>
        ///  Creates a new <see cref="AtreyuBrokerage"/> from the specified values
        /// </summary>
        /// <param name="host">Instance url</param>
        /// <param name="requestPort">Port for request/reply (REQREP) messaging pattern</param>
        /// <param name="subscribePort">Port for publish/subscribe (PUBSUB) messaging pattern</param>
        /// <param name="username">The login user name</param>
        /// <param name="password">The login password</param>
        /// <param name="clientId">Assigned by Atreyu</param>
        /// <param name="brokerMPID">Broker MPID Required for short sale transactions</param>
        /// <param name="locate">tells the broker that the client has located shares for the short sale</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="job">The job packet</param>
        public AtreyuBrokerage(
            string host,
            int requestPort,
            int subscribePort,
            string username,
            string password,
            string clientId,
            string brokerMPID,
            string locate,
            IAlgorithm algorithm,
            LiveNodePacket job) : this(host, requestPort, subscribePort, username, password, clientId, brokerMPID, locate, algorithm?.Transactions, algorithm?.Portfolio, job)
        {
        }

        /// <summary>
        ///  Creates a new <see cref="AtreyuBrokerage"/> from the specified values
        /// </summary>
        /// <param name="host">Instance url</param>
        /// <param name="requestPort">Port for request/reply (REQREP) messaging pattern</param>
        /// <param name="subscribePort">Port for publish/subscribe (PUBSUB) messaging pattern</param>
        /// <param name="username">The login user name</param>
        /// <param name="password">The login password</param>
        /// <param name="clientId">Assigned by Atreyu</param>
        /// <param name="brokerMPID">Broker MPID Required for short sale transactions</param>
        /// <param name="locate">tells the broker that the client has located shares for the short sale</param>
        /// <param name="orderProvider">The algorithm order provider</param>
        /// <param name="securityProvider">The algorithm security provider</param>
        /// <param name="job">The job packet</param>
        public AtreyuBrokerage(
            string host,
            int requestPort,
            int subscribePort,
            string username,
            string password,
            string clientId,
            string brokerMPID,
            string locate,
            IOrderProvider orderProvider,
            ISecurityProvider securityProvider,
            LiveNodePacket job) : base("Atreyu")
        {
            if (orderProvider == null)
            {
                throw new ArgumentNullException(nameof(orderProvider));
            }

            if (securityProvider == null)
            {
                throw new ArgumentNullException(nameof(securityProvider));
            }

            if (string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            _job = job;
            _clientId = clientId;
            _brokerMPID = brokerMPID;
            _locateRqd = locate;
            _orderProvider = orderProvider;
            _securityProvider = securityProvider;
            _symbolMapper = new AtreyuSymbolMapper();
            _messageHandler = new BrokerageConcurrentMessageHandler<ExecutionReport>(OnMessageImpl);

            _zeroMQ = new ZeroMQConnectionManager(host, requestPort, subscribePort, username, password);
            _zeroMQ.MessageRecieved += (s, e) => OnMessage(e);

            // call home
            ValidateSubscription();
        }

        public override List<Order> GetOpenOrders()
        {
            if (Log.DebuggingEnabled)
            {
                Log.Debug("AtreyuBrokerage.GetOpenOrders()");
            }

            if (_orders?.Any() != true)
            {
                return new List<Order>();
            }

            var result = _orders
                .Select(ConvertOrder)
                .ToList();

            return result;
        }

        public override List<Holding> GetAccountHoldings()
        {
            return GetAccountHoldings(_job.BrokerageData, (_securityProvider as SecurityPortfolioManager)?.Securities.Values);
        }

        public override List<CashAmount> GetCashBalance()
        {
            return GetCashBalance(_job.BrokerageData, (_securityProvider as SecurityPortfolioManager)?.CashBook);
        }

        public override bool PlaceOrder(Order order)
        {
            if (order.AbsoluteQuantity % 1 != 0)
            {
                throw new ArgumentException(
                    $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: Quantity has to be an Integer, but sent {order.Quantity}");
            }

            var request = new NewEquityOrderMessage()
            {
                Side = ConvertDirection(order),
                Symbol = _symbolMapper.GetBrokerageSymbol(order.Symbol),
                ClientId = _clientId,
                ClOrdID = GetNewOrdID(),
                OrderQty = (int)order.AbsoluteQuantity,
                //DeliverToCompID = "QC-CS-INET",
                TimeInForce = ConvertTimeInForce(order.TimeInForce),
                TransactTime = DateTime.UtcNow.ToString(DateFormat.FIXWithMillisecond, CultureInfo.InvariantCulture),
                Account = "DEFAULT"
            };

            if (request.Side.Equals("5") || request.Side.Equals("SELL_SHORT", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(_brokerMPID))
                {
                    throw new ArgumentNullException(nameof(_brokerMPID), "AtreyuBrokerage.PlaceOrder: Broker MPID(5700) required for short sale transactions.");
                }
                if (string.IsNullOrEmpty(_locateRqd))
                {
                    throw new ArgumentNullException(nameof(_locateRqd), "AtreyuBrokerage.PlaceOrder: LOCATERQD(114) required for short sale transactions.");
                }

                //Broker MPID Required for short sale transactions.
                request.LocateBrokerID = _brokerMPID;
                request.LocateRqd = _locateRqd;
            }

            switch (order.Type)
            {
                case OrderType.Market:
                    request.OrdType = "1";
                    break;
                case OrderType.Limit:
                    request.OrdType = "2";
                    request.Price = (order as LimitOrder)?.LimitPrice ?? order.Price;
                    break;
                case OrderType.MarketOnClose:
                    request.OrdType = "5";
                    break;
                default:
                    throw new NotSupportedException($"AtreyuBrokerage.ConvertOrderType: Unsupported order type: {order.Type}");
            }

            if (order.Type == OrderType.Limit && (order.Properties is AtreyuOrderProperties orderProperties))
            {
                if (orderProperties.PostOnly)
                {
                    request.RoutingPolicy = "P";
                }
            }

            var submitted = false;
            _messageHandler.WithLockedStream(() =>
            {
                var response = _zeroMQ.Send<SubmitResponseMessage>(request);

                if (response == null)
                {
                    throw new Exception("AtreyuBrokerage.PlaceOrder: message was not sent.");
                }

                if (response.Status == 0)
                {
                    // Atreyu status flow has an intermediate PENDING_NEW, but Lean doesn't
                    // skip order event on REQUEST-RESPONSE response
                    // wait and fire event when receive confirmation from PUBLISH-SUBSCRIBE
                    order.BrokerId.Add(response.ClOrdID);
                    Log.Trace($"Create Order request submitted successfully at {response.SendingTime}- OrderId: {order.Id}");
                    submitted = true;
                }
                else
                {
                    var message =
                        $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: {response.Text}";
                    OnOrderEvent(new OrderEvent(
                        order,
                        Time.ParseFIXUtcTimestamp(response.SendingTime),
                        OrderFee.Zero,
                        "Atreyu Order Event")
                    {
                        Status = OrderStatus.Invalid
                    });
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, message));
                }
            });
            return submitted;
        }

        public override bool UpdateOrder(Order order)
        {
            if (order.BrokerId.Count == 0)
            {
                throw new ArgumentNullException(nameof(order.BrokerId), "AtreyuBrokerage.UpdateOrder: There is no brokerage id to be updated for this order.");
            }

            if (order.AbsoluteQuantity % 1 != 0)
            {
                throw new ArgumentException(
                    $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: Quantity has to be an Integer, but sent {order.Quantity}");
            }

            var request = new CancelReplaceEquityOrderMessage()
            {
                ClientId = _clientId,
                ClOrdID = GetNewOrdID(),
                OrderQty = (int)order.AbsoluteQuantity,
                OrigClOrdID = order.BrokerId.Last(),
                TransactTime = DateTime.UtcNow.ToString(DateFormat.FIXWithMillisecond, CultureInfo.InvariantCulture),
            };

            if (order.Type == OrderType.Limit)
            {
                request.Price = (order as LimitOrder)?.LimitPrice ?? order.Price;
            }

            var submitted = false;
            _messageHandler.WithLockedStream(() =>
            {
                var response = _zeroMQ.Send<SubmitResponseMessage>(request);

                if (response == null)
                {
                    throw new Exception("AtreyuBrokerage.UpdateOrder: message was not sent.");
                }

                if (response.Status == 0)
                {
                    // Atreyu status flow has an intermediate PENDING_REPLACE, but Lean doesn't
                    // skip order event on REQUEST-RESPONSE response
                    // wait and fire event when receive confirmation from PUBLISH-SUBSCRIBE
                    // OrigClOrdID - ClOrdID of the previous order (not necessarily the initial order)
                    // keep all chain
                    order.BrokerId.Add(response.ClOrdID);
                    Log.Trace($"Replace submitted successfully - OrderId: {order.Id}");
                    submitted = true;
                }
                else
                {
                    var message = $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: {response.Text}";
                    OnOrderEvent(new OrderEvent(
                        order,
                        Time.ParseFIXUtcTimestamp(response.SendingTime),
                        OrderFee.Zero,
                        "Atreyu Order Event")
                    {
                        Status = OrderStatus.Invalid
                    });
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, message));
                }
            });

            return submitted;
        }

        public override bool CancelOrder(Order order)
        {
            if (!order.BrokerId.Any())
            {
                // we need the brokerage order id in order to perform a cancellation
                Log.Trace("AtreyuBrokerage.CancelOrder(): Unable to cancel order without BrokerId.");
                return false;
            }

            var submitted = false;
            _messageHandler.WithLockedStream(() =>
            {
                var response = _zeroMQ.Send<SubmitResponseMessage>(new CancelEquityOrderMessage()
                {
                    ClientId = _clientId,
                    ClOrdID = GetNewOrdID(),
                    OrigClOrdID = order.BrokerId.Last(),
                    TransactTime = DateTime.UtcNow.ToString(DateFormat.FIXWithMillisecond, CultureInfo.InvariantCulture),
                });

                if (response == null)
                {
                    throw new Exception("AtreyuBrokerage.CancelOrder: message was not sent.");
                }

                if (response.Status == 0)
                {
                    Log.Trace($"Cancel submitted successfully - OrderId: {order.Id}");
                    submitted = true;
                }
                else
                {
                    var message =
                        $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: {response.Text}";
                    OnOrderEvent(new OrderEvent(
                        order,
                        Time.ParseFIXUtcTimestamp(response.SendingTime),
                        OrderFee.Zero,
                        "Atreyu Order Event")
                    {
                        Status = OrderStatus.Invalid
                    });
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, message));
                }
            });
            return submitted;
        }

        public override void Connect()
        {
            if (!_zeroMQ.IsConnected)
            {
                _zeroMQ.Connect();
                // will throw if it fails only during normal market hours
                var response = _zeroMQ.Logon();
                // logon can fail outside market hours, which is expected but we don't want to fail because of it
                if (response != null && response.Status == 0)
                {
                    _orders = response.Orders.ToList();
                }
            }
        }

        public override void Disconnect()
        {
            _zeroMQ.Disconnect();
            _zeroMQ.DisposeSafely();
        }

        public override IEnumerable<BaseData> GetHistory(HistoryRequest request)
        {
            throw new InvalidOperationException("Atreyu doesn't support history");
        }

        private class ModulesReadLicenseRead : Api.RestResponse
        {
            [JsonProperty(PropertyName = "license")]
            public string License;
            [JsonProperty(PropertyName = "organizationId")]
            public string OrganizationId;
        }

        /// <summary>
        /// Validate the user of this project has permission to be using it via our web API.
        /// </summary>
        private static void ValidateSubscription()
        {
            try
            {
                var productId = 65;
                var userId = Config.GetInt("job-user-id");
                var token = Config.Get("api-access-token");
                var organizationId = Config.Get("job-organization-id", null);
                // Verify we can authenticate with this user and token
                var api = new ApiConnection(userId, token);
                if (!api.Connected)
                {
                    throw new ArgumentException("Invalid api user id or token, cannot authenticate subscription.");
                }
                // Compile the information we want to send when validating
                var information = new Dictionary<string, object>()
                {
                    {"productId", productId},
                    {"machineName", Environment.MachineName},
                    {"userName", Environment.UserName},
                    {"domainName", Environment.UserDomainName},
                    {"os", Environment.OSVersion}
                };
                // IP and Mac Address Information
                try
                {
                    var interfaceDictionary = new List<Dictionary<string, object>>();
                    foreach (var nic in NetworkInterface.GetAllNetworkInterfaces().Where(nic => nic.OperationalStatus == OperationalStatus.Up))
                    {
                        var interfaceInformation = new Dictionary<string, object>();
                        // Get UnicastAddresses
                        var addresses = nic.GetIPProperties().UnicastAddresses
                            .Select(uniAddress => uniAddress.Address)
                            .Where(address => !IPAddress.IsLoopback(address)).Select(x => x.ToString());
                        // If this interface has non-loopback addresses, we will include it
                        if (!addresses.IsNullOrEmpty())
                        {
                            interfaceInformation.Add("unicastAddresses", addresses);
                            // Get MAC address
                            interfaceInformation.Add("MAC", nic.GetPhysicalAddress().ToString());
                            // Add Interface name
                            interfaceInformation.Add("name", nic.Name);
                            // Add these to our dictionary
                            interfaceDictionary.Add(interfaceInformation);
                        }
                    }
                    information.Add("networkInterfaces", interfaceDictionary);
                }
                catch (Exception)
                {
                    // NOP, not necessary to crash if fails to extract and add this information
                }
                // Include our OrganizationId is specified
                if (!string.IsNullOrEmpty(organizationId))
                {
                    information.Add("organizationId", organizationId);
                }
                var request = new RestRequest("modules/license/read", Method.POST) { RequestFormat = DataFormat.Json };
                request.AddParameter("application/json", JsonConvert.SerializeObject(information), ParameterType.RequestBody);
                api.TryRequest(request, out ModulesReadLicenseRead result);
                if (!result.Success)
                {
                    throw new InvalidOperationException($"Request for subscriptions from web failed, Response Errors : {string.Join(',', result.Errors)}");
                }

                var encryptedData = result.License;
                // Decrypt the data we received
                DateTime? expirationDate = null;
                long? stamp = null;
                bool? isValid = null;
                if (encryptedData != null)
                {
                    // Fetch the org id from the response if we are null, we need it to generate our validation key
                    if (string.IsNullOrEmpty(organizationId))
                    {
                        organizationId = result.OrganizationId;
                    }
                    // Create our combination key
                    var password = $"{token}-{organizationId}";
                    var key = SHA256.HashData(Encoding.UTF8.GetBytes(password));
                    // Split the data
                    var info = encryptedData.Split("::");
                    var buffer = Convert.FromBase64String(info[0]);
                    var iv = Convert.FromBase64String(info[1]);
                    // Decrypt our information
                    using var aes = new AesManaged();
                    var decryptor = aes.CreateDecryptor(key, iv);
                    using var memoryStream = new MemoryStream(buffer);
                    using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                    using var streamReader = new StreamReader(cryptoStream);
                    var decryptedData = streamReader.ReadToEnd();
                    if (!decryptedData.IsNullOrEmpty())
                    {
                        var jsonInfo = JsonConvert.DeserializeObject<JObject>(decryptedData);
                        expirationDate = jsonInfo["expiration"]?.Value<DateTime>();
                        isValid = jsonInfo["isValid"]?.Value<bool>();
                        stamp = jsonInfo["stamped"]?.Value<int>();
                    }
                }
                // Validate our conditions
                if (!expirationDate.HasValue || !isValid.HasValue || !stamp.HasValue)
                {
                    throw new InvalidOperationException("Failed to validate subscription.");
                }

                var nowUtc = DateTime.UtcNow;
                var timeSpan = nowUtc - Time.UnixTimeStampToDateTime(stamp.Value);
                if (timeSpan > TimeSpan.FromHours(12))
                {
                    throw new InvalidOperationException("Invalid API response.");
                }
                if (!isValid.Value)
                {
                    throw new ArgumentException($"Your subscription is not valid, please check your product subscriptions on our website.");
                }
                if (expirationDate < nowUtc)
                {
                    throw new ArgumentException($"Your subscription expired {expirationDate}, please renew in order to use this product.");
                }
            }
            catch (Exception e)
            {
                Log.Error($"ValidateSubscription(): Failed during validation, shutting down. Error : {e.Message}");
                Environment.Exit(1);
            }
        }
    }
}
