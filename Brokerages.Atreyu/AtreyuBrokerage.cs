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

using QuantConnect.Brokerages.Atreyu.Client.Messages;
using QuantConnect.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;
using HistoryRequest = QuantConnect.Data.HistoryRequest;
using Order = QuantConnect.Orders.Order;

namespace QuantConnect.Brokerages.Atreyu
{
    public partial class AtreyuBrokerage : Brokerage
    {
        private readonly IAlgorithm _algorithm;
        private readonly IOrderProvider _orderProvider;
        private readonly ZeroMQConnectionManager _zeroMQ;
        private readonly ISymbolMapper _symbolMapper = new AtreyuSymbolMapper();

        public AtreyuBrokerage(IAlgorithm algorithm, IDataAggregator aggregator)
            : this(Config.Get("atreyu-host"),
                Config.GetInt("atreyu-req-port"),
                Config.GetInt("atreyu-sub-port"),
                Config.Get("atreyu-username"),
                Config.Get("atreyu-password"),
                algorithm)
        { }

        public AtreyuBrokerage(
            string host,
            int reqPort,
            int subPort,
            string username,
            string password,
            IAlgorithm algorithm) : base("Atreyu")
        {
            _algorithm = algorithm;
            _orderProvider = algorithm.Transactions;

            _zeroMQ = new ZeroMQConnectionManager(host, reqPort, subPort);
            _zeroMQ.MessageRecieved += (s, e) => OnMessage(e);
        }


        public override bool IsConnected => _zeroMQ.IsConnected;

        public override List<Order> GetOpenOrders()
        {
            Log.Trace("AtreyuBrokerage.GetOpenOrders()");
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
            Log.Trace("AtreyuBrokerage.GetAccountHoldings()");
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
            Log.Trace("AtreyuBrokerage.GetCashBalance()");
            return new List<CashAmount>() { new CashAmount(1000, "USD") };
            //throw new NotImplementedException();
        }

        public override bool PlaceOrder(Order order)
        {
            var response = _zeroMQ.Send<ResponseMessage>(new NewEquityOrderMessage()
            {
                Side = "1",
                Symbol = "GOOG",
                ClOrdID = "goog1",
                OrderQty = 1,
                Price = 1,
                ExDestination = "NSDQ",
                OrdType = "2"
            });

            if (response.Status != 0)
            {
                var message = $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: {response.Text}";
                OnOrderEvent(new OrderEvent(
                        order,
                        DateTime.UtcNow,
                        OrderFee.Zero,
                        "Atreyu Order Event")
                {
                    Status = OrderStatus.Invalid
                });
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, message));
            }
            else
            {
                OnOrderEvent(new OrderEvent(
                        order,
                        Time.ParseDate(response.TransactTime),
                        OrderFee.Zero,
                        "Atreyu Order Event")
                {
                    Status = OrderStatus.Submitted
                });
                Log.Trace($"Order submitted successfully - OrderId: {order.Id}");
            }

            return true;
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

        public override IEnumerable<BaseData> GetHistory(HistoryRequest request)
        {
            throw new InvalidOperationException("Atreyu doesn't support history");
        }
    }
}
