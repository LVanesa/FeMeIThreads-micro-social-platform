//PASUL 1 - useri si roluri
using Microsoft.AspNetCore.Identity;
using NuGet.Protocol.Plugins;

namespace Threads.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public string? ProfilePicture { get; set; }
        public string? CoverPicture { get; set; }
        
        public string? Biografie { get; set; }

        public bool? Visibility { get; set; }
        public ICollection<Post>? Posts { get; set; }
        public ICollection<Comment>? Comments { get; set; }

        public virtual ICollection<GroupUser>? GroupUsers {  get; set; }
        public ApplicationUser()
        {
            // Set the default value for the Visibility property
            Visibility = true; // or false, depending on your default choice
            ProfilePicture = "/images/ghost.png";
            CoverPicture = "/images/coverDefault.png";
        }

    }

}
