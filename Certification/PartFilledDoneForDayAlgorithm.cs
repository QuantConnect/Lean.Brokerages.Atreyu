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
    public class PartFilledDoneForDayAlgorithm : BasicTemplateAlgorithm
    {
        protected override string TestCode { get; } = "D2";
        protected override string[] Tickers
        {
            get { return new[] { "DAL" }; }
        }

        /// <summary>
        /// Execution(X) New
        /// Execution(X) Partial Fill; 33
        /// Execution(X) Partial Fill; 66
        /// Execution(X) Done for Day; 66
        /// </summary>
        /// <param name="orderEvent"></param>
        public override void OnOrderSubmitted(OrderEvent orderEvent)
        {
            Executions[orderEvent.Symbol.Value].Executions = new Queue<ExecutionEvent>(new[]
            {
                new ExecutionEvent {Status = OrderStatus.PartiallyFilled, FillQuantity = 33},
                new ExecutionEvent {Status = OrderStatus.PartiallyFilled, FillQuantity = 33},
                new ExecutionEvent {Status = OrderStatus.Invalid, FillQuantity = 0}
            });
        }
    }
}
