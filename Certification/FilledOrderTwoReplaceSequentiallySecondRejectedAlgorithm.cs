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
using QuantConnect.Data.Fundamental;
using QuantConnect.Scheduling;

namespace QuantConnect.Atreyu.Certification
{
    /// <summary>
    /// D16
    /// Two cancel/replace request sent one after the other -
    /// broker rejects second while first still to be processed
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class FilledOrderTwoReplaceSequentiallySecondRejectedAlgorithm : BaseAtreyuCertificationTestAlgorithm
    {
        protected override string TestCode { get; } = "D17";
        protected override string[] Tickers
        {
            get
            {
                return new[] { "NOK" };
            }
        }
        private bool _replacePending = false;
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
                    if (symbol.Executions?.Any() == true)
                    {
                        if (symbol.Ticket.QuantityFilled == 10 && !_replacePending)
                        {
                            Transactions.UpdateOrder(new UpdateOrderRequest(
                                DateTime.UtcNow,
                                symbol.OrderId,
                                new UpdateOrderFields { Quantity = 80 }));
                          
                            Transactions.UpdateOrder(new UpdateOrderRequest(
                                DateTime.UtcNow,
                                symbol.OrderId,
                                new UpdateOrderFields { Quantity = 70 }));
                            _replacePending = true;
                        }
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
                new ExecutionEvent {Status = OrderStatus.PartiallyFilled, FillQuantity = 10},
                new ExecutionEvent {Status = OrderStatus.UpdateSubmitted},
                new ExecutionEvent {Status = OrderStatus.PartiallyFilled},
                new ExecutionEvent {Status = OrderStatus.PartiallyFilled, FillQuantity = 8},
                new ExecutionEvent {Status = OrderStatus.Filled, FillQuantity = 62}
            });
        }

        public override void OnEndOfAlgorithm()
        {
            base.OnEndOfAlgorithm();

            foreach (var exec in Executions.Values)
            {
                if (exec.Ticket.QuantityFilled != 80)
                {
                    throw new Exception($"Filled Quantity {exec.Ticket.QuantityFilled}; expected 80.");
                }
            }
        }
    }
}
