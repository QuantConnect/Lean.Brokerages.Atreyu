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
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using QuantConnect.Brokerages;

namespace QuantConnect.Atreyu
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
            { "atreyu-password", Config.Get("atreyu-password")},
            { "atreyu-client-id", Config.Get("atreyu-client-id")},
            { "atreyu-cash-balance", Config.Get("atreyu-cash-balance")}
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
            var errors = new List<string>();
            var host = Read<string>(job.BrokerageData, "atreyu-host", errors);
            var requestPort = Read<int>(job.BrokerageData, "atreyu-req-port", errors);
            var subscribePort = Read<int>(job.BrokerageData, "atreyu-sub-port", errors);
            var username = Read<string>(job.BrokerageData, "atreyu-username", errors);
            var password = Read<string>(job.BrokerageData, "atreyu-password", errors);
            var clientId = Read<string>(job.BrokerageData, "atreyu-client-id", errors);
            var cashBalance = Read<decimal>(job.BrokerageData, "atreyu-cash-balance", errors);

            if (errors.Count != 0)
            {
                // if we had errors then we can't create the instance
                throw new Exception(string.Join(Environment.NewLine, errors));
            }

            var brokerage = new AtreyuBrokerage(
                host, 
                requestPort, 
                subscribePort, 
                username, 
                password,
                clientId,
                cashBalance,
                algorithm);

            return brokerage;
        }
    }
}
