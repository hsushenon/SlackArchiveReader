using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SlackReaderApp
{
    public partial class SlackReaderForm : Form
    {
        //string m_ArchiveFolderPath = @"D:\Others\BowbazarSlackTest";
        string m_OutputFolderPath = @"C:\Data\SlackOutput";
        string m_ArchiveFolderPath = @"C:\Data\Bowbazar Slack export Jun 2 2017";
        string m_UsersFilerPath = @"C:\Data\Bowbazar Slack export Jun 2 2017\users.json";
        Dictionary<string, string> m_UserDic = new Dictionary<string, string>();
        public SlackReaderForm()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                //read users
                string users = File.ReadAllText(m_UsersFilerPath);
                List<User> userList = JsonConvert.DeserializeObject<List<User>>(users);
                foreach (User u in userList)
                {
                    m_UserDic.Add(u.ID, u.Real_name);
                }

                //read channel folders
                foreach (string folder in Directory.EnumerateDirectories(m_ArchiveFolderPath))
                {
                    //Get channel name, folder name
                    string channelName = folder.Substring(folder.LastIndexOf("\\") + 1);

                    string path = m_OutputFolderPath + @"\" + channelName + ".html";
                    // Delete the file if it exists.
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }

                    HtmlGenerator gen = new HtmlGenerator(channelName);

                    //Read messages
                    foreach (string file in Directory.EnumerateFiles(folder, "*.json"))
                    {
                        //Get date file name
                        string dateMessage = file.Substring(file.LastIndexOf("\\") + 1).Replace(".json", "");

                        dateMessage = "---" + dateMessage + "---";

                        gen.AddContent(dateMessage, true);
                        string contents = File.ReadAllText(file);

                        List<SlackMessage> smList = JsonConvert.DeserializeObject<List<SlackMessage>>(contents);

                        foreach (SlackMessage sm in smList)
                        {
                            double time = double.Parse(sm.TS);
                            DateTime dt = UnixTimeStampToDateTime(time);

                            string name = string.Empty;

                            if (sm.User != null && m_UserDic.ContainsKey(sm.User))
                                name = m_UserDic[sm.User];
                            else  //There is no user set here, need special handling
                                if (sm.SubType != null && sm.SubType.Equals("file_comment"))
                            {
                                //TODO 
                            }

                            gen.AddContent(name + " " + dt.ToLongTimeString(), true);
                            gen.AddContent(sm.Text, false);
                        }
                    }

                    string fullContent = gen.GetFullContent();
                    File.AppendAllText(path, fullContent);
                }

                MessageBox.Show("Completed");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

    
    }
}
