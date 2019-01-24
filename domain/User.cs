using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace lapi.domain
{
    public class User
    {
        [Required]
        public string Name { get; set; }
        
        public string Surname { get; set; }
        public string Description { get; set; }

        public string DN { get; set; }

        public string Password { get; set; }


        public bool? IsDisabled { get; set; }
        public bool IsLocked { get; set; }
        public bool PasswordExpired { get; set; }




        public User()
        {
        }


    }
}
