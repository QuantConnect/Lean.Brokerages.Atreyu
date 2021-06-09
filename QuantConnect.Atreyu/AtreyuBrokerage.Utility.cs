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
using QuantConnect.Orders;
using QuantConnect.Orders.TimeInForces;
using QuantConnect.Securities;

namespace QuantConnect.Atreyu
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
                case "1":
                    leanOrder = new MarketOrder(
                        symbol,
                        atreyuOrder.Quantity,
                        datetime);
                    break;
                case "LIMIT":
                case "2":
                    leanOrder = new LimitOrder(
                        symbol,
                        atreyuOrder.Quantity,
                        atreyuOrder.Price,
                        datetime);
                    break;
                case "MARKETONCLOSE":
                case "5":
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
                case "GTC":
                    return TimeInForce.GoodTilCanceled;
                case "2":
                case "3":
                    throw new ArgumentException($"AtreyuBrokerage.ConvertTimeInForce: Unsupported TimeInForce value returned from brokerage: {atreyuOrderTimeInForce}");
                case "0":
                case "DAY":
                default:
                    return TimeInForce.Day;
            }
        }

        private string ConvertTimeInForce(TimeInForce timeInForce)
        {
            switch (timeInForce)
            {
                case DayTimeInForce day:
                    return "0";
                case GoodTilCanceledTimeInForce gtc:
                //return "1";
                default:
                    throw new ArgumentException("AtreyuBrokerage.ConvertTimeInForce: currently support only Day orders (TIF DAY). No DAY+/ GTX or GTC supported.");
            }
        }

        private OrderStatus ConvertOrderStatus(Client.Messages.Order atreyuOrder) =>
            ConvertOrderStatus(atreyuOrder.OrdStatus);

        private OrderStatus ConvertOrderStatus(string atreyuOrderStatus)
        {
            switch (atreyuOrderStatus)
            {
                case "0":
                case "NEW":
                    return OrderStatus.Submitted;
                case "1":
                case "PARTIALLY_FILLED":
                    return OrderStatus.PartiallyFilled;
                case "2":
                case "FILLED":
                    return OrderStatus.Filled;
                case "4":
                case "CANCELED":
                    return OrderStatus.Canceled;
                case "6":
                case "PENDING_CANCEL":
                    return OrderStatus.CancelPending;
                case "5":
                case "E":
                case "PENDING_REPLACE":
                case "REPLACED":
                    return OrderStatus.UpdateSubmitted;

                case "3":
                case "DONE_FOR_DAY":
                case "7":
                case "8":
                case "REJECTED":
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
            // Unlike OrdStatus, ExecType field is provided in executions
            // values are similar, but a bit different. Char values are not provided
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
                case "REPLACE":
                    return OrderStatus.UpdateSubmitted;
                case "CANCELED":
                    return OrderStatus.Canceled;
                case "REJECTED":
                case "DONE_FOR_DAY":
                    return OrderStatus.Invalid;

                default:
                    throw new ArgumentException($"AtreyuBrokerage.ConvertOrderStatus: Unsupported order status returned from brokerage: {execType}");
            }
        }

        private string ConvertDirection(Order order)
        {
            var holdingQuantity = _securityProvider.GetHoldingsQuantity(order.Symbol);
            switch (order.Direction)
            {
                case OrderDirection.Buy: return "1";
                case OrderDirection.Sell: return (holdingQuantity >= order.AbsoluteQuantity ? "2" : "5");

                // not sure how to map these guys
                default:
                    throw new ArgumentException($"AtreyuBrokerage.ConvertDirection: Unsupported order direction: {order.Direction}");
            }
        }

        private string GetNewOrdID() => Guid.NewGuid().ToString().Replace("-", string.Empty);
    }
}
