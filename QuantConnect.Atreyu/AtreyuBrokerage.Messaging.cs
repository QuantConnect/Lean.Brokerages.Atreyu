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

using Newtonsoft.Json;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using System;
using System.Collections.Concurrent;
using System.Linq;
using Newtonsoft.Json.Linq;
using QuantConnect.Atreyu.Client.Messages;

namespace QuantConnect.Atreyu
{
    public partial class AtreyuBrokerage
    {
        private volatile bool _streamLocked;
        private readonly ConcurrentQueue<ExecutionReport> _messageBuffer = new ConcurrentQueue<ExecutionReport>();
        private readonly string[] _notMappedStatuses = { "PENDING_REPLACE" };

        // MaxValue allows to prevent previous messages
        private int _lastMsgSeqNum = int.MaxValue;
        private bool _resetting = false;
        public void OnMessage(string message)
        {
            var token = JObject.Parse(message);
            var msgType = token.GetValue("MsgType", StringComparison.OrdinalIgnoreCase)?.Value<string>();
            if (string.IsNullOrEmpty(msgType))
            {
                throw new ArgumentException("Message type is not specified.");
            }

            if (Log.DebuggingEnabled)
            {
                Log.Debug(message);
            }

            // Atreyu Message Gateway (AMG)
            // adds a message sequence number to each message is sends on the PUB/SUB channel
            var newMsgSeqNum = token.Value<int>("MsgSeqNum");
            if (_lastMsgSeqNum == int.MaxValue || (_lastMsgSeqNum + 1 == newMsgSeqNum))
            {
                // checks that the sequence number of the next message to be processed is one greater than the last message
                _resetting = false;
                _lastMsgSeqNum = token.Value<int>("MsgSeqNum");
            }
            else
            {
                //If not then a Logon must re - issued re - synchronise the engine states
                if (!_resetting && (_lastMsgSeqNum + 1 < newMsgSeqNum))
                {
                    var response = _zeroMQ.Logon(_lastMsgSeqNum);
                    if (response.Status != 0)
                    {
                        throw new Exception("Could not re-login to Atreyu.");
                    }

                    _resetting = true;

                    // we should clear buffer as AMG re-play all messages starting from specific point
                    _messageBuffer.Clear();
                }
                return;
            }

            // we can ignore not-execution messages
            // TODO: subscribe to channels that we really need and miss others
            if (!token.TryGetValue("ExecType", StringComparison.OrdinalIgnoreCase, out _) &&
                !token.TryGetValue("CxlRejReason", StringComparison.OrdinalIgnoreCase, out _))
            {
                return;
            }

            var report = token.ToObject<ExecutionReport>();
            try
            {
                if (_streamLocked)
                {
                    _messageBuffer.Enqueue(report);
                    return;
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }

            OnMessageImpl(report);
        }

        private void OnMessageImpl(ExecutionReport report)
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
                var orderFee = security.FeeModel.GetOrderFee(new OrderFeeParameters(security, order));
                var status = ConvertOrderStatus(report.OrdStatus);
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

        /// <summary>
        /// Lock the streaming processing while we're sending orders as sometimes they fill before the REST call returns.
        /// </summary>
        private void LockStream()
        {
            _streamLocked = true;
        }

        /// <summary>
        /// Unlock stream and process all backed up messages.
        /// </summary>
        private void UnlockStream()
        {
            while (_messageBuffer.Any())
            {
                _messageBuffer.TryDequeue(out var e);

                OnMessageImpl(e);
            }

            // Once dequeued in order; unlock stream.
            _streamLocked = false;
        }

        private void WithLockedStream(Action code)
        {
            try
            {
                LockStream();
                code();
            }
            finally
            {
                UnlockStream();
            }
        }
    }
}
