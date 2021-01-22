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
using System.Collections.Concurrent;
using System.Linq;
using Newtonsoft.Json;
using QuantConnect.Brokerages.Atreyu.Client.Messages;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;

namespace QuantConnect.Brokerages.Atreyu
{
    public partial class AtreyuBrokerage
    {
        private volatile bool _streamLocked;
        private readonly ConcurrentQueue<ExecutionReport> _messageBuffer = new ConcurrentQueue<ExecutionReport>();

        public void OnMessage(string message)
        {
            Log.Trace(message);
            ExecutionReport report = JsonConvert.DeserializeObject<ExecutionReport>(message);
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
            if (report.MsgType.Equals("ExecutionReport"))
            {
                switch (report.ExecType.ToUpper())
                {
                    case "REJECTED":
                    case "NEW":
                    case "EXECUTED":
                    case "PENDING_CANCEL":
                    case "CANCELED":
                        OnExecution(report);
                        break;
                    case "FILL":
                        OnOrderFill(report);
                        break;
                    default:
                        throw new InvalidOperationException($"AtreyuBrokerage: execution type is not supported; received {report.ExecType}");
                }
            }
        }

        private void OnExecution(ExecutionReport report)
        {
            Orders.Order order = _orderProvider.GetOrderByBrokerageId(report.OrigClOrdID ?? report.ClOrdID);
            if (order != null)
            {
                OnOrderEvent(new OrderEvent(order, Time.ParseFIXUtcTimestamp(report.TransactTime), OrderFee.Zero, "Atreyu Order Event")
                {
                    Status = ConvertExecType(report.ExecType)
                });
            }
        }

        private void OnOrderFill(ExecutionReport report)
        {
            Orders.Order order = _orderProvider.GetOrderByBrokerageId(report.ClOrdID);
            if (order != null)
            {
                OnOrderEvent(new OrderEvent(order, Time.ParseFIXUtcTimestamp(report.TransactTime), OrderFee.Zero, "Atreyu Order Event")
                {
                    Status = OrderStatus.Filled
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
                ExecutionReport e;
                _messageBuffer.TryDequeue(out e);

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
