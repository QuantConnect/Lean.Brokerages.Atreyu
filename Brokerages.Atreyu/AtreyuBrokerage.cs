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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Brokerages.Atreyu.Client.Messages;
using QuantConnect.Util;
using Order = QuantConnect.Orders.Order;

namespace QuantConnect.Brokerages.Atreyu
{
    public partial class AtreyuBrokerage : Brokerage, IDataQueueHandler
    {
        private readonly IAlgorithm _algorithm;
        private readonly IDataAggregator _aggregator;
        private readonly IOrderProvider _orderProvider;
        private readonly ZeroMQConnectionManager _zeroMQ;
        private readonly ISymbolMapper _symbolMapper = new AtreyuSymbolMapper();

        /// <summary>
        /// Count subscribers for each (symbol, tickType) combination
        /// </summary>
        private EventBasedDataQueueHandlerSubscriptionManager _subscriptionManager;

        public AtreyuBrokerage(IAlgorithm algorithm, IDataAggregator aggregator)
            : this(Config.Get("atreyu-host"),
                Config.GetInt("atreyu-req-port"),
                Config.GetInt("atreyu-sub-port"),
                Config.Get("atreyu-username"),
                Config.Get("atreyu-password"),
                algorithm,
                aggregator)
        { }

        public AtreyuBrokerage(
            string host,
            int reqPort,
            int subPort,
            string username,
            string password,
            IAlgorithm algorithm,
            IDataAggregator aggregator) : base("Atreyu")
        {
            _algorithm = algorithm;
            _aggregator = aggregator;
            _orderProvider = algorithm.Transactions;

            _subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();
            _subscriptionManager.SubscribeImpl += (s, t) => Subscribe(s);
            _subscriptionManager.UnsubscribeImpl += (s, t) => Unsubscribe(s);

            _zeroMQ = new ZeroMQConnectionManager(host, reqPort, subPort);
            _zeroMQ.MessageRecieved += (s, e) => OnMessage(e);
        }

        /// <summary>
        /// Subscribe to the specified configuration
        /// </summary>
        /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        /// <returns>The new enumerator for this subscription request</returns>
        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            if (!CanSubscribe(dataConfig.Symbol))
            {
                return Enumerable.Empty<BaseData>().GetEnumerator();
            }

            var enumerator = _aggregator.Add(dataConfig, newDataAvailableHandler);
            _subscriptionManager.Subscribe(dataConfig);

            return enumerator;
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        private bool Subscribe(IEnumerable<Symbol> symbols)
        {
            throw new NotImplementedException();
        }

        private bool CanSubscribe(Symbol dataConfigSymbol)
        {
            return !dataConfigSymbol.Value.Contains("UNIVERSE") && dataConfigSymbol.SecurityType == SecurityType.Equity;
        }

        /// <summary>
        /// Removes the specified configuration
        /// </summary>
        /// <param name="dataConfig">Subscription config to be removed</param>
        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            _subscriptionManager.Unsubscribe(dataConfig);
            _aggregator.Remove(dataConfig);
        }

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        private bool Unsubscribe(IEnumerable<Symbol> symbols)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the job we're subscribing for
        /// </summary>
        /// <param name="job">Job we're subscribing for</param>
        public void SetJob(LiveNodePacket job) { }

        public override bool IsConnected => _zeroMQ.IsConnected;

        public override List<Order> GetOpenOrders()
        {
            var response = _zeroMQ.Send<QueryResultMessage<Client.Messages.Order>>(new QueryOpenOrdersMessage());
            if (response.Status != 0)
            {
                throw new Exception($"AtreyuBrokerage.GetOpenOrders: request failed: [{(int)response.Status}] ErrorMessage: {response.Text}");
            }

            if (response.Result?.Any() != true)
            {
                return new List<Order>();
            }

            return response.Result
                .Select(ConvertOrder)
                .ToList();
        }

        public override List<Holding> GetAccountHoldings()
        {
            var response = _zeroMQ.Send<QueryResultMessage<Client.Messages.Position>>(new QueryPositionsMessage());
            if (response.Status != 0)
            {
                throw new Exception($"AtreyuBrokerage.GetAccountHoldings: request failed: [{(int)response.Status}] ErrorMessage: {response.Text}");
            }

            if (response.Result?.Any() != true)
            {
                return new List<Holding>();
            }

            return response.Result
                .Select(ConvertHolding)
                .ToList();
        }

        public override List<CashAmount> GetCashBalance()
        {
            return new List<CashAmount>();
            //throw new NotImplementedException();
        }

        public override bool PlaceOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override bool UpdateOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override bool CancelOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override void Connect()
        {
            if (!_zeroMQ.IsConnected)
                _zeroMQ.Connect();
        }

        public override void Disconnect()
        {
            _zeroMQ.Disconnect();
            _zeroMQ.DisposeSafely();
        }
    }
}
