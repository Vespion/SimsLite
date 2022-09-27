using System.ComponentModel.DataAnnotations.Schema;

namespace AuthServer.Data
{
    public class KeyChallenge : AuthenticationSession
    {
        public string Challenge { get; set; } = null!;

        [ForeignKey(nameof(Device))]
        public string DeviceId { get; set; } = null!;

        public virtual Device Device { get; set; } = null!;
    }
}