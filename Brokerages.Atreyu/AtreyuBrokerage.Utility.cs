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
            switch (atreyuOrder.OrdType)
            {
                case "1":
                    leanOrder = new MarketOrder { Price = atreyuOrder.Price };
                    break;
                case "2":
                    leanOrder = new LimitOrder { LimitPrice = atreyuOrder.Price };
                    break;
                case "3":
                    leanOrder = new StopMarketOrder { StopPrice = atreyuOrder.Price };
                    break;
                case "4":
                    leanOrder = new StopLimitOrder { StopPrice = atreyuOrder.StopPrice, LimitPrice = atreyuOrder.Price };
                    break;
                case "5":
                    leanOrder = new MarketOnCloseOrder() { Price = atreyuOrder.Price };
                    break;

                default:
                    throw new InvalidOperationException($"AtreyuBrokerage.ConvertOrder: Unsupported order type returned from brokerage: {atreyuOrder.OrdType}");
            }

            leanOrder.Quantity = atreyuOrder.Quantity;
            leanOrder.BrokerId = new List<string> { atreyuOrder.ClOrdID.ToStringInvariant() };
            // TODO: support options
            leanOrder.Symbol = _symbolMapper.GetLeanSymbol(atreyuOrder.Symbol, SecurityType.Equity, Market.USA);
            leanOrder.Time = DateTime.ParseExact(atreyuOrder.TransactTime, "YYYYMMDD-hh:mm:ss.s", CultureInfo.InvariantCulture);
            leanOrder.Status = ConvertOrderStatus(atreyuOrder);
            leanOrder.Price = atreyuOrder.Price;

            return leanOrder;
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
                    Quantity = position.LongQty > 0 ? position.LongQty : position.ShortQty,
                    //UnrealizedPnL = position.ProfitLoss,
                    CurrencySymbol = "$",
                   // MarketPrice = ??,
                    //Type = position.SecurityType
                };
            }
            catch (Exception)
            {
                Log.Error($"AtreyuBrokerage.ConvertHolding(): failed to set {position.Symbol} market price");
                throw;
            }
        }

        private OrderStatus ConvertOrderStatus(Client.Messages.Order atreyuOrder)
        {
            switch (atreyuOrder.OrdStatus)
            {
                case "0": return OrderStatus.New;
                case "1": return OrderStatus.PartiallyFilled;
                case "2": return OrderStatus.Filled;
                case "4": return OrderStatus.Canceled;
                case "6": return OrderStatus.CancelPending;
                case "A": return OrderStatus.Submitted;
                case "E": return OrderStatus.UpdateSubmitted;

                case "7":
                case "8":
                case "9":
                case "C":
                    return OrderStatus.Invalid;

                // not sure how to map these guys
                default:
                    throw new ArgumentException($"AtreyuBrokerage.ConvertOrderStatus: Unsupported order status returned from brokerage: {atreyuOrder.OrdStatus}");
            }
        }
    }
}
