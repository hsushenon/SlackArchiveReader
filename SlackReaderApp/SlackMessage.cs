using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackReaderApp
{
    public class SlackMessage
    {
        public string User { get; set; }
        public string Purpose { get; set; }
        public string Inviter { get; set; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public string Text { get; set; }
        //public attachments Attachments { get; set; }
        public string TS { get; set; }
        public List<File> files { get; set; }
        public List<Reaction> reactions { get; set; }
        public List<attachments> attachments { get; set; }

    }

    public class Reaction
    {
        public string name { get; set; }
        public List<string> users { get; set; }
        public int count { get; set; }
    }

    public class attachments
    {
        public string title { get; set; }
        public string id { get; set; }
        public string title_link { get; set; }
        public string fallback { get; set; }
        public string text { get; set; }
        public string from_url { get; set; }
        public string service_icon { get; set; }
        public string service_name { get; set; }
        public string service_url { get; set; }
        public string author_name { get; set; }
        public string author_link { get; set; }
        public string thumb_url { get; set; }
        public string thumb_width { get; set; }
        public string thumb_height { get; set; }
        //public string thumb_width { get; set; }



    }
}