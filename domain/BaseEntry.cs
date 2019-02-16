using System;

namespace lapi.domain
{
    public class BaseEntry
    {
        //var dt = new DateTime(1601, 01, 01, 0, 0, 0, DateTimeKind.Utc).AddTicks(accountExpires);
        public DateTime CreateTime { get; set; }
        //public string Creator { get; set; }
        public DateTime ModifyTime { get; set; }
        //public string LastModifier { get; set; }
    }
}