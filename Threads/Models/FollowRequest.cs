using Microsoft.Build.Execution;
using System.ComponentModel.DataAnnotations;

namespace Threads.Models
{
    public class FollowRequest
    {
        public int Id { get; set; }
        public string? SenderId { get; set; } // ID of the user who sent the request
        public string? ReceiverId { get; set; } // ID of the user who received the request

        // Navigation properties
        public virtual ApplicationUser? Sender { get; set; }
        public virtual ApplicationUser? Receiver { get; set; }

        public string? Status { get; set; } // pending, accepted, rejected
    }
}
