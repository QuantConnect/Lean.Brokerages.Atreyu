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
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Atreyu.Client.Messages
{
    public class Order
    {
        public string Account { get; set; }
        
        public string Side { get; set; }
        
        public string Symbol { get; set; }
        
        public int OrderQty { get; set; }
        
        public string DeliverToCompID { get; set; }
        
        public string ExDestination { get; set; }
        
        public string ExecInst { get; set; }
        
        public string TransactTime { get; set; }

        public string HandlInst { get; set; }
        
        public string OrdStatus { get; set; }
        
        public string OrdType { get; set; }
        
        public string TimeInForce { get; set; }
        
        public string AvgPx { get; set; }

        //TODO: not clear what does tag "31" stand for
        //[JsonProperty(PropertyName = "31", Required = Required.DisallowNull)]
        //public string 31 { get; set; }
        
        public decimal Price { get; set; }
        
        public decimal StopPx { get; set; }
        
        public decimal CumQty { get; set; }
        
        public decimal LeavesQty { get; set; }

        //TODO: not clear what does tag "32" stand for
        //[JsonProperty(PropertyName = "32", Required = Required.DisallowNull)]
        //public string 32 { get; set; }
        
        public string ClOrdID { get; set; }

        [JsonIgnore]
        public decimal Quantity
        {
            get
            {
                switch (Side.ToUpperInvariant())
                {
                    case "BUY": return OrderQty;
                    case "SELL": return -OrderQty;
                    default:
                        throw new ArgumentException($"AtreyuBrokerage.Order: Unsupported trade direction type returned from brokerage: {Side}");
                }
            }
        }
    }
}
