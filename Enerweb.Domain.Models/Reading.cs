using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Enerweb.Domain.Models
{
    public class Reading
    {
        public Guid Id { get; set; }
        public Guid MeterId { get; set; }
        public decimal Value { get; set; }
        public DateTime Timestamp { get; set; }

        // Navigation properties
        public virtual Meters Meter { get; set; }
    }
}
