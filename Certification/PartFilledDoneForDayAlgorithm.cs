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
    /// D2 - Assuming day order
    /// Part-filled order, completed with done-for-day
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class PartFilledDoneForDayAlgorithm : QCAlgorithm
    {
        private readonly string _ticker = "DAL";
        Dictionary<string, Queue<decimal>> _executions = new Dictionary<string, Queue<decimal>>();

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
            if (_executions.ContainsKey(_ticker))
            {
                return;
            }

            foreach (var bar in data.Bars)
            {
                // find the front contract expiring no earlier than in 90 days
                if (_executions.ContainsKey(bar.Key.Value))
                {
                    continue;
                }

                LimitOrder(bar.Key, 100, 5);
                _executions.Add(bar.Key.Value, null);
            }
        }

        /// <summary>
        /// Execution(X) New
        /// Execution(X) Partial Fill; 33
        /// Execution(X) Partial Fill; 66
        /// Execution(X) Done for Day; 66
        /// </summary>
        /// <param name="orderEvent"></param>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Submitted)
            {
                _executions[orderEvent.Symbol.Value] = new Queue<decimal>(new decimal[] { 33, 33, 0 });
                return;
            }

            if (!_executions.TryGetValue(orderEvent.Symbol.Value, out var queue))
            {
                throw new Exception($"Unexpected Symbol arrived. Symbol {orderEvent.Symbol}");
            }

            if (queue.Count > 1 && orderEvent.Status != OrderStatus.PartiallyFilled)
            {
                throw new Exception($"Unexpected Order status. Expected {OrderStatus.PartiallyFilled}, but was {orderEvent.Status}");
            }

            if (queue.Count == 1 && orderEvent.Status != OrderStatus.Invalid)
            {
                throw new Exception($"Unexpected Order status. Expected {OrderStatus.Invalid}, but was {orderEvent.Status}");
            }

            var expected = queue.Dequeue();
            if (orderEvent.FillQuantity != expected)
            {
                throw new Exception($"Unexpected Fill Quantity arrived. Expected {expected}, but was {orderEvent.FillQuantity}");
            }

            if (_executions.All(s => s.Value?.Count == 0))
            {
                Quit("Certification Test D2 Passed successfully");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_executions.Any())
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

            var failed = _executions
                .Where(s => s.Value.Any())
                .Select(s => s.Key)
                .ToArray();
            if (failed.Any())
            {
                throw new Exception($"Following symbols were not processed properly: {string.Join(", ", failed)}.");
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
