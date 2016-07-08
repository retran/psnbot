using System;

namespace PSNBot.Model
{
    public class Image
    {
        public DateTime TimeStamp { get; set; }

        public byte[] Data { get; set; }
        public string Source { get; set; }
    }
}
