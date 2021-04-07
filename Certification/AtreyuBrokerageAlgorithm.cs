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

using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using System.Collections.Generic;
using QuantConnect.Algorithm;

namespace QuantConnect.Atreyu.Certification
{
    /// <summary>
    /// The demonstration algorithm shows some of the most common order methods when working with Crypto assets.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class AtreyuBrokerageAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            // Note: the conversion rates above are required in backtesting (for now) because of this issue:
            // https://github.com/QuantConnect/Lean/issues/1859
            DefaultOrderProperties = new AtreyuOrderProperties
            { 
                PostOnly = true, 
                TimeInForce = TimeInForce.Day
            };
            SetBrokerageModel(BrokerageName.Atreyu);
            AddEquity("ORCL", Resolution.Second, Market.USA, extendedMarketHours: true);
        }

        public override void OnWarmupFinished()
        {
            foreach (var item in Portfolio.CashBook)
            {
                Log($"{item.Key} : {item.Value.Amount}{item.Value.CurrencySymbol}");
            }
        }

        private bool _invested = false;

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!_invested)
            {
                LimitOrder(Symbol("ORCL"), 1, 50);
                //Liquidate(Symbol("BAC"));
                //Transactions.CancelOpenOrders(Symbol("ORCL"));
                //var order = Transactions
                //    .GetOpenOrders()
                //    .FirstOrDefault();
                //Transactions.UpdateOrder(new UpdateOrderRequest(
                //    DateTime.UtcNow, order.Id, new UpdateOrderFields()
                //    {
                //        Quantity = 110,
                //        Tag = "Change quantity: " + Time.Day
                //    }));

                _invested = true;
            }
            //else if (Portfolio.Invested)
            //{
            //    Liquidate(Symbol("BAC"));
            //}
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log(orderEvent.ToString());
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "10"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.18%"},
            {"Compounding Annual Return", "-99.992%"},
            {"Drawdown", "3.800%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-2.542%"},
            {"Sharpe Ratio", "-15.98"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-5.47"},
            {"Beta", "327.12"},
            {"Annual Standard Deviation", "0.201"},
            {"Annual Variance", "0.04"},
            {"Information Ratio", "-16.063"},
            {"Tracking Error", "0.2"},
            {"Treynor Ratio", "-0.01"},
            {"Total Fees", "$85.27"}
        };
    }
}
