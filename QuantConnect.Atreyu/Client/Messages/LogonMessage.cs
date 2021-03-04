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

namespace QuantConnect.Atreyu.Client.Messages
{
    public class LogonMessage : RequestMessage
    {
        public LogonMessage()
        {
            MsgType = "A";
        }

        public LogonMessage(string username, string password): this()
        {
            Username = username;
            Password = password;
        }

        [JsonProperty(PropertyName = "553")]
        public string Username { get; internal set; }

        [JsonProperty(PropertyName = "554")]
        public string Password { get; internal set; }

        //public int? MsgSeqNum { get; set; }
    }
}
