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
    /// D5 - Order fills before cancel accepted
    /// Pending cancel is returned
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class FilledBeforeCancelAcceptedAlgorithm : BaseAtreyuCertificationTestAlgorithm
    {
        protected override string TestCode { get; } = "D5";
        protected override string[] Tickers
        {
            get
            {
                return new[] { "PRU" };
            }
        }
        private bool _cancelPending = false;

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
                    if (symbol.Executions?.Any() == true && !_cancelPending && symbol.Ticket.QuantityFilled == 25)
                    {
                        Transactions.CancelOrder(symbol.OrderId);
                        _cancelPending = true;
                    }
                    continue;
                }

                var ticket = LimitOrder(bar.Key, 100, 5);
                Executions.Add(bar.Key.Value, new Execution(ticket));
            }
        }
        
        public override void OnOrderSubmitted(OrderEvent orderEvent)
        {
            Executions[orderEvent.Symbol.Value].Executions = new Queue<ExecutionEvent>(new[]
            {
                new ExecutionEvent {Status = OrderStatus.PartiallyFilled, FillQuantity = 25},
                new ExecutionEvent {Status = OrderStatus.CancelPending},
                new ExecutionEvent {Status = OrderStatus.PartiallyFilled, FillQuantity = 25},
                new ExecutionEvent {Status = OrderStatus.CancelPending},
                new ExecutionEvent {Status = OrderStatus.Filled, FillQuantity = 50},
                new ExecutionEvent {Status = OrderStatus.Filled}
            });
        }

        public override void OnEndOfAlgorithm()
        {
            base.OnEndOfAlgorithm();

            foreach (var exec in Executions.Values)
            {
                if (exec.Ticket.QuantityFilled != 100)
                {
                    throw new Exception($"Filled Quantity {exec.Ticket.QuantityFilled}; expected 100.");
                }
            }
        }
    }
}
