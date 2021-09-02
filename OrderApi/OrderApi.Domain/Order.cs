using System;
using System.Collections.Generic;
using System.Text;

namespace OrderApi.Domain
{
    public partial class Order
    {
        public Guid Id { get; set; }
        public int OrderState { get; set; }
        public Guid CustomerGuid { get; set; }
        public string CustomerFullName { get; set; }
    }
}
