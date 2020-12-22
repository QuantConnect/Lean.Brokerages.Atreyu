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

        [JsonProperty(PropertyName = "1", Required = Required.DisallowNull)]
        public string Account { get; set; }

        [JsonProperty(PropertyName = "54", Required = Required.DisallowNull)]
        public string Side { get; set; }

        [JsonProperty(PropertyName = "55", Required = Required.DisallowNull)]
        public string Symbol { get; set; }

        [JsonProperty(PropertyName = "38", Required = Required.DisallowNull)]
        public decimal OrderQty { get; set; }

        [JsonProperty(PropertyName = "128", Required = Required.DisallowNull)]
        public string DeliverToCompID { get; set; }

        [JsonProperty(PropertyName = "100", Required = Required.DisallowNull)]
        public string ExDestination { get; set; }

        [JsonProperty(PropertyName = "18", Required = Required.DisallowNull)]
        public string ExecInst { get; set; }

        [JsonProperty(PropertyName = "60", Required = Required.DisallowNull)]
        public string TransactTime { get; set; }

        [JsonProperty(PropertyName = "21", Required = Required.DisallowNull)]
        public string HandlInst { get; set; }

        [JsonProperty(PropertyName = "39", Required = Required.DisallowNull)]
        public string OrdStatus { get; set; }

        [JsonProperty(PropertyName = "40", Required = Required.DisallowNull)]
        public string OrdType { get; set; }

        [JsonProperty(PropertyName = "59", Required = Required.DisallowNull)]
        public string TimeInForce { get; set; }

        [JsonProperty(PropertyName = "6", Required = Required.DisallowNull)]
        public string AvgPx { get; set; }

        //TODO: not clear what does tag "31" stand for
        //[JsonProperty(PropertyName = "31", Required = Required.DisallowNull)]
        //public string 31 { get; set; }

        [JsonProperty(PropertyName = "44", Required = Required.DisallowNull)]
        public decimal Price { get; set; }

        [JsonProperty(PropertyName = "99", Required = Required.DisallowNull)]
        public decimal StopPrice { get; set; }

        [JsonProperty(PropertyName = "14", Required = Required.DisallowNull)]
        public decimal CumQty { get; set; }

        [JsonProperty(PropertyName = "151", Required = Required.DisallowNull)]
        public decimal LeavesQty { get; set; }

        //TODO: not clear what does tag "32" stand for
        //[JsonProperty(PropertyName = "32", Required = Required.DisallowNull)]
        //public string 32 { get; set; }

        [JsonProperty(PropertyName = "11", Required = Required.DisallowNull)]
        public string ClOrdID { get; set; }

        [JsonIgnore]
        public decimal Quantity
        {
            get
            {
                switch (Side)
                {
                    case "1": return OrderQty;
                    case "2": return -OrderQty;
                    default:
                        throw new ArgumentException($"AtreyuBrokerage.Order: Unsupported trade direction type returned from brokerage: {Side}");
                }
            }
        }
    }
}
