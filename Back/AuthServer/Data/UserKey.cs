using System.ComponentModel.DataAnnotations.Schema;

namespace AuthServer.Data
{
    public class UserKey
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(Device))]
        public string DeviceId { get; set; } = null!;

        public string? Attestation { get; set; }

        public byte[] PublicKey { get; set; } = null!;

        public virtual Device Device { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}