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
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using QuantConnect.Algorithm;
using QuantConnect.Scheduling;

namespace QuantConnect.Atreyu.Certification
{
    /// <summary>
    /// D3 - Request cancel of unfilled order
    /// Request cancel of unfilled order
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class CancelledOrderAlgorithm : QCAlgorithm
    {
        private readonly string _ticker = "ORCL";
        private readonly Dictionary<string, Queue<OrderStatus>> _executions = new Dictionary<string, Queue<OrderStatus>>();
        private bool _cancelPending = false;
        private int orderId = 0;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            DefaultOrderProperties = new AtreyuOrderProperties
            {
                PostOnly = true,
                TimeInForce = TimeInForce.Day
            };
            SetCash(100000);
            SetBrokerageModel(BrokerageName.Atreyu);

            AddEquity(_ticker, Resolution.Second, Market.USA, extendedMarketHours: true);

            Schedule.Add(new ScheduledEvent(
                "Abort",
                DateTime.UtcNow.AddMinutes(1),
                (s, date) => SetQuit(true)));
        }
        
        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            foreach (var bar in data.Bars)
            {
                // find the front contract expiring no earlier than in 90 days
                if (_executions.TryGetValue(bar.Key.Value, out var queue))
                {
                    if (queue?.Any() == true && !_cancelPending)
                    {
                        Transactions.CancelOrder(orderId);
                        _cancelPending = true;
                    }
                    continue;
                }

                var ticketOrder = LimitOrder(bar.Key, 100, 5);
                orderId = ticketOrder.OrderId;
                _executions.Add(bar.Key.Value, null);
            }
        }

        /// <summary>
        /// LEAN(internal)  Pending Cancel, by BrokerageTransactionHandler.HandleOrderEvent
        /// Execution(X)    Pending Cancel
        /// Execution(X)    Cancelled
        /// </summary>
        /// <param name="orderEvent"></param>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.OrderId != orderId)
            {
                // ignore events not related to current order, i.e. open orders
                return;
            }

            if (orderEvent.Status == OrderStatus.Submitted)
            {
                _executions[orderEvent.Symbol.Value] = new Queue<OrderStatus>(new[] { OrderStatus.CancelPending, OrderStatus.CancelPending, OrderStatus.Canceled });
                return;
            }

            if (!_executions.TryGetValue(orderEvent.Symbol.Value, out var queue))
            {
                throw new Exception($"Unexpected Symbol has arrived. Symbol {orderEvent.Symbol}");
            }

            var expectedStatus = queue.Dequeue();
            if (orderEvent.Status != expectedStatus)
            {
                throw new Exception($"Unexpected Order status. Expected {expectedStatus}, but was {orderEvent.Status}");
            }

            if (_executions.All(s => s.Value?.Count == 0))
            {
                Quit("Certification Test D3 Passed successfully");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_executions.Count != 1)
            {
                throw new Exception($"{_ticker} was not sent.");
            }

            if (_executions.Any(s => s.Value == null))
            {
                var keys = _executions
                    .Where(s => s.Value == null)
                    .Select(s => s.Key);

                throw new Exception($"{string.Join(", ", keys)} were sent but not accepted by brokerage.");
            }

            var notFilled = _executions
                .Where(s => s.Value.Any())
                .Select(s => s.Key)
                .ToArray();
            if (notFilled.Any())
            {
                throw new Exception($"Following symbol was not processed properly: {string.Join(", ", notFilled)}. Expected: Cancelled");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };
    }
}
