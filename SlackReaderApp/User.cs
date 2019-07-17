using System.Collections.Generic;

namespace SlackReaderApp
{
    public class User
    {
        public string ID { get; set; }
        public string Team_id { get; set; }
        public string Name { get; set; }
        public string Deleted { get; set; }
        public string Color { get; set; }
        public string Real_name { get; set; }
        public string TZ_label { get; set; }
        public string TZ_offset { get; set; }
        public string Profile { get; set; }
        public string Is_admin { get; set; }
        public string Is_owner { get; set; }
        public string Is_primary_owner { get; set; }
        public string Is_restricted { get; set; }
        public string Is_ultra_restricted { get; set; }
        public string Is_bot { get; set; }
        public string Updated { get; set; }

        public profile profile { get; set; }

    }

    public class profile
    {
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string title { get; set; }
        public string skype { get; set; }
        public string phone { get; set; }

    }

    public class UserName
    {
        public string Name;
        public string Real_name;
    }

    //public class Reaction
    //{
    //    public string name { get; set; }
    //    public List<string> users { get; set; }
    //    public int count { get; set; }
    //}

    public class InitialComment
    {
        public string id { get; set; }
        public int created { get; set; }
        public int timestamp { get; set; }
        public string user { get; set; }
        public bool is_intro { get; set; }
        public string comment { get; set; }
    }

    public class File
    {
        public string id { get; set; }
        public int created { get; set; }
        public int timestamp { get; set; }
        public string name { get; set; }
        public string title { get; set; }
        public string mimetype { get; set; }
        public string filetype { get; set; }
        public string pretty_type { get; set; }
        public string user { get; set; }
        public bool editable { get; set; }
        public int size { get; set; }
        public string mode { get; set; }
        public bool is_external { get; set; }
        public string external_type { get; set; }
        public bool is_public { get; set; }
        public bool public_url_shared { get; set; }
        public bool display_as_bot { get; set; }
        public string username { get; set; }
        public string url_private { get; set; }
        public string url_private_download { get; set; }
        public string thumb_64 { get; set; }
        public string thumb_80 { get; set; }
        public string thumb_360 { get; set; }
        public int thumb_360_w { get; set; }
        public int thumb_360_h { get; set; }
        public string thumb_480 { get; set; }
        public int thumb_480_w { get; set; }
        public int thumb_480_h { get; set; }
        public string thumb_160 { get; set; }
        public string thumb_720 { get; set; }
        public int thumb_720_w { get; set; }
        public int thumb_720_h { get; set; }
        public string thumb_960 { get; set; }
        public int thumb_960_w { get; set; }
        public int thumb_960_h { get; set; }
        public string thumb_1024 { get; set; }
        public int thumb_1024_w { get; set; }
        public int thumb_1024_h { get; set; }
        public int image_exif_rotation { get; set; }
        public int original_w { get; set; }
        public int original_h { get; set; }
        public string permalink { get; set; }
        public string permalink_public { get; set; }
        public List<string> channels { get; set; }
        public List<object> groups { get; set; }
        public List<object> ims { get; set; }
        public int comments_count { get; set; }
        public List<Reaction> reactions { get; set; }
        public InitialComment initial_comment { get; set; }
    }

    public class Comment
    {
        public string id { get; set; }
        public int created { get; set; }
        public int timestamp { get; set; }
        public string user { get; set; }
        public bool is_intro { get; set; }
        public string comment { get; set; }
    }

    public class RootObject
    {
        public string type { get; set; }
        public string subtype { get; set; }
        public string text { get; set; }
        public File file { get; set; }
        public Comment comment { get; set; }
        public bool is_intro { get; set; }
        public string ts { get; set; }
        public string user { get; set; }
        public bool? upload { get; set; }
        public bool? display_as_bot { get; set; }
        public string username { get; set; }
        public object bot_id { get; set; }
    }
}
