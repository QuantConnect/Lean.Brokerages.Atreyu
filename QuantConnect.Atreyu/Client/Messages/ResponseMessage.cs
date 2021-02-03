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
    /// <summary>
    /// Base Response model. Use for Request-Reply and Publish-Subscribe
    /// </summary>
    public class ResponseMessage
    {
        /// <summary>
        /// Status of the response, 0 is success
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public int Status { get; set; }

        /// <summary>
        /// Free format status text
        /// </summary>
        [JsonProperty(PropertyName = "Text")]
        public string Text { get; set; }

        /// <summary>
        /// Fallback for Text
        /// Received when order submission fails
        /// </summary>
        [JsonProperty(PropertyName = "58")]
        public string Text58
        {
            set => Text = value;
        }

        public string SendingTime { get; set; }
    }
}
