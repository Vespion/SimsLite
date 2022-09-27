using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace AuthServer.Data
{
    public class User : IdentityUser<Guid>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="IdentityUser"/>.
        /// </summary>
        /// <remarks>
        /// The Id property is initialized to form a new GUID string value.
        /// </remarks>
        public User()
        {
            base.Id = Guid.NewGuid();
            base.SecurityStamp = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="IdentityUser"/>.
        /// </summary>
        /// <param name="userName">The user name.</param>
        /// <remarks>
        /// The Id property is initialized to form a new GUID string value.
        /// </remarks>
        public User(string userName) : this()
        {
            base.UserName = userName;
        }

        [ProtectedPersonalData]
        public string FirstName { get; set; } = null!;

        [ProtectedPersonalData]
        public string LastName { get; set; } = null!;

        public virtual ICollection<Token> Tokens { get; set; } = null!;

        public virtual ICollection<Device> Devices { get; set; } = null!;

        public virtual ICollection<UserKey> Keys { get; set; } = null!;
    }

    public class Token
    {
        public string Id { get; set; } = null!;

        public string SecurityToken { get; set; } = null!;

        public bool Refresh { get; set; }

        [ForeignKey(nameof(User))]
        [PersonalData]
        public Guid UserId { get; set; }

        public virtual User User { get; set; } = null!;
    }
}