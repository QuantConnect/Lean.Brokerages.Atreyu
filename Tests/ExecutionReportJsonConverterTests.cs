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
            }"),
            new TestCaseData(@"{
               ""MsgType"":""OrderCancelReject"",
               ""SendingTime"":""20210409-20:30:22.066"",
               ""ClOrdID"":""99290bbce87b49de84fd259b2f87acb0"",
               ""OrderID"":""O-168493115"",
               ""OrdStatus"":""NEW"",
               ""OrigClOrdID"":""1d4bbf69ea3c4a9ab31cc11e67483025"",
               ""Text"":""Cancel request rejected during T5"",
               ""TransactTime"":""20210409-20:30:22.028"",
               ""ClientID"":""QC-TEST1"",
               ""CxlRejResponseTo"":""ORDER_CANCEL_REQUEST"",
               ""MsgSeqNum"":263,
               ""status"":0,
               ""SecondaryOrderID"":""1d4bbf69ea3c4a9ab31cc11e67483025""
            }")
        };

        [Test, TestCaseSource(nameof(ExtraMessages))]
        public void ThrowsIfNoExecTypeTag(string json)
        {
            // expect either "ExecType" or "CxlRejReason"

            Assert.Throws<ArgumentException>(() =>
            {
                JsonConvert.DeserializeObject<FillOrderReport>(json);
            });
        }

        public static TestCaseData[] MainMessages => new[]
        {
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
               ""ExecType"":""FILL"",
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
            }", typeof(FillOrderReport)),
            new TestCaseData(@"{
               ""MsgType"":""OrderCancelReject"",
               ""SendingTime"":""20210409-20:30:22.066"",
               ""ClOrdID"":""99290bbce87b49de84fd259b2f87acb0"",
               ""OrderID"":""O-168493115"",
               ""OrdStatus"":""NEW"",
               ""OrigClOrdID"":""1d4bbf69ea3c4a9ab31cc11e67483025"",
               ""Text"":""Cancel request rejected during T5"",
               ""TransactTime"":""20210409-20:30:22.028"",
               ""CxlRejReason"":2,
               ""ClientID"":""QC-TEST1"",
               ""CxlRejResponseTo"":""ORDER_CANCEL_REQUEST"",
               ""MsgSeqNum"":263,
               ""status"":0,
               ""SecondaryOrderID"":""1d4bbf69ea3c4a9ab31cc11e67483025""
            }", typeof(OrderCancelRejectReport)),
            new TestCaseData(@"{
               ""MsgType"":""ExecutionReport"",
               ""SendingTime"":""20210409-17:53:07.345"",
               ""AvgPx"":0,
               ""ClOrdID"":""bba6bdf67a0b4e5aaa3e51c9e01cc789"",
               ""CumQty"":0,
               ""ExecID"":""E-168493058"",
               ""ExecTransType"":""NEW"",
               ""LastPx"":0,
               ""LastShares"":0,
               ""OrderID"":""O-168493058"",
               ""OrderQty"":100,
               ""OrdStatus"":""NEW"",
               ""Price"":5,
               ""Side"":""BUY"",
               ""Symbol"":""GE"",
               ""TransactTime"":""20210409-17:53:07.346"",
               ""ClientID"":""QC-TEST1"",
               ""ExecType"":""NEW"",
               ""LeavesQty"":100,
               ""EffectiveTime"":""20210409-17:53:07.346"",
               ""MsgSeqNum"":4,
               ""SecondaryOrderID"":""bba6bdf67a0b4e5aaa3e51c9e01cc789"",
               ""status"":0,
               ""PossDupFlag"":true
            }", typeof(ExecutionReport))
        };
        [Test, TestCaseSource(nameof(MainMessages))]
        public void ParseExecTypeTag(string json, Type type)
        {
            Assert.DoesNotThrow(() =>
            {
                Assert.IsInstanceOf(
                    type,
                JsonConvert.DeserializeObject<ExecutionReport>(json));
            });
        }
    }
}
