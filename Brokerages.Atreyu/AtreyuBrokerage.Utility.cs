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
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using QuantConnect.Logging;
using QuantConnect.Orders;

namespace QuantConnect.Brokerages.Atreyu
{
    public partial class AtreyuBrokerage
    {
        private Order ConvertOrder(Client.Messages.Order atreyuOrder)
        {
            Order leanOrder;

            var symbol = _symbolMapper.GetLeanSymbol(atreyuOrder.Symbol, SecurityType.Equity, Market.USA);
            var datetime = Time.ParseFIXUtcTimestamp(atreyuOrder.TransactTime);
            switch (atreyuOrder.OrdType)
            {
                case "MARKET":
                    leanOrder = new MarketOrder(
                        symbol,
                        atreyuOrder.Quantity,
                        datetime);
                    break;
                case "LIMIT":
                    leanOrder = new LimitOrder(
                        symbol,
                        atreyuOrder.Quantity,
                        atreyuOrder.Price,
                        datetime);
                    break;
                case "MARKETONCLOSE":
                    leanOrder = new MarketOnCloseOrder(
                        symbol,
                        atreyuOrder.Quantity,
                        datetime);
                    break;

                default:
                    throw new InvalidOperationException($"AtreyuBrokerage.ConvertOrder: Unsupported order type returned from brokerage: {atreyuOrder.OrdType}");
            }
            leanOrder.BrokerId.Add(atreyuOrder.ClOrdID.ToStringInvariant());
            leanOrder.Properties.TimeInForce = ConvertTimeInForce(atreyuOrder.TimeInForce);
            leanOrder.Status = ConvertOrderStatus(atreyuOrder);

            return leanOrder;
        }

        private TimeInForce ConvertTimeInForce(string atreyuOrderTimeInForce)
        {
            switch (atreyuOrderTimeInForce)
            {
                case "1":
                    return TimeInForce.GoodTilCanceled;
                case "2":
                case "3":
                    throw new ArgumentException($"AtreyuBrokerage.ConvertTimeInForce: Unsupported TimeInForce value returned from brokerage: {atreyuOrderTimeInForce}");
                case "DAY":
                default:
                    return TimeInForce.Day;
            }
        }

        private Holding ConvertHolding(Client.Messages.Position position)
        {
            try
            {
                // TODO: need data for holding?
                return new Holding
                {
                    Symbol = _symbolMapper.GetLeanSymbol(position.Symbol, SecurityType.Crypto, Market.Bitfinex),
                    //AveragePrice = position.BasePrice,
                    //MarketPrice = position.BasePrice,
                    Quantity = position.LongQty > 0 ? position.LongQty : position.ShortQty,
                    CurrencySymbol = "$",
                    Type = SecurityType.Equity
                };
            }
            catch (Exception)
            {
                Log.Error($"AtreyuBrokerage.ConvertHolding(): failed to set {position.Symbol} market price");
                throw;
            }
        }

        private OrderStatus ConvertOrderStatus(Client.Messages.Order atreyuOrder) =>
            ConvertOrderStatus(atreyuOrder.OrdStatus);

        private OrderStatus ConvertOrderStatus(string atreyuOrderStatus)
        {
            switch (atreyuOrderStatus)
            {
                case "NEW":
                    return OrderStatus.Submitted;
                case "PARTIALLY_FILLED":
                    return OrderStatus.PartiallyFilled;
                case "FILLED":
                    return OrderStatus.Filled;
                case "CANCELED":
                    return OrderStatus.Canceled;
                case "PENDING_CANCEL":
                    return OrderStatus.CancelPending;
                case "PENDING_REPLACE":
                case "REPLACED":
                    return OrderStatus.UpdateSubmitted;

                case "7":
                case "8":
                case "9":
                case "C":
                    return OrderStatus.Invalid;

                // not sure how to map these guys
                default:
                    throw new ArgumentException($"AtreyuBrokerage.ConvertOrderStatus: Unsupported order status returned from brokerage: {atreyuOrderStatus}");
            }
        }

        private OrderStatus ConvertExecType(string execType)
        {
            switch (execType.ToUpperInvariant())
            {
                case "NEW":
                    return OrderStatus.Submitted;
                case "PARTIAL_FILL":
                    return OrderStatus.PartiallyFilled;
                case "FILL":
                    return OrderStatus.Filled;
                case "PENDING_CANCEL":
                    return OrderStatus.CancelPending;
                case "PENDING_REPLACE":
                case "REPLACE":
                    return OrderStatus.UpdateSubmitted;
                case "CANCELED":
                    return OrderStatus.Canceled;
                case "REJECTED":
                    return OrderStatus.Invalid;

                default:
                    throw new ArgumentException($"AtreyuBrokerage.ConvertOrderStatus: Unsupported order status returned from brokerage: {execType}");
            }
        }

        private string ConvertDirection(OrderDirection direction)
        {
            switch (direction)
            {
                case OrderDirection.Buy: return "1";
                case OrderDirection.Sell: return "2";

                // not sure how to map these guys
                default:
                    throw new ArgumentException($"AtreyuBrokerage.ConvertDirection: Unsupported order direction: {direction}");
            }
        }
    }
}
