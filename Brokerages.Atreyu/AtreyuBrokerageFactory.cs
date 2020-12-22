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

using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;
using System;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Atreyu
{
    /// <summary>
    /// Factory class to create Atreyu brokerage
    /// </summary>
    public class AtreyuBrokerageFactory : BrokerageFactory
    {
        /// <summary>
        /// Factory constructor
        /// </summary>
        public AtreyuBrokerageFactory() : this(typeof(AtreyuBrokerage))
        {
        }

        /// <summary>
        /// Factory constructor
        /// </summary>
        public AtreyuBrokerageFactory(Type brokerageType) : base(brokerageType)
        {
        }

        /// <summary>
        /// Not required
        /// </summary>
        public override void Dispose()
        {
        }

        /// <summary>
        /// Provides brokerage connection data
        /// </summary>
        public override Dictionary<string, string> BrokerageData => new Dictionary<string, string>
        {
            { "atreyu-host", Config.Get("atreyu-host")},
            { "atreyu-req-port", Config.Get("atreyu-req-port")},
            { "atreyu-sub-port", Config.Get("atreyu-sub-port")},
            { "atreyu-username", Config.Get("atreyu-username")},
            { "atreyu-password", Config.Get("atreyu-password")}
        };

        /// <summary>
        /// The Atreyu brokerage model
        /// </summary>
        /// <param name="orderProvider">The order provider</param>
        public override IBrokerageModel GetBrokerageModel(IOrderProvider orderProvider) => new AtreyuBrokerageModel();

        /// <summary>
        /// Create the Brokerage instance
        /// </summary>
        /// <param name="job"></param>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public override IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm)
        {
            var required = new[] {
                "atreyu-host",
                "atreyu-req-port",
                "atreyu-sub-port",
                "atreyu-username",
                "atreyu-password"
            };

            foreach (var item in required)
            {
                if (string.IsNullOrEmpty(job.BrokerageData[item]))
                    throw new Exception($"AtreyuBrokerageFactory.CreateBrokerage: Missing {item} in config.json");
            }

            var brokerage = new AtreyuBrokerage(
                job.BrokerageData["atreyu-host"],
                job.BrokerageData["atreyu-req-port"].ToInt32(),
                job.BrokerageData["atreyu-sub-port"].ToInt32(),
                job.BrokerageData["atreyu-username"],
                job.BrokerageData["atreyu-password"],
                algorithm,
                Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager")));
            Composer.Instance.AddPart<IDataQueueHandler>(brokerage);

            return brokerage;
        }
    }
}
