using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace User.Manage.API.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
        public virtual ICollection<UserRole> Roles { get; } = new List<UserRole>();
    }

    public class UserRole : IdentityUserRole<int>
    {
        [ForeignKey("RoleId")]
        public virtual ApplicationUserRole Role { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;
    }

    public class ApplicationUserRole : IdentityRole<int>
    {
        public virtual ICollection<IdentityRoleClaim<int>> Claims { get; set; } =
            new List<IdentityRoleClaim<int>>();
    }
}
