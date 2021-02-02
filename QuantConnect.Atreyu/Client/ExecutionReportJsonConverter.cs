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
using Newtonsoft.Json.Linq;
using QuantConnect.Atreyu.Client.Messages;

namespace QuantConnect.Atreyu.Client
{
    public class ExecutionReportJsonConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("The ExecutionReportJsonConverter does not implement a WriteJson method");
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer
            )
        {
            JObject token = JObject.Load(reader);
            var execType = token.GetValue("ExecType", StringComparison.OrdinalIgnoreCase)?.Value<string>();
            if (string.IsNullOrEmpty(execType))
            {
                throw new ArgumentException("Execution report type is not specified.");
            }

            ExecutionReport report;
            switch (execType)
            {
                case "PARTIAL_FILL":
                case "FILL":
                    report = new FillOrderReport();
                    break;
                default:
                    report = new ExecutionReport();
                    break;

            }

            serializer?.Populate(token.CreateReader(), report);
            return report;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(ExecutionReport).IsAssignableFrom(objectType);
        }
    }
}
