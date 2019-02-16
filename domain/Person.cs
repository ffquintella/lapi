using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace lapi.domain
{
    public class Person: BaseEntry
    {
        [Required]
        public string Name { get; set; }
        
        public string Surname { get; set; }
        public string Description { get; set; }

        public string DN { get; set; }
        
        public List<string> Mails { get; set; }
        
        public List<string> Phones { get; set; }
        
        public List<string> Mobiles { get; set; }
        
        public List<string> Addresses { get; set; }
        
        public List<string> IDs { get; set; }
        
        public string State { get; set; }
        
        public string GivenName { get; set; }

        public string Password { get; set; }

        public bool? IsDisabled { get; set; }



        public Person()
        {
        }


    }
}
