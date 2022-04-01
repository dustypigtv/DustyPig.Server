using System;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    public class LogEntry
    {
        public int Id { get; set; }

        public DateTime Timestamp { get; set; }

        [MaxLength(250)]
        public string Logger { get; set; }

        [MaxLength(250)]
        public string CallSite { get; set; }

        [MaxLength(12)]
        public string Level { get; set; }

        [MaxLength(4000)]
        public string Message { get; set; }

        public string Exception { get; set; }
    }
}
