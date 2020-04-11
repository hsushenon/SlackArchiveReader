using Lib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

namespace SlackArchiveReaderConsoleApp
{
    class SlackReader
    {
        string m_OutputFolderPath = @"C:\Projects\My projects\SlackReaderApp\Output\";
        string m_ArchiveFolderPath = @"C:\Projects\My projects\SlackReaderApp\BowbazarSlackExportJun2017_Test2\";
        string m_UsersFilerPath = @"C:\Projects\My projects\SlackReaderApp\BowbazarSlackExportJun2017_Test2\users.json";
        string m_ImageFolder = string.Empty;
        string m_GraphicsFolder = string.Empty;
        string m_FilesFolder = string.Empty;
        
        Dictionary<string, UserName> m_UserDic = new Dictionary<string, UserName>();
        private Dictionary<string, string> m_EmoticonDic;
      
        // This delegate enables asynchronous calls for setting  
        // the text property on a TextBox control.  
        delegate void StringArgReturningVoidDelegate(string text);

        bool m_DownloadFiles = true;

        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void Load()
        {
            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                LoadEmoticons();
            }
            catch (Exception ex)
            {
                Logger.LogError(Log, ex);
                SetText(ex.Message);
            }
        }
        
        private void LoadEmoticons()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "SlackArchiveReaderConsoleApp.emoticonList.txt";
                m_EmoticonDic = new Dictionary<string, string>();
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string value = string.Format("<img src='Graphics/{0}' width='16' height='16' />", line + ".png");
                        m_EmoticonDic.Add(":" + line + ":", value);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(Log, ex);
                SetText(ex.Message);
            }
        }
        
        public bool Start(string archiveFolderPath, string outputFolderPath, bool downloadFiles)
        {
            bool success = false;
            try
            {
                m_DownloadFiles = downloadFiles;
                m_OutputFolderPath = outputFolderPath;
                m_ArchiveFolderPath = archiveFolderPath;
                m_ImageFolder = m_OutputFolderPath + "\\Images";
                m_GraphicsFolder = m_OutputFolderPath + "\\Graphics";
                m_FilesFolder = m_OutputFolderPath + "\\Files";
                m_UsersFilerPath = m_ArchiveFolderPath + "\\users.json";

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
                        Console.Write("Could not find the source graphics folder to copy from, please copy it manually to the output folder path");
                    }
                    else
                    {
                        SetText("Copying graphics..");
                        Common.DirectoryCopy(graphicsSourceFolderPath, m_GraphicsFolder, false);
                    }
                }

                Log.Info("Archiving started.");

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
                        
                        string contents = System.IO.File.ReadAllText(file);

                        List<SlackMessage> smList = JsonConvert.DeserializeObject<List<SlackMessage>>(contents);

                        //multiple consecutive messages from the same person in the same day should be without the time stamp
                        string lastUser = string.Empty;
                        DateTime lastMessageTime = DateTime.Now;

                        bool addFirstMessage = false;

                        foreach (SlackMessage sm in smList)
                        {
                            if (sm.Thread_ts != null && !sm.TS.Equals(sm.Thread_ts))
                            {
                                continue;//as the message is in thread and handled separately.
                            }

                            if (!addFirstMessage)
                            {
                                gen.AddContent(dateMessage, true, false, false);
                                addFirstMessage = true;
                            }

                            double time = double.Parse(sm.TS);
                            DateTime dt = Common.TimeStampToDateTime(time);

                            //string name = string.Empty;
                            UserName un;
                            if (sm.User != null && m_UserDic.ContainsKey(sm.User))
                            {
                                un = m_UserDic[sm.User];
                                if (string.IsNullOrEmpty(un.Real_name))
                                {
                                    un.Real_name = "NoRealName";
                                }
                            }
                            else if (sm.SubType.Equals("file_comment") && !string.IsNullOrEmpty(sm.Text) && sm.Text.Length > 38)
                            {
                                //special handling to get user from comment
                                //TODO change logic to use comment field
                                string user = sm.Text.Substring(2, 9);
                                if (m_UserDic.ContainsKey(user))
                                {
                                    un = m_UserDic[user];
                                    string userTag = "<@" + user + ">";
                                    string userFile = sm.Text.Substring(28, 9);
                                    sm.Text = sm.Text.Replace(userTag, un.Real_name);

                                    //Find comment on and replace user
                                    if (m_UserDic.ContainsKey(userFile))
                                    {
                                        userTag = "<@" + userFile + ">";
                                        sm.Text = sm.Text.Replace(userTag, m_UserDic[userFile].Real_name);
                                    }
                                }
                                else
                                {
                                    un = new UserName();
                                    un.Real_name = "NoName";
                                }
                            }
                            else //There is no user set here, need special handling
                            {
                                un = new UserName();
                                un.Real_name = "NoName";
                            }

                            if (!un.Real_name.Equals(lastUser) || dt.Subtract(lastMessageTime).TotalMinutes > 5)
                            {
                                gen.AddContent(un.Real_name + " " + dt.ToLongTimeString(), true, false, false);
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

                            string outMessage = gen.AddContent(sm.Text, false, true, true); //Move up as if text and file is there text is first then file

                            if (sm.files != null && m_DownloadFiles)  //.File != null) JSON format changed in may 2019
                            {
                                HandleFiles(sm, gen);
                            }

                            if (sm.replies != null)
                            {
                                foreach (Reply r in sm.replies)
                                {
                                    double timer = double.Parse(r.ts);
                                    DateTime dtr = Common.TimeStampToDateTime(timer);

                                    //If the reply is on same date
                                    if (dtr.Day == dt.Day && dtr.Month == dt.Month && dtr.Year == dt.Year)
                                    {
                                        HandleReplyThread(r, smList, gen, dateMessage);
                                    }
                                    else
                                    {
                                        //TODO special handling as the date of the correct reply thread is not resolve satisfactorily
                                        //If not found we check one day before and after also
                                        bool found = false;
                                        //For day which is on 9th or earlier, it is 09
                                        string mm = dtr.Month.ToString();
                                        string dd = dtr.Day.ToString();
                                        if (dtr.Month <= 9)
                                            mm = "0" + mm;
                                        if (dtr.Day <= 9)
                                            dd = "0" + dd;

                                        string fileR = string.Format("{0}\\{1}-{2}-{3}.json", folder, dtr.Year, mm, dd);

                                        if (System.IO.File.Exists(fileR))
                                        {
                                            string contentsr = System.IO.File.ReadAllText(fileR);

                                            List<SlackMessage> smListr = JsonConvert.DeserializeObject<List<SlackMessage>>(contentsr);
                                            found = HandleReplyThread(r, smListr, gen, dateMessage);
                                        }

                                        if (!found)
                                        {
                                            DateTime replyDate = dtr.AddDays(-1);
                                            //For day which is on 9th or earlier, it is 09
                                            mm = replyDate.Month.ToString();
                                            dd = replyDate.Day.ToString();
                                            if (replyDate.Month <= 9)
                                                mm = "0" + mm;
                                            if (replyDate.Day <= 9)
                                                dd = "0" + dd;

                                            fileR = string.Format("{0}\\{1}-{2}-{3}.json", folder, replyDate.Year, mm, dd);

                                            if (System.IO.File.Exists(fileR))
                                            {
                                                string contentsr = System.IO.File.ReadAllText(fileR);

                                                List<SlackMessage> smListr = JsonConvert.DeserializeObject<List<SlackMessage>>(contentsr);
                                                found = HandleReplyThread(r, smListr, gen, dateMessage);
                                            }

                                            if (!found)
                                            {
                                                replyDate = dtr.AddDays(1);
                                                //For day which is on 9th or earlier, it is 09
                                                mm = replyDate.Month.ToString();
                                                dd = replyDate.Day.ToString();
                                                if (replyDate.Month <= 9)
                                                    mm = "0" + mm;
                                                if (replyDate.Day <= 9)
                                                    dd = "0" + dd;

                                                fileR = string.Format("{0}\\{1}-{2}-{3}.json", folder, replyDate.Year, mm, dd);

                                                if (System.IO.File.Exists(fileR))
                                                {
                                                    string contentsr = System.IO.File.ReadAllText(fileR);

                                                    List<SlackMessage> smListr = JsonConvert.DeserializeObject<List<SlackMessage>>(contentsr);
                                                    found = HandleReplyThread(r, smListr, gen, dateMessage);

                                                    if (!found)
                                                    {
                                                        string message = "Reply not found. Date: " + dateMessage + "Channel:" + channelName;
                                                        Log.Info(message);
                                                        SetText(message);
                                                    }
                                                }
                                            }
                                        }
                                        
                                    }
                                }
                                //Set last user to blank as need to set user name again after going through thread
                                lastUser = string.Empty;
                            }


                            if (!string.IsNullOrEmpty(outMessage))
                            {
                                SetText(outMessage + "Date: " + dateMessage);
                            }

                            //Handle attachments
                            if (sm.attachments != null)
                            {
                                HandleAttachments(sm, gen);
                            }

                            //Handle reactions
                            if (sm.reactions != null)
                            {
                                HandleReactions(sm, gen);
                            }
                        }
                    }

                    string fullContent = gen.GetFullContent();
                    System.IO.File.AppendAllText(path, fullContent);
                }
                success = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(Log, ex);
                SetText(ex.Message);
            }
            return success;
        }

        private bool HandleReplyThread(Reply r, List<SlackMessage> smList, HtmlGenerator gen, string dateMessage)
        {
            bool found = false;
            try
            {
                List<SlackMessage> smList2 = smList.Where(x => x.Thread_ts != null && r.ts.Equals(x.TS)).ToList();

                if (smList2 != null)
                {
                    found = true;
                    foreach (SlackMessage sm in smList2)
                    {
                        double time = double.Parse(sm.TS);
                        DateTime dt = Common.TimeStampToDateTime(time);

                        UserName un;
                        if (sm.User != null && m_UserDic.ContainsKey(sm.User))
                        {
                            un = m_UserDic[sm.User];
                        }
                        else
                        {
                            if (sm.SubType.Equals("file_comment") && !string.IsNullOrEmpty(sm.Text) && sm.Text.Length > 38)
                            {
                                //special handling to get user from comment
                                //TODO change logic
                                string user = sm.Text.Substring(2, 9);
                                if (m_UserDic.ContainsKey(user))
                                {
                                    un = m_UserDic[user];
                                    string userTag = "<@" + user + ">";
                                    string userFile = sm.Text.Substring(28, 9);
                                    sm.Text = sm.Text.Replace(userTag, un.Real_name);

                                    //Find comment on and replace user
                                    //string userFile = sm.Text.Substring(28, 9);
                                    if (m_UserDic.ContainsKey(userFile))
                                    {
                                        userTag = "<@" + userFile + ">";
                                        sm.Text = sm.Text.Replace(userTag, m_UserDic[userFile].Real_name);
                                    }
                                }
                                else
                                {
                                    un = new UserName();
                                    un.Real_name = "NoName";
                                }
                            }
                            else
                            {
                                un = new UserName();
                                un.Real_name = "NoName";
                            }
                        }

                        gen.AddContent("--Replies--" + un.Real_name + " " + dt.ToShortDateString() + " " + dt.ToLongTimeString(), true, false, false);

                        //TODO think of better way to show replies
                        string outMessage = gen.AddContent("--         " + sm.Text, false, true, true);

                        if (sm.files != null && m_DownloadFiles)  //.File != null) JSON format changed in may 2019
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
                Logger.LogError(Log, ex);
                SetText(ex.Message);
            }
            return found;
        }

        private void HandleFiles(SlackMessage sm, HtmlGenerator gen)
        {
            try
            {
                foreach (Lib.File f in sm.files)
                {
                    //change name of file as some has same name
                    string name = f.id + "_" + f.name;
                 
                    if (f.mimetype.Contains("image"))// for test&& channelName.Equals("photos"))
                    {
                        //Download image
                        string fileName = m_ImageFolder + "\\" + name;

                        DownloadFile(f.url_private_download, fileName);
                        SetText(string.Format("Downloading image {0}", fileName));

                        string imageTag = string.Format("<img src='Images/{0}' width='{1}' height='{2}'>", name, f.thumb_360_w, f.thumb_360_h);
                        gen.AddContent(f.title, false, false, false);
                        gen.AddContent(imageTag, false, false, false);
                        continue;
                    }
                    else
                    {
                        //file_share
                        //Download other types
                        string fileName = m_FilesFolder + "\\" + name;

                        DownloadFile(f.url_private_download, fileName);
                        SetText(string.Format("Downloading file {0}", fileName));

                        gen.AddLinkContent(fileName, fileName);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(Log, ex);
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
                Logger.LogError(Log, ex);
                SetText(ex.Message);
            }
        }

        private void HandleAttachments(SlackMessage sm, HtmlGenerator gen)
        {
            try
            {
                foreach (attachments att in sm.attachments)
                {
                    gen.AddLinkContent(att.title, att.from_url);

                    if (!string.IsNullOrEmpty(att.text))
                    {
                        gen.AddContent("--  " + att.text, false, false, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(Log, ex);
                SetText(ex.Message);
            }
        }
        
        private void DownloadFile(string url, string fileName)
        {
            try
            {
                if (!System.IO.File.Exists(fileName) && !string.IsNullOrEmpty(url))
                {
                    using (WebClient client = new WebClient())
                    {
                        client.DownloadFile(new Uri(url), fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(Log, ex);
                SetText(string.Format("Could not download {0}", fileName));
            }
        }
        
        private void SetText(string text)
        {
            // InvokeRequired required compares the thread ID of the  
            // calling thread to the thread ID of the creating thread.  
            // If these threads are different, it returns true.  
            //if (this.txtMessage.InvokeRequired)
            {
                //StringArgReturningVoidDelegate d = new StringArgReturningVoidDelegate(SetText);
                //this.Invoke(d, new object[] { text });
            }
            //else
            {
                Console.WriteLine(text);
                //this.txtMessage.AppendText(text + "\r\n");
            }
        }
    }
}
