using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Enerweb.Domain.Models
{
    public class ReportRequest
    {

            public Guid Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public string? Description { get; set; }

            public string Entity { get; set; } = string.Empty;

            public string QueryDefinition { get; set; } = string.Empty;

            public Guid RequestedBy { get; set; }

            public DateTime CreatedAt { get; set; }

            public DateTime? LastExecutedAt { get; set; }

            public bool IsSaved { get; set; }

    }
}
