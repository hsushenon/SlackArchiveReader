using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SlackReaderApp
{
    public partial class SlackReaderForm : Form
    {
        string m_OutputFolderPath = @"C:\Projects\My projects\SlackReaderApp\Output";
        string m_ArchiveFolderPath = @"C:\Projects\My projects\SlackReaderApp\BowbazarSlackExportJun2017_Test2\";
        string m_UsersFilerPath = @"C:\Projects\My projects\SlackReaderApp\BowbazarSlackExportJun2017_Test\users.json";
        Dictionary<string, UserName> m_UserDic = new Dictionary<string, UserName>();
        private Dictionary<string, string> m_EmoticonDic;
        string m_EmoticonList = @"C:\Projects\My projects\SlackReaderApp\emoticonList.txt";
        // This delegate enables asynchronous calls for setting  
        // the text property on a TextBox control.  
        delegate void StringArgReturningVoidDelegate(string text);
        
        public SlackReaderForm()
        {
            try
            {
                InitializeComponent();


                LoadEmoticons(m_EmoticonList);

                //regex test
                //string message = "asdasd:touge:sdasd";
                //string pattern = @"(:)\S*(:)"; 
                //Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
                //bool hasEmoticon = rgx.IsMatch(message);
                //if (hasEmoticon)
                //{//
                //}
            }
            catch (Exception ex)
            {
                SetText(ex.Message);
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                txtMessage.Text = "Started...";
                btnStart.Enabled = false;
                if (!string.IsNullOrEmpty(txtArchiveFolder.Text))
                    m_ArchiveFolderPath = txtArchiveFolder.Text;

                if (!string.IsNullOrEmpty(txtOutputFolder.Text))
                    m_OutputFolderPath = txtOutputFolder.Text;

                Task t = Task.Factory.StartNew(()
                                    => Start());
                t.ContinueWith(
                               (antecedent) =>
                               {
                                   End();
                               }
                             );
            }
            catch (Exception ex)
            {
                SetText(ex.Message); 
            }
        }

        private void End()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(this.End));
                return;
            }
            else
            {
                this.txtMessage.AppendText("Done.");
                btnStart.Enabled = true;
            }
            
        }

        private void Start()
        {
            try
            {
                //Create folder for image to download
                string imageFolder = m_OutputFolderPath + "\\Images";
                string graphicsFolder = m_OutputFolderPath + "\\Graphics";
                string filesFolder = m_OutputFolderPath + "\\Files";
                if (!Directory.Exists(imageFolder))
                {
                    Directory.CreateDirectory(imageFolder);
                }
                if (!Directory.Exists(filesFolder))
                {
                    Directory.CreateDirectory(filesFolder);
                }
                if (!Directory.Exists(graphicsFolder))
                {
                    MessageBox.Show("Copy the graphics folder in the output folder path");
                    return;
                }

                //read users
                string users = System.IO.File.ReadAllText(m_UsersFilerPath);
                List<User> userList = JsonConvert.DeserializeObject<List<User>>(users);
                foreach (User u in userList)
                {
                    UserName un = new UserName();
                    un.Name = u.Name;
                    un.Real_name = u.Real_name;
                    m_UserDic.Add(u.ID, un);
                }

                

                //read channel folders
                foreach (string folder in Directory.EnumerateDirectories(m_ArchiveFolderPath))
                {
                    //Get channel name, folder name
                    string channelName = folder.Substring(folder.LastIndexOf("\\") + 1);

                    string path = m_OutputFolderPath + @"\" + channelName + ".html";
                    
                    // Delete the file if it exists.
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }

                    HtmlGenerator gen = new HtmlGenerator(channelName, m_EmoticonDic, m_UserDic);

                    SetText(string.Format("Processing channel {0}..", channelName));

                   
                    //Read messages
                    foreach (string file in Directory.EnumerateFiles(folder, "*.json"))
                    {
                        //Get date file name
                        string dateMessage = file.Substring(file.LastIndexOf("\\") + 1).Replace(".json", "");

                        dateMessage = "---" + dateMessage + "---";
                       
                        gen.AddContent(dateMessage, true);

                        string contents = System.IO.File.ReadAllText(file);

                        List<SlackMessage> smList = JsonConvert.DeserializeObject<List<SlackMessage>>(contents);

                        //multiple consecutive messages from the same person in the same day should be without the time stamp
                        string lastUser = string.Empty;
                        foreach (SlackMessage sm in smList)
                        {
                            double time = double.Parse(sm.TS);
                            DateTime dt = UnixTimeStampToDateTime(time);

                            //string name = string.Empty;
                            UserName un;
                            if (sm.User != null && m_UserDic.ContainsKey(sm.User))
                            {
                                un = m_UserDic[sm.User];
                            }
                            else  //There is no user set here, need special handling
                            {
                                un = new UserName();
                                un.Real_name = "nouser";
                                if (sm.SubType != null && sm.SubType.Equals("file_comment"))
                                {
                                    //TODO 
                                    continue;
                                }
                            }

                            if (!un.Real_name.Equals(lastUser))
                            {
                                gen.AddContent(un.Real_name + " " + dt.ToLongTimeString(), true);
                                lastUser = un.Real_name;
                            }

                            //special handling to replace userID with username when join 
                            if (sm.SubType != null)
                            {
                                if (sm.SubType.Equals("channel_join") || sm.SubType.Equals("channel_purpose"))
                                {
                                    //Channel join message is like  "<@U0535FH0Q|hsushenon> has joined the channel"
                                    string currentText = string.Format("<@{0}|{1}>", sm.User, un.Name);
                                    sm.Text = sm.Text.Replace(currentText, un.Real_name);
                                }
                            }



                            if (sm.File != null)
                            {
                                if (sm.File.mimetype.Contains("image"))// for test&& channelName.Equals("photos"))
                                {
                                    //Download image
                                    string fileName = imageFolder + "\\" + sm.File.name;
                                    DownloadImage(sm.File.url_private_download, fileName);

                                    SetText(string.Format("Downloading image {0}", fileName));

                                    string imageTag = string.Format("<img src='Images/{0}' width='{1}' height='{2}'>", sm.File.name, sm.File.thumb_360_w, sm.File.thumb_360_h);
                                    gen.AddContent(imageTag, false);
                                    continue;
                                }
                                else
                                {
                                    //file_share
                                    //Download other types
                                    string fileName = filesFolder + "\\" + sm.File.name;
                                    DownloadImage(sm.File.url_private_download, fileName);

                                    SetText(string.Format("Downloading file {0}", fileName));

                                    gen.AddLinkContent(fileName);
                                    continue;
                                }
                            }

                            gen.AddContent(sm.Text, false);
                            

                            //Handle attachments
                            if (sm.attachments != null)
                            {
                                foreach(attachments att in sm.attachments)
                                gen.AddLinkContent(att.from_url);
                            }

                            //Handle attachments
                            if (sm.reactions != null)
                            {
                                foreach (Reaction re in sm.reactions)
                                    gen.AddReactionContent(re);
                            }
                        }
                    }

                    string fullContent = gen.GetFullContent();
                    System.IO.File.AppendAllText(path, fullContent);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace);
            }
        }

        void LoadEmoticons(string fileName)
        {
            //Create cheat sheet
            //foreach (string file in Directory.EnumerateFiles(fileName))
            //{
            //    string fn = file.Substring(file.LastIndexOf("\\") + 1).Replace(".png", "");
            //    System.IO.File.AppendAllText(@"C:\Projects\My projects\SlackReaderApp\emoticonList.txt", fn + "\r\n");
            //}

       
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "SlackReaderApp.emoticonList.txt";
            m_EmoticonDic = new Dictionary<string, string>();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                //string result = reader.ReadToEnd();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string value = string.Format("<img src='Graphics/{0}' width='16' height='16' />", line + ".png");

                    m_EmoticonDic.Add(":" + line + ":", value);
                }
            }


            //m_EmoticonDic = new Dictionary<string, string>();
            //using (var reader = new StreamReader(fileName))
            //{
            //    string line;
            //    while ((line = reader.ReadLine()) != null)
            //    {
            //        string value = string.Format("<img src='Graphics/{0}' width='16' height='16' />", line+".png");
                    
            //        m_EmoticonDic.Add(":" + line + ":", value);
            //    }
            //}

        }

        private void SetText(string text)
        {
            // InvokeRequired required compares the thread ID of the  
            // calling thread to the thread ID of the creating thread.  
            // If these threads are different, it returns true.  
            if (this.txtMessage.InvokeRequired)
            {
                StringArgReturningVoidDelegate d = new StringArgReturningVoidDelegate(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.txtMessage.AppendText(text +"\r\n" );
            }
        }

        private void DownloadImage(string url, string fileName)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(new Uri(url), fileName);
                }
            }
            catch (Exception)
            {
                SetText(string.Format("Could not download {0}", fileName));
            }
        }

        private DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }


        private void btnArchiveFolder_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
                DialogResult result = folderBrowserDialog1.ShowDialog();
                if (result == DialogResult.OK)
                {
                    m_ArchiveFolderPath = folderBrowserDialog1.SelectedPath;
                    txtArchiveFolder.Text = m_ArchiveFolderPath;
                    m_UsersFilerPath = m_ArchiveFolderPath + "\\users.json";
                }
            }
            catch (Exception ex)
            {
                SetText(ex.Message);
            }
        }

        private void btnOutputFolder_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
                DialogResult result = folderBrowserDialog1.ShowDialog();
                if (result == DialogResult.OK)
                {
                    m_OutputFolderPath = folderBrowserDialog1.SelectedPath;
                    txtOutputFolder.Text = m_OutputFolderPath;
                }
            }
            catch (Exception ex)
            {
                SetText(ex.Message);
            }
        }

       
    }
}
