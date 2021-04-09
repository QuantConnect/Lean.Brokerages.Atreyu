using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Orders;

namespace QuantConnect.Atreyu.Certification
{
    public class Execution
    {
        public int OrderId { get; set; }

        public string Symbol { get; set; }

        public Queue<ExecutionEvent> Executions { get; set; }

        public Execution(OrderTicket ticket)
        {
            OrderId = ticket.OrderId;
            Symbol = ticket.Symbol.Value;
        }

        public void Assert(OrderEvent orderEvent)
        {
            var expected = Executions.Dequeue();

            if (orderEvent.Status != expected.Status)
            {
                throw new Exception($"Unexpected Order status. Expected {expected.Status}, but was {orderEvent.Status}");
            }

            if (orderEvent.FillQuantity != expected.FillQuantity)
            {
                throw new Exception($"Unexpected Fill Quantity. Expected {expected.FillQuantity}, but was {orderEvent.FillQuantity}");
            }
        }
    }

    public class ExecutionEvent
    {
        public OrderStatus Status { get; set; }

        public decimal FillQuantity { get; set; }

        public override bool Equals(object obj) => Equals(obj as ExecutionEvent);

        public bool Equals(ExecutionEvent other)
        {
            return other != null &&
                   Status == other.Status &&
                   FillQuantity == other.FillQuantity;
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 41;
                hash = hash * 59 + Status.GetHashCode();
                hash = hash * 59 + FillQuantity.GetHashCode();
                return hash;
            }
        }
    }
}
