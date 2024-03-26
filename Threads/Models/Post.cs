using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Threads.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        /*[Required(ErrorMessage = "Nu poți adăuga o postare fără conținut")]*/
        public string? Content { get; set; }

        public DateTime Date { get; set; }

        public int? GroupId { get; set; }
        public virtual Group? Group { get; set; }

        public virtual ICollection<Comment>? Comments { get; set;}

        [NotMapped]
        public IEnumerable<SelectListItem>? Groups {get; set;}


        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
    }
}
