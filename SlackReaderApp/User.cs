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
}
