using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Enerweb.Domain.Models
{
    public class Site
    {
        public Guid Id { get; set; }

        public string SiteName { get; set; }
        public string SiteCode { get; set; }


        public bool IsActive { get; set; }

        // Navigation properties
        public ICollection<Meters> Meters { get; set; } = new List<Meters>();

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
