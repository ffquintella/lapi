using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace lapi.domain
{
    public class Group
    {
        public Group()
        {
        }

        [Required]
        public string Name { get; set; }
        public string Description { get; set; }


        public string DN { get; set; }
        
        public GroupType Type { get; set; }

        private List<String> _member;
        [Required]
        public List<String> Member
        {
            get
            {
                if (_member == null) _member = new List<String>();
                return _member;
            }
            set
            {
                _member = value;
            }
        }

    }
}
