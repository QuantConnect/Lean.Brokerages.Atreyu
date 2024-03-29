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
    public class Position
    {
        public string Symbol { get; set; }
        
        public string SecurityType { get; set; }

        // [JsonProperty(PropertyName = "703", Required = Required.DisallowNull)]
        // (ETR - position as result of trading) and (SOD - position from the Start Of Day)
        // https://quantconnect.slack.com/archives/G01G4CC5A2K/p1608571876012700
        //public string PosType { get; set; } 

        public decimal LongQty { get; set; }
        
        public decimal ShortQty { get; set; }
    }
}
