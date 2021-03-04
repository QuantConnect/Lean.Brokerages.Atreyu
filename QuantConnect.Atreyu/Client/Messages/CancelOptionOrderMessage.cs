﻿/*
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

namespace QuantConnect.Atreyu.Client.Messages
{
    public class CancelOptionOrderMessage : CancelEquityOrderMessage
    {
        [JsonProperty(PropertyName = "200")]
        public string MaturityMonthYear { get; set; }

        [JsonProperty(PropertyName = "201", Required = Required.DisallowNull)]
        public string PutOrCall { get; set; }

        [JsonProperty(PropertyName = "202", Required = Required.DisallowNull)]
        public string StrikePrice { get; set; }

        [JsonProperty(PropertyName = "205")]
        public string MaturityDay { get; set; }

        [JsonProperty(PropertyName = "541")]
        public string MaturityDate { get; set; }
    }
}
