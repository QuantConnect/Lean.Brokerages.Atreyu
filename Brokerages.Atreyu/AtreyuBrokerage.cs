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
            var response = _zeroMQ.Send<OpenOrdersResultMessage>(new QueryOpenOrdersMessage());
            if (response.Status != 0)
            {
                throw new Exception($"AtreyuBrokerage.GetOpenOrders: request failed: [{(int)response.Status}] ErrorMessage: {response.Text}");
            }

            if (response.Orders?.Any() != true)
            {
                return new List<Order>();
            }

            var result = response.Orders
                .Select(ConvertOrder)
                .ToList();

            return result;
        }

        public override List<Holding> GetAccountHoldings()
        {
            Log.Trace("AtreyuBrokerage.GetAccountHoldings()");
            var response = _zeroMQ.Send<OpenPositionsResultMessage>(new QueryPositionsMessage());
            if (response.Status != 0)
            {
                throw new Exception($"AtreyuBrokerage.GetAccountHoldings: request failed: [{(int)response.Status}] ErrorMessage: {response.Text}");
            }

            if (response.Positions?.Any() != true)
            {
                return new List<Holding>();
            }

            return response.Positions
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
            if ((order.Quantity % 1) != 0)
            {
                throw new ArgumentException(
                    $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: Quantity has to be an Integer, but sent {order.Quantity}");
            }

            var request = new NewEquityOrderMessage()
            {
                Side = ConvertDirection(order.Direction),
                Symbol = _symbolMapper.GetBrokerageSymbol(order.Symbol),
                ClOrdID = order.Id.ToString(),
                OrderQty = (int)order.Quantity,
                // DeliverToCompID = "CS", // exclude for testing purposes
                ExDestination = "NSDQ",
                ExecInst = "1",
                HandlInst = "1",
                TransactTime = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff")
            };

            switch (order.Type)
            {
                case OrderType.Market:
                    request.OrdType = "1";
                    break;
                case OrderType.Limit:
                    request.OrdType = "2";
                    request.Price = (order as LimitOrder)?.LimitPrice ?? order.Price;
                    break;
                case OrderType.StopMarket:
                    request.OrdType = "3";
                    break;
                case OrderType.StopLimit:
                    request.OrdType = "4";
                    break;
                default:
                    throw new NotSupportedException($"AtreyuBrokerage.ConvertOrderType: Unsupported order type: {order.Type}");
            }

            bool submitted = false;
            WithLockedStream(() =>
            {
                var response = _zeroMQ.Send<ResponseMessage>(request);

                if (response.Status != 0)
                {
                    var message =
                        $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: {response.Text}";
                    OnOrderEvent(new OrderEvent(
                        order,
                        DateTime.UtcNow,
                        OrderFee.Zero,
                        "Atreyu Order Event")
                    {
                        Status = OrderStatus.Invalid
                    });
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, message));
                    submitted = false;
                }
                else
                {
                    order.BrokerId.Add(request.ClOrdID);
                    OnOrderEvent(new OrderEvent(
                        order,
                        Time.ParseDate(response.SendingTime),
                        OrderFee.Zero,
                        "Atreyu Order Event")
                    {
                        Status = OrderStatus.Submitted
                    });
                    Log.Trace($"Order submitted successfully - OrderId: {order.Id}");
                    submitted = true;
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

            if (order.BrokerId.Count > 1)
            {
                throw new NotSupportedException("AtreyuBrokerage.UpdateOrder: Multiple orders update not supported. Please cancel and re-create.");
            }

            if ((order.Quantity % 1) != 0)
            {
                throw new ArgumentException(
                    $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: Quantity has to be an Integer, but sent {order.Quantity}");
            }

            var response = _zeroMQ.Send<ResponseMessage>(new CancelReplaceEquityOrderMessage()
            {
                ClOrdID = Guid.NewGuid().ToString("N"),
                OrderQty = 1,
                Price = 1200,
                OrigClOrdID = order.BrokerId.First(),
                TransactTime = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff")
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
                    Time.ParseDate(response.SendingTime),
                    OrderFee.Zero,
                    "Atreyu Order Event")
                {
                    Status = OrderStatus.Submitted
                });
                Log.Trace($"Order submitted successfully - OrderId: {order.Id}");
            }

            return true;
        }

        public override bool CancelOrder(Order order)
        {
            Log.Trace("AtreyuBrokerage.CancelOrder(): {0}", order);

            if (!order.BrokerId.Any())
            {
                // we need the brokerage order id in order to perform a cancellation
                Log.Trace("AtreyuBrokerage.CancelOrder(): Unable to cancel order without BrokerId.");
                return false;
            }

            var response = _zeroMQ.Send<ResponseMessage>(new CancelEquityOrderMessage()
            {
                ClOrdID = Guid.NewGuid().ToString("N"),
                OrigClOrdID = order.BrokerId.First(),
                TransactTime = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff")
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
                    Time.ParseDate(response.SendingTime),
                    OrderFee.Zero,
                    "Atreyu Order Event")
                {
                    Status = OrderStatus.Canceled
                });
                Log.Trace($"Order submitted successfully - OrderId: {order.Id}");
            }

            return true;
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
