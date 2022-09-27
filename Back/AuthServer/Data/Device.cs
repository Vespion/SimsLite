using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace AuthServer.Data
{
    public class Device
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; } = null!;

        [ForeignKey(nameof(User))]
        [PersonalData]
        public Guid UserId { get; set; }

        [PersonalData]
        public string? NotificationUrl { get; set; }

        [ProtectedPersonalData]
        public string DeviceName { get; set; } = null!;

        public virtual User User { get; set; } = null!;
    }
}