﻿using System.ComponentModel.DataAnnotations;

namespace Threads.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage ="Continutul este obligatoriu")]
        public string Content { get; set; }
        public DateTime Date { get; set; }
        public int? PostId { get; set; }
        public virtual Post? Post { get; set; }

        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
    }
}
