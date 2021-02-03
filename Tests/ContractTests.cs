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
using NUnit.Framework;
using QuantConnect.Atreyu.Client.Messages;

namespace QuantConnect.Atreyu.Tests
{
    [TestFixture]
    public class ContractTests
    {
        [Test]
        public void DeserializeHeartbeat()
        {
            var json = @"{
                ""MsgType"":""Heartbeat"",
                ""status"":0,
                ""Text"":""FIX session heartbeat"",
                ""SendingTime"":""20210128-21:02:32.118""
            }";

            JObject token = JObject.Parse(json);
            Assert.True(token.TryGetValue("MsgType", StringComparison.OrdinalIgnoreCase, out JToken msgType));
            Assert.AreEqual("Heartbeat", msgType.Value<string>());
            Assert.False(token.TryGetValue("ExecType", StringComparison.OrdinalIgnoreCase, out _));
        }

        [Test]
        public void SerializeLogon()
        {
            var usernameStr = "TEST";
            var passwordStr = "0123456789";
            var logonMessage = new LogonMessage(usernameStr, passwordStr);
            var json = JsonConvert.SerializeObject(logonMessage);

            JObject token = JObject.Parse(json);
            Assert.True(token.TryGetValue("35", StringComparison.OrdinalIgnoreCase, out JToken msgType));
            Assert.AreEqual("A", msgType.Value<string>());
            Assert.True(token.TryGetValue("553", StringComparison.OrdinalIgnoreCase, out JToken username));
            Assert.AreEqual(usernameStr, username.Value<string>());
            Assert.True(token.TryGetValue("554", StringComparison.OrdinalIgnoreCase, out JToken password));
            Assert.AreEqual(passwordStr, password.Value<string>());
        }

        [Test]
        public void DeserializeLogonResponseSuccess()
        {
            var json = @"{
               ""sessionid"":""6070afc5-1ef0-4ced-bb45-92a2ed813849"",
               ""positions"":[],
               ""orders"":[],
               ""MsgType"":""Logon"",
               ""status"":0,
               ""Text"":""FIX Connection OK"",
               ""SendingTime"":""20210128-21:36:17.638""
            }";

            var response = JsonConvert.DeserializeObject<LogonResponseMessage>(json);
            Assert.AreEqual("6070afc5-1ef0-4ced-bb45-92a2ed813849", response.SessionId);
            Assert.AreEqual(0, response.Status);
            Assert.IsNotEmpty(response.Text);
        }

        [Test]
        public void DeserializeLogonResponseFail()
        {
            var json = @"{
                ""MsgType"":""Logon"",
                ""status"":121,
                ""SendingTime"":""20210128-22:05:03.512"",
                ""58"":""FLIRT ERROR - INVALID PASSWORD""
            }";

            var response = JsonConvert.DeserializeObject<LogonResponseMessage>(json);
            Assert.Null(response.SessionId);
            Assert.Greater(response.Status, 0);
            Assert.IsNotEmpty(response.Text);
        }

        [Test]
        public void DeserializeSubmitResponseFail()
        {
            var json = @"{
                ""MsgType"":""D"",
                ""status"":1132,
                ""SendingTime"":""20210203-19:12:05.091"",
                ""58"":""FLIRT - ERROR TAG:TimeInForce UNKNOWN: (null)""
            }";

            var response = JsonConvert.DeserializeObject<SubmitResponseMessage>(json);
            Assert.Greater(response.Status, 0);
            Assert.IsNotEmpty(response.Text);
            Assert.IsNotEmpty(response.SendingTime);
        }

        [Test]
        [TestCaseSource(nameof(ExecutionReports))]
        public void DeserializeNotExecSubscribeResponse(string json)
        {
            var report = JsonConvert.DeserializeObject<ExecutionReport>(json);
            Assert.AreEqual(typeof(ExecutionReport), report.GetType());
            Assert.IsNotEmpty(report.ExecType);
            Assert.IsNotEmpty(report.Text);
        }

        public static TestCaseData[] ExecutionReports => new[]
        {
            // new
            new TestCaseData(@"{
               ""MsgType"":""ExecutionReport"",
               ""SendingTime"":""20210203-21:04:55.132"",
               ""OnBehalfOfCompID"":""CERTSIM-TM-06"",
               ""Account"":""ACC1"",
               ""AvgPx"":0,
               ""ClOrdID"":""d5489e6095f8412ab763c597ff53fb9a"",
               ""CumQty"":0,
               ""ExecID"":""E-2186543106"",
               ""ExecTransType"":""NEW"",
               ""LastPx"":0,
               ""LastShares"":0,
               ""OrderID"":""O-2186543106"",
               ""OrderQty"":100,
               ""OrdStatus"":""NEW"",
               ""Price"":50,
               ""Side"":""BUY"",
               ""Symbol"":""ORCL"",
               ""TransactTime"":""20210203-21:04:55.134"",
               ""ExecType"":""NEW"",
               ""LeavesQty"":100,
               ""SecondaryOrderID"":""d5489e6095f8412ab763c597ff53fb9a"",
               ""status"":0
            }"),
            new TestCaseData(@"{
               ""MsgType"":""ExecutionReport"",
               ""SendingTime"":""20210203-21:32:13.135"",
               ""OnBehalfOfCompID"":""CERTSIM-TM-06"",
               ""Account"":""ACC1"",
               ""AvgPx"":0,
               ""ClOrdID"":""6ac9d350d1e441cabbd32615f44177ff"",
               ""CumQty"":0,
               ""ExecID"":""E-2186543107"",
               ""ExecTransType"":""NEW"",
               ""LastPx"":0,
               ""LastShares"":0,
               ""OrderID"":""O-2186543106"",
               ""OrderQty"":100,
               ""OrdStatus"":""PENDING_CANCEL"",
               ""OrigClOrdID"":""d5489e6095f8412ab763c597ff53fb9a"",
               ""Price"":50,
               ""Side"":""BUY"",
               ""Symbol"":""ORCL"",
               ""TransactTime"":""20210203-21:32:13.135"",
               ""ExecType"":""PENDING_CANCEL"",
               ""LeavesQty"":100,
               ""SecondaryOrderID"":""d5489e6095f8412ab763c597ff53fb9a"",
               ""status"":0
            }"),
            new TestCaseData(@"{
               ""MsgType"":""ExecutionReport"",
               ""SendingTime"":""20210203-21:32:13.176"",
               ""OnBehalfOfCompID"":""CERTSIM-TM-06"",
               ""Account"":""ACC1"",
               ""AvgPx"":0,
               ""ClOrdID"":""6ac9d350d1e441cabbd32615f44177ff"",
               ""CumQty"":0,
               ""ExecID"":""E-2186543108"",
               ""ExecTransType"":""NEW"",
               ""LastPx"":0,
               ""LastShares"":0,
               ""OrderID"":""O-2186543106"",
               ""OrderQty"":100,
               ""OrdStatus"":""CANCELED"",
               ""OrigClOrdID"":""d5489e6095f8412ab763c597ff53fb9a"",
               ""Price"":50,
               ""Side"":""BUY"",
               ""Symbol"":""ORCL"",
               ""Text"":""Cancelled by simulator, no reason provided!"",
               ""TransactTime"":""20210203-21:32:13.139"",
               ""ExecType"":""CANCELED"",
               ""LeavesQty"":0,
               ""SecondaryOrderID"":""d5489e6095f8412ab763c597ff53fb9a"",
               ""status"":0
            }")
        };
    }
}
