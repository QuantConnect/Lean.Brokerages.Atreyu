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
using System.Linq;
using System.Threading;
using Moq;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Binance;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Tests;
using QuantConnect.Tests.Brokerages;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Atreyu.Tests
{
    /// <summary>
    /// Atreyu-CertificationSimulatorTests-v2.2.2
    /// </summary>
    public class AtreyuBrokerageTests : QuantConnect.Tests.Brokerages.BrokerageTests
    {
        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {
            var securities = new SecurityManager(new TimeKeeper(DateTime.UtcNow, TimeZones.NewYork))
            {
                { Symbol, CreateSecurity(Symbol) },
                { GESymbol, CreateSecurity(GESymbol) }
            };

            var transactions = new SecurityTransactionManager(null, securities);
            transactions.SetOrderProcessor(new FakeOrderProcessor());

            var algorithm = new Mock<IAlgorithm>();
            algorithm.Setup(a => a.Transactions).Returns(transactions);
            algorithm.Setup(a => a.BrokerageModel).Returns(new AtreyuBrokerageModel());
            algorithm.Setup(a => a.Portfolio).Returns(new SecurityPortfolioManager(securities, transactions));

            return new AtreyuBrokerage(orderProvider, securityProvider);
        }

        protected override decimal GetAskPrice(Symbol symbol)
        {
            // not used, we use bid/ask prices
            return 0;
        }

        private static Symbol StaticSymbol => QuantConnect.Symbol.Create("ORCL", SecurityType.Equity, Market.USA);
        private static Symbol GESymbol => QuantConnect.Symbol.Create("GE", SecurityType.Equity, Market.USA);
        private static Symbol ORCLSymbol => QuantConnect.Symbol.Create("ORCL", SecurityType.Equity, Market.USA);
        private static Symbol NVDASymbol => QuantConnect.Symbol.Create("NVDA", SecurityType.Equity, Market.USA);

        /// <summary>
        /// Gets the symbol to be traded, must be shortable
        /// </summary>
        protected override Symbol Symbol => StaticSymbol;

        protected override SecurityType SecurityType => StaticSymbol.SecurityType;

        /// <summary>
        /// Returns whether or not the brokers order methods implementation are async
        /// </summary>
        protected override bool IsAsync() => true;

        /// <summary>
        /// Returns whether or not the brokers order cancel method implementation is async
        /// </summary>
        protected override bool IsCancelAsync() => true;

        /// <summary>
        /// Gets the default order quantity
        /// </summary>
        protected override decimal GetDefaultQuantity() => 5;

        private static TestCaseData[] CancelOrderParameters => new[]
        {
            new TestCaseData(new MarketOrderTestParameters(ORCLSymbol, new OrderProperties{TimeInForce = TimeInForce.Day})).SetName("MarketOrder"),   // D1
            new TestCaseData(new LimitOrderTestParameters(ORCLSymbol, 100m, 10m, new OrderProperties{TimeInForce = TimeInForce.Day})).SetName("LimitOrder")     //D3
        };

        // GE is shortable; placed 100 shares
        // Both Market and Limit orders executed immediatelly according to D1
        private static TestCaseData[] FilledParameters => new[]
        {
            new TestCaseData(new MarketOrderTestParameters(GESymbol, new OrderProperties{TimeInForce = TimeInForce.Day})).SetName("MarketOrder"),
            new TestCaseData(new LimitOrderTestParameters(GESymbol, 50m, 2m, new OrderProperties{TimeInForce = TimeInForce.Day})).SetName("LimitOrder")
        };

        private static TestCaseData[] NonShortableParameters => new[]
        {
            new TestCaseData(new MarketOrderTestParameters(NVDASymbol, new OrderProperties{TimeInForce = TimeInForce.Day})).SetName("MarketOrder"),
            new TestCaseData(new LimitOrderTestParameters(NVDASymbol, 50m, 2m, new OrderProperties{TimeInForce = TimeInForce.Day})).SetName("LimitOrder")
        };

        [Test, TestCaseSource(nameof(CancelOrderParameters))]
        public override void CancelOrders(OrderTestParameters parameters)
        {
            const int secondsTimeout = 20;
            Log.Trace("");
            Log.Trace("CANCEL ORDERS");
            Log.Trace("");

            var order = PlaceOrderWaitForStatus(parameters.CreateLongOrder(GetDefaultQuantity()), parameters.ExpectedStatus);

            var canceledOrderStatusEvent = new ManualResetEvent(false);
            EventHandler<OrderEvent> orderStatusCallback = (sender, fill) =>
            {
                if (fill.Status == OrderStatus.Canceled)
                {
                    canceledOrderStatusEvent.Set();
                }
            };
            Brokerage.OrderStatusChanged += orderStatusCallback;
            var cancelResult = false;
            try
            {
                cancelResult = Brokerage.CancelOrder(order);
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }

            Assert.AreEqual(IsCancelAsync() && parameters.ExpectedCancellationResult, cancelResult);

            if (parameters.ExpectedCancellationResult)
            {
                // We expect the OrderStatus.Canceled event
                canceledOrderStatusEvent.WaitOneAssertFail(1000 * secondsTimeout, "Order timedout to cancel");
            }

            var openOrders = Brokerage.GetOpenOrders();
            var cancelledOrder = openOrders.FirstOrDefault(x => x.Id == order.Id);
            Assert.IsNull(cancelledOrder);

            canceledOrderStatusEvent.Reset();

            var cancelResultSecondTime = false;
            try
            {
                cancelResultSecondTime = Brokerage.CancelOrder(order);
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
            // atreyu doesn't allow to place cancel request for the same order
            // "Text":"##:REJECT - ORDER NOT ACTIVE CLORDID (...)"
            Assert.False(cancelResultSecondTime);
            // We do NOT expect the OrderStatus.Canceled event
            Assert.IsFalse(canceledOrderStatusEvent.WaitOne(new TimeSpan(0, 0, 10)));

            Brokerage.OrderStatusChanged -= orderStatusCallback;
        }

        [Test, TestCaseSource(nameof(FilledParameters))]
        public override void LongFromZero(OrderTestParameters parameters)
        {
            base.LongFromZero(parameters);
        }

        [Test, TestCaseSource(nameof(FilledParameters))]
        public override void CloseFromLong(OrderTestParameters parameters)
        {
            base.CloseFromLong(parameters);
        }

        [Test, TestCaseSource(nameof(FilledParameters))]
        public override void ShortFromZero(OrderTestParameters parameters)
        {
            base.ShortFromZero(parameters);
        }

        [Test, TestCaseSource(nameof(FilledParameters))]
        public override void CloseFromShort(OrderTestParameters parameters)
        {
            base.CloseFromShort(parameters);
        }

        [Test, TestCaseSource(nameof(FilledParameters))]
        public override void ShortFromLong(OrderTestParameters parameters)
        {
            base.ShortFromLong(parameters);
        }

        [Test, TestCaseSource(nameof(FilledParameters))]
        public override void LongFromShort(OrderTestParameters parameters)
        {
            base.LongFromShort(parameters);
        }

        [Test, TestCaseSource(nameof(NonShortableParameters))]
        public void ShortRejected(OrderTestParameters parameters)
        {
            PlaceOrderWaitForStatus(
                parameters.CreateShortOrder(GetDefaultQuantity()),
                OrderStatus.Invalid,
                allowFailedSubmission: true);
        }

        [Test]
        public override void GetAccountHoldings()
        {
            Log.Trace("");
            Log.Trace("GET ACCOUNT HOLDINGS");
            Log.Trace("");

            // ignore existing holdings as it doesn't contain information required for Lean (avg price is missed)
            Assert.IsEmpty(Brokerage.GetAccountHoldings());

            PlaceOrderWaitForStatus(new MarketOrder(GESymbol, GetDefaultQuantity(), DateTime.UtcNow, properties: new OrderProperties() { TimeInForce = TimeInForce.Day }));
            Thread.Sleep(3000);

            // still ignore 
            Assert.IsEmpty(Brokerage.GetAccountHoldings());
        }

        [Test]
        public void ParseUserInput()
        {
            var brokerage = new AtreyuBrokerage(
                "tcp://trademachine-nlb-c049c393793491c1.elb.us-east-1.amazonaws.com",
                24686,
                24687,
                "QUANTCONNECT",
                "udpWuzLTH7GDe9bN",
                "QC-TEST1",
                "[{\"currency\":\"usd\", \"amount\":1000.0}, {\"currency\":\"eur\", \"amount\":100.0}]",
                "[{\"AveragePrice\": 5,\"Quantity\": 33,\"Symbol\": {\"Value\": \"GME\",\"ID\": \"GME 2T\",\"Permtick\": \"GME\"},\"MarketPrice\": 10, \"Type\":1 }]",
                "ABCD",
                "N",
                new OrderProvider(),
                new SecurityProvider());

            Assert.AreEqual(2, brokerage.GetCashBalance().Count);

            Assert.AreEqual(1, brokerage.GetAccountHoldings().Count);
            var holding = brokerage.GetAccountHoldings().FirstOrDefault();
            Assert.NotNull(holding);
            Assert.AreEqual(5, holding.AveragePrice);
            Assert.AreEqual(33, holding.Quantity);
            Assert.AreEqual("GME", holding.Symbol.Value);
            Assert.AreEqual(10, holding.MarketPrice);
            Assert.AreEqual(SecurityType.Equity, holding.Type);
        }
    }
}