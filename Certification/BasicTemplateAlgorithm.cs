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
    /// Certification Test Template Algorithm
    /// Implements basic initialize and asserts
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public abstract class BasicTemplateAlgorithm : QCAlgorithm
    {
        protected virtual string TestCode { get; } = "None";
        protected virtual string[] Tickers { get; } = new string[0];
        protected Dictionary<string, Execution> Executions { get; } = new Dictionary<string, Execution>();

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
            foreach (var ticker in Tickers)
            {
                AddEquity(ticker, Resolution.Second, Market.USA, extendedMarketHours: true);
            }

            Schedule.Add(new ScheduledEvent(
                "Abort",
                DateTime.UtcNow.AddMinutes(3),
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
                if (Executions.ContainsKey(bar.Key.Value))
                {
                    continue;
                }

                var ticket = LimitOrder(bar.Key, 100, 5);
                Executions.Add(bar.Key.Value, new Execution(ticket));
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            // ignore symbols from previous run (AMG replay)
            if (!Executions.ContainsKey(orderEvent.Symbol.Value))
            {
                return;
            }

            if (orderEvent.Status == OrderStatus.Submitted && Executions.TryGetValue(orderEvent.Symbol.Value, out var ticker) && ticker.Executions == null)
            {
                OnOrderSubmitted(orderEvent);
                return;
            }

            if (!Executions.TryGetValue(orderEvent.Symbol.Value, out var target) || target.Executions?.Any() != true)
            {
                throw new Exception($"Unexpected order event: {orderEvent}");
            }

            target.Assert(orderEvent);

            if (Executions.All(s => s.Value?.Executions.Count == 0))
            {
                Quit($"Certification Test {TestCode} Passed successfully");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (Executions.Count != Tickers.Length)
            {
                throw new Exception($"{string.Join(", ", Tickers.Except(Executions.Keys))} were not sent.");
            }

            if (Executions.Any(s => s.Value == null))
            {
                var keys = Executions
                    .Where(s => s.Value == null)
                    .Select(s => s.Key);

                throw new Exception($"{string.Join(", ", keys)} were sent but not accepted by brokerage.");
            }

            var notFilled = Executions
                .Where(s => s.Value.Executions.Any())
                .Select(s => s.Key)
                .ToArray();
            if (notFilled.Any())
            {
                throw new Exception($"Following symbol didn't fill properly: {string.Join(", ", notFilled)}. Expected: Filled");
            }
        }

        public virtual void OnOrderSubmitted(OrderEvent orderEvent) { }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };
    }
}
