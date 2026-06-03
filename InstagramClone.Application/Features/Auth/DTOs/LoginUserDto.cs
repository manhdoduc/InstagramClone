using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.Features.Auth.DTOs{
    public class LoginUserDto
    {
        public string Identifier { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
