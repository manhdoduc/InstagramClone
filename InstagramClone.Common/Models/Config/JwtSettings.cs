using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Common.Models.Config
{
    public sealed class JwtSettings
    {
        public string Issuer { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;
        public string Key { get; init; } = string.Empty;
        public int ExpiryInMinutes { get; init; }
        public int RefreshTokenExpiryTime { get; init; }
    }
}
