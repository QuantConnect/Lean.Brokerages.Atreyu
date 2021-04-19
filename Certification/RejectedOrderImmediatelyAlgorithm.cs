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
    /// E4 - A new order is immediately rejected
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class RejectedOrderImmediatelyAlgorithm : BasicTemplateAlgorithm
    {
        protected override string TestCode { get; } = "E4";
        protected override string[] Tickers
        {
            get
            {
                return new[] { "MSFT" };
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            foreach (var bar in data.Bars)
            {
                if (!Tickers.Contains(bar.Key.Value))
                    continue;

                if (Executions.TryGetValue(bar.Key.Value, out var symbol))
                {
                    continue;
                }

                var ticket = LimitOrder(bar.Key, 100, 5);
                Executions.Add(bar.Key.Value, new Execution(ticket));
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Invalid && Executions.TryGetValue(orderEvent.Symbol.Value, out var ticker) && ticker.Executions == null)
            {
                Quit($"Certification Test {TestCode} Passed successfully");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (Executions.Values.Any(e => e.Executions != null))
            {
                throw new Exception($"Order should be rejected immediately");
            }
        }
    }
}
