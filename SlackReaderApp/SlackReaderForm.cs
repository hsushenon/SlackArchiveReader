﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;

namespace SlackReaderApp
{
    public partial class SlackReaderForm : Form
    {
        string m_OutputFolderPath = @"C:\Projects\My projects\SlackReaderApp\Output\";
        string m_ArchiveFolderPath = @"C:\Projects\My projects\SlackReaderApp\BowbazarSlackExportJun2017_Test2\";
        string m_UsersFilerPath = @"C:\Projects\My projects\SlackReaderApp\BowbazarSlackExportJun2017_Test2\users.json";
        string m_ImageFolder = string.Empty;
        string m_GraphicsFolder = string.Empty;
        string m_FilesFolder = string.Empty;
        int m_FileIndex = 1;

        Dictionary<string, UserName> m_UserDic = new Dictionary<string, UserName>();
        private Dictionary<string, string> m_EmoticonDic;
        string m_EmoticonList = @"C:\Projects\My projects\SlackReaderApp\emoticonList.txt";
        // This delegate enables asynchronous calls for setting  
        // the text property on a TextBox control.  
        delegate void StringArgReturningVoidDelegate(string text);

        const bool IS_DEBUG = false;//TODO set to false when deploy, change for when debug testing
        
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
                m_FileIndex = 1;
                //Create folder for image to download
                m_ImageFolder = m_OutputFolderPath + "\\Images";
                m_GraphicsFolder = m_OutputFolderPath + "\\Graphics";
                m_FilesFolder = m_OutputFolderPath + "\\Files";

                //if (!IS_DEBUG)
                {
                    if (!Directory.Exists(m_ImageFolder))
                    {
                        Directory.CreateDirectory(m_ImageFolder);
                    }
                    if (!Directory.Exists(m_FilesFolder))
                    {
                        Directory.CreateDirectory(m_FilesFolder);
                    }
                    if (!Directory.Exists(m_GraphicsFolder))
                    {
                        //Check if the Graphics folder exist then copy it to output folder
                        string graphicsSourceFolderPath = ".\\Graphics";
                        if (!Directory.Exists(graphicsSourceFolderPath))
                        {
                            MessageBox.Show("Could not find the graphics folder, please copy it to the output folder path");
                            return;
                        }
                        else
                        {
                            Common.DirectoryCopy(graphicsSourceFolderPath, m_GraphicsFolder, false);
                        }
                    }
                }

                //read users
                string users = System.IO.File.ReadAllText(m_UsersFilerPath);
                List<User> userList = JsonConvert.DeserializeObject<List<User>>(users);
                foreach (User u in userList)
                {
                    UserName un = new UserName();
                    un.Name = u.Name;
                    un.Real_name = u.Real_name;
                    if (!m_UserDic.ContainsKey(u.ID))
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
                       
                        gen.AddContent(dateMessage, true, false);

                        string contents = System.IO.File.ReadAllText(file);

                        List<SlackMessage> smList = JsonConvert.DeserializeObject<List<SlackMessage>>(contents);

                        //multiple consecutive messages from the same person in the same day should be without the time stamp
                        string lastUser = string.Empty;
                        DateTime lastMessageTime = DateTime.Now;

                        foreach (SlackMessage sm in smList)
                        {
                            if (sm.Thread_ts != null && !sm.TS.Equals(sm.Thread_ts))
                            {
                                continue;//as the message is in thread and handled separately.
                            }

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
                                un.Real_name = "NoName";
                                //if (sm.SubType != null && sm.SubType.Equals("file_comment"))
                                //{
                                //    //TODO 
                                //    continue;
                                //}
                            }

                            if (!un.Real_name.Equals(lastUser)  || dt.Subtract(lastMessageTime).TotalMinutes > 5)
                            {
                                gen.AddContent(un.Real_name + " " + dt.ToLongTimeString(), true, false);
                                lastUser = un.Real_name;
                            }
                          
                            lastMessageTime = dt;

                            //special handling to replace userID with username when join 
                            if (sm.SubType != null)
                            {   //TODO create enums for subtype
                                if (sm.SubType.Equals("channel_join") || sm.SubType.Equals("channel_purpose"))
                                {
                                    //Channel join message is like  "<@U0535FH0Q|hsushenon> has joined the channel"
                                    string currentText = string.Format("<@{0}|{1}>", sm.User, un.Name);
                                    sm.Text = sm.Text.Replace(currentText, un.Real_name);
                                }
                              
                            }

                            string outMessage = gen.AddContent(sm.Text, false, true); //Move up as if text and file is there text is first then file

                            if (sm.replies != null)
                            {
                                foreach (Reply r in sm.replies)
                                {
                                    HandleReplyThread(r, smList, gen, dateMessage);
                                }
                                //Set last user to blank as need to set user name again after going through thread
                                lastUser = string.Empty;
                            }

                            if (sm.files != null)  //.File != null) JSON format changed in may 2019
                            {
                                HandleFiles(sm, gen);
                                //TODO cleanup
                                //foreach (File f in sm.files)
                                //{
                                //    //change name of file as some has same name
                                //    string name = f.name;
                                //    string indexString = "_" + m_FileIndex++ + ".";
                                //    name = name.Replace(".", indexString);

                                //    if (f.mimetype.Contains("image"))// for test&& channelName.Equals("photos"))
                                //    {
                                //        //Download image

                                //        string fileName = m_ImageFolder + "\\" + name;
                                //        DownloadImage(f.url_private_download, fileName);

                                //        SetText(string.Format("Downloading image {0}", fileName));


                                //        string imageTag = string.Format("<img src='Images/{0}' width='{1}' height='{2}'>", name, f.thumb_360_w, f.thumb_360_h);
                                //        gen.AddContent(f.title, false, false);
                                //        gen.AddContent(imageTag, false, false);
                                //        continue;
                                //    }
                                //    else
                                //    {
                                //        //file_share
                                //        //Download other types
                                //        string fileName = m_FilesFolder + "\\" + name;
                                //        DownloadImage(f.url_private_download, fileName);

                                //        SetText(string.Format("Downloading file {0}", fileName));

                                //        gen.AddLinkContent(fileName);
                                //        continue;
                                //    }
                                //}
                            }

                            //string outMessage = gen.AddContent(sm.Text, false, true);

                            if (!string.IsNullOrEmpty(outMessage))
                            {
                                SetText(outMessage + "Date: " + dateMessage);
                            }

                            //Handle attachments
                            if (sm.attachments != null)
                            {
                                HandleAttachments(sm, gen);
                                //foreach (attachments att in sm.attachments)
                                //{
                                //    gen.AddLinkContent(att.from_url);

                                //    if (!string.IsNullOrEmpty(att.fallback))
                                //    {
                                //        gen.AddContent("--  "+att.fallback, false, true);
                                //    }
                                //}
                            }

                            //Handle reactions
                            if (sm.reactions != null)
                            {
                                HandleReactions(sm, gen);
                                //foreach (Reaction re in sm.reactions)
                                //    gen.AddReactionContent(re);
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

        private void HandleReplyThread(Reply r, List<SlackMessage> smList, HtmlGenerator gen, string dateMessage)
        {
            try
            {
                List<SlackMessage> smList2 = smList.Where(x => x.Thread_ts != null && r.ts.Equals(x.TS)).ToList();

                if (smList2 != null)
                {
                    foreach (SlackMessage sm in smList2)
                    {
                        double time = double.Parse(sm.TS);
                        DateTime dt = UnixTimeStampToDateTime(time);

                        //string name = string.Empty;
                        UserName un;
                        if (sm.User != null && m_UserDic.ContainsKey(sm.User))
                        {
                            un = m_UserDic[sm.User];
                        }
                        else
                        {
                            un = new UserName();
                            un.Real_name = "NoName";
                        }

                        gen.AddContent("--Replies--" + un.Real_name + " " + dt.ToLongTimeString(), true, false);

                        //TODO think of better way to show replies
                        string outMessage = gen.AddContent("--         " + sm.Text, false, true);

                        if (sm.files != null)  //.File != null) JSON format changed in may 2019
                        {
                            HandleFiles(sm, gen);
                        }


                        if (!string.IsNullOrEmpty(outMessage))
                        {
                            SetText(outMessage + "Date: " + dateMessage);
                        }

                        if (sm.attachments != null)
                        {
                            HandleAttachments(sm, gen);
                        }

                        if (sm.reactions != null)
                        {
                            HandleReactions(sm, gen);
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                SetText(ex.Message);
            }
        }

        private void HandleFiles(SlackMessage sm, HtmlGenerator gen)
        {
            try
            {
                foreach (File f in sm.files)
                {
                    //change name of file as some has same name
                    string name = f.name;
                    string indexString = "_" + m_FileIndex++ + ".";
                    name = name.Replace(".", indexString);

                    if (f.mimetype.Contains("image"))// for test&& channelName.Equals("photos"))
                    {
                        //Download image

                        string fileName = m_ImageFolder + "\\" + name;
                        DownloadImage(f.url_private_download, fileName);

                        SetText(string.Format("Downloading image {0}", fileName));


                        string imageTag = string.Format("<img src='Images/{0}' width='{1}' height='{2}'>", name, f.thumb_360_w, f.thumb_360_h);
                        gen.AddContent(f.title, false, false);
                        gen.AddContent(imageTag, false, false);
                        continue;
                    }
                    else
                    {
                        //file_share
                        //Download other types
                        string fileName = m_FilesFolder + "\\" + name;
                        DownloadImage(f.url_private_download, fileName);

                        SetText(string.Format("Downloading file {0}", fileName));

                        gen.AddLinkContent(fileName);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                SetText(ex.Message);
            }
        }

        private void HandleReactions(SlackMessage sm, HtmlGenerator gen)
        {
            try
            {
                foreach (Reaction re in sm.reactions)
                    gen.AddReactionContent(re);
            }
            catch (Exception ex)
            {
                SetText(ex.Message);
            }
        }

        private void HandleAttachments(SlackMessage sm, HtmlGenerator gen)
        {
            try
            {
                foreach (attachments att in sm.attachments)
                {
                    gen.AddLinkContent(att.from_url);

                    if (!string.IsNullOrEmpty(att.fallback))
                    {
                        gen.AddContent("--  " + att.fallback, false, true);
                    }
                }
            }
            catch (Exception ex)
            {
                SetText(ex.Message);
            }
        }

        private void LoadEmoticons(string fileName)
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
