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
    public class ExecutionReportJsonConverterTests
    {
        public static TestCaseData[] ExtraMessages => new[]
        {
            new TestCaseData(@"{
                ""MsgType"":""Heartbeat"",
                ""status"":0,
                ""Text"":""FIX session heartbeat"",
                ""SendingTime"":""20210128-21:02:32.118""
            }"),
            new TestCaseData(@"{
               ""MsgType"":""PositionReport"",
               ""Symbol"":""GE"",
               ""SecurityType"":""CS"",
               ""PosType"":""ELECTRONIC_TRADE_QTY"",
               ""LongQty"":100,
               ""ShortQty"":0,
               ""status"":0
            }"),
            new TestCaseData(@"{
               ""MsgType"":""ExecutionReport"",
               ""SendingTime"":""20210204-18:24:07.388"",
               ""OnBehalfOfCompID"":""CERTSIM-TM-06"",
               ""Account"":""ACC1"",
               ""AvgPx"":50,
               ""ClOrdID"":""99fea658d67e4d82bc4e9ec1a567e15a"",
               ""CumQty"":110,
               ""ExecID"":""E-3553951751"",
               ""ExecTransType"":""NEW"",
               ""LastMkt"":""ATRU"",
               ""LastPx"":50,
               ""LastShares"":38,
               ""OrderID"":""O-3553951746"",
               ""OrderQty"":110,
               ""OrdStatus"":""FILLED"",
               ""OrigClOrdID"":""de45c9c024e744399712b5554bcc909d"",
               ""Price"":50,
               ""Side"":""BUY"",
               ""Symbol"":""BAC"",
               ""TransactTime"":""20210204-18:24:07.387"",
               ""LeavesQty"":0,
               ""SecondaryOrderID"":""de45c9c024e744399712b5554bcc909d"",
               ""status"":0
            }")
        };

        [Test, TestCaseSource(nameof(ExtraMessages))]
        public void ThrowsIfNoExecTypeTag(string json)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                JsonConvert.DeserializeObject<FillOrderReport>(json);
            });
        }
    }
}
