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
using System.Linq;
using Newtonsoft.Json;
using QuantConnect.Orders;
using QuantConnect.Logging;
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages;
using QuantConnect.Orders.Fees;
using QuantConnect.Atreyu.Client.Messages;

namespace QuantConnect.Atreyu
{
    public partial class AtreyuBrokerage
    {
        private readonly BrokerageConcurrentMessageHandler<ExecutionReport> _messageHandler;
        private readonly string[] _notMappedStatuses = { "PENDING_REPLACE" };

        public void OnMessage(JObject token)
        {
            ExecutionReport report = null;

            // we can ignore not-execution messages
            if (token.TryGetValue("ExecType", StringComparison.OrdinalIgnoreCase, out _)
                || token.TryGetValue("CxlRejReason", StringComparison.OrdinalIgnoreCase, out _))
            {
                report = token.ToObject<ExecutionReport>();
            }

            _messageHandler.HandleNewMessage(report);
        }

        private void OnMessageImpl(ExecutionReport report)
        {
            try
            {
                switch (report)
                {
                    case FillOrderReport fill:
                        OnOrderFill(fill);
                        break;
                    case OrderCancelRejectReport reject:
                        OnCancelRejected(reject);
                        break;
                    case ExecutionReport execution:
                        OnExecution(execution);
                        break;
                    default:
                        throw new InvalidOperationException($"AtreyuBrokerage: execution type is not supported; received {report.ExecType}");
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void OnExecution(ExecutionReport report)
        {
            if (_notMappedStatuses.Contains(report.ExecType, StringComparer.OrdinalIgnoreCase))
            {
                // do nothing specific;
                // Lean doesn't have OrderStatus.Updated
                // we map UpdateSubmitted to Atreyu.Replaced
                return;
            }

            var atreyuOrderId = report.OrigClOrdID ?? report.ClOrdID;
            var order = _orderProvider.GetOrderByBrokerageId(atreyuOrderId);
            if (order != null)
            {
                OnOrderEvent(new OrderEvent(order, Time.ParseFIXUtcTimestamp(report.TransactTime), OrderFee.Zero, $"Atreyu Order Event. Message: {report.Text}")
                {
                    Status = ConvertOrderStatus(report.OrdStatus)
                });

                if (ConvertExecType(report.ExecType) == OrderStatus.Submitted)
                {
                    _orders.Add(
                        new Client.Messages.Order()
                        {
                            Symbol = order.Symbol.Value,
                            TransactTime = report.TransactTime,
                            OrdType = order.Type == OrderType.MarketOnClose
                                ? "MARKETONCLOSE"
                                : order.Type == OrderType.Limit
                                    ? "LIMIT"
                                    : "MARKET",
                            Side = ConvertDirection(order),
                            OrderQty = (int)order.AbsoluteQuantity,
                            Price = order.Price,
                            ClOrdID = order.BrokerId.First(),
                            TimeInForce = ConvertTimeInForce(order.TimeInForce),
                            OrdStatus = "NEW"

                        });
                }
                else if (ConvertExecType(report.ExecType) == OrderStatus.Canceled)
                {
                    _orders = _orders
                        .Where(o => !order.BrokerId.Contains(o.ClOrdID))
                        .ToList();
                }
            }
        }

        private void OnOrderFill(FillOrderReport report)
        {
            if (report == null)
            {
                throw new ArgumentException($"Received unexpected filling report format. Content: {JsonConvert.SerializeObject(report)}");
            }

            var atreyuOrderId = report.OrigClOrdID ?? report.ClOrdID;
            var order = _orderProvider.GetOrderByBrokerageId(atreyuOrderId);
            try
            {
                if (order == null)
                {
                    // not our order, nothing else to do here
                    return;
                }

                var security = _securityProvider.GetSecurity(order.Symbol);
                var fillPrice = report.LastPx;
                var fillQuantity = order.Direction == OrderDirection.Sell ? -report.LastShares : report.LastShares;
                var updTime = Time.ParseFIXUtcTimestamp(report.TransactTime);
                var status = ConvertOrderStatus(report.OrdStatus);
                var orderFee = OrderFee.Zero;
                if (report.LastShares != 0)
                {
                    // create a new order just with the filled shares 'report.LastShares', so partial fills have fees
                    var reducedOrder = new MarketOrder(order.Symbol, report.LastShares, order.Time, order.Tag, order.Properties);
                    orderFee = security.FeeModel.GetOrderFee(new OrderFeeParameters(security, reducedOrder));
                }
                var orderEvent = new OrderEvent
                (
                    order.Id, order.Symbol, updTime, status,
                    order.Direction, fillPrice, fillQuantity,
                    orderFee, $"Atreyu Order Event {order.Direction}"
                );

                OnOrderEvent(orderEvent);
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        /// <summary>
        /// If rejected by trader/exchange
        /// </summary>
        /// <param name="report"></param>
        private void OnCancelRejected(OrderCancelRejectReport report)
        {
            var atreyuOrderId = report.OrigClOrdID ?? report.ClOrdID;
            var order = _orderProvider.GetOrderByBrokerageId(atreyuOrderId);
            if (order != null)
            {
                order.BrokerId.Remove(report.ClOrdID);
                OnOrderEvent(new OrderEvent(order, Time.ParseFIXUtcTimestamp(report.TransactTime), OrderFee.Zero,
                    $"Atreyu Order Event. Message: {report.Text}, reason: {report.CxlRejReason}.")
                {
                    Status = ConvertOrderStatus(report.OrdStatus)
                });
            }
        }
    }
}
