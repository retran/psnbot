using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSNBot.Model
{
    public class Image
    {
        public DateTime TimeStamp { get; set; }

        public byte[] Data { get; set; }
        public string Source { get; set; }
    }
}
