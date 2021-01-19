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

using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Atreyu.Client.Messages
{
    public class NewEquityOrderMessage: SignedMessage
    {
        public NewEquityOrderMessage()
        {
            MsgType = "D";
        }

        [JsonProperty(PropertyName = "11", Required = Required.DisallowNull)]
        public string ClOrdID { get; set; }

        [JsonProperty(PropertyName = "54", Required = Required.DisallowNull)]
        public string Side { get; set; }

        [JsonProperty(PropertyName = "55", Required = Required.DisallowNull)]
        public string Symbol { get; set; }

        [JsonProperty(PropertyName = "38", Required = Required.DisallowNull)]
        public int OrderQty { get; set; }

        [JsonProperty(PropertyName = "44")]
        public decimal Price { get; set; }

        [JsonProperty(PropertyName = "99")]
        public decimal StopPx { get; set; }

        [JsonProperty(PropertyName = "128", Required = Required.DisallowNull)]
        public string DeliverToCompID { get; set; }

        [JsonProperty(PropertyName = "100", Required = Required.DisallowNull)]
        public string ExDestination { get; set; }

        [JsonProperty(PropertyName = "18", Required = Required.DisallowNull)]
        public string ExecInst { get; set; }

        [JsonProperty(PropertyName = "60", Required = Required.DisallowNull)]
        public string TransactTime { get; set; }

        [JsonProperty(PropertyName = "21")]
        public string HandlInst { get; set; }

        [JsonProperty(PropertyName = "40")]
        public string OrdType { get; set; }

        [JsonProperty(PropertyName = "7552")]
        public string RoutingPolicy { get; set; }
    }
}
