using System;
using System.ComponentModel.DataAnnotations;

namespace lapi.domain
{
    public class AuthenticationRequest
    {

        public string Login { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
