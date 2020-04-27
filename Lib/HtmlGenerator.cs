using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;


namespace Lib
{
    public class HtmlGenerator
    {
        private string m_FullContent;

        private StringBuilder m_Content;

        private Dictionary<string, string> m_EmoticonDic;
        private Dictionary<string, UserName> m_UserDic;
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public HtmlGenerator(string title, Dictionary<string, string> emoDic, Dictionary<string, UserName> userDic)
        {
            AddTemplateContent(title);

            m_Content = new StringBuilder();
            m_EmoticonDic = emoDic;
            m_UserDic = userDic;
        }
        
        private void AddTemplateContent(string title)
        {
            m_FullContent = @"<!DOCTYPE html>
                              <html>
                                <head>
                                    <title></title>
                                    <style>
                                        body {
                                              font-family: calibri;
                                              font-size: 20px;
                                              width: 50%;
                                              margin: auto;
                                             }
                                    </style>
                                    </head>
                               <body></body>
                            </html > ";
            m_FullContent = m_FullContent.Replace("<title></title>", string.Format("<title>{0}</title>", title));
        }

        //To add more style
        public string AddContent(string message, bool isBold, bool checkEmoticon, bool checkTextFormatting)
        {
            string outMessage = String.Empty;
            try
            {
                //check for emoticon
                if (checkEmoticon)
                {
                    HandleEmoticons(ref message, ref outMessage);
                }

                //check for text formatting, italics,  e.g _really_  should be rendered as really in italics
                if (checkTextFormatting)
                {
                    //For italics
                    //For one word
                    string pattern = @"[.,;?\s](_)\S*(_)[.,;?\s]";
                    char splChar = '_';
                    HandleTextFormatting(ref message, splChar, pattern, "<i>", "</i>");

                    //For multiple words
                    pattern = @"[.,;?\s](_)[\S\s]*(_)[.,;?\s]";
                    HandleTextFormatting(ref message, splChar, pattern, "<i>", "</i>");

                    //For bold
                    //For one word
                    pattern = @"[.,;?\s](\*)\S*(\*)[.,;?\s]";
                    splChar = '*';
                    HandleTextFormatting(ref message, splChar, pattern, "<b>", "</b>");

                    //For multiple words
                    //TODO one case is not handled properly is when there are two or more series of multi words.
                    pattern = @"[.,;?\s](\*)[\S\s]*(\*)[.,;?\s]";
                    HandleTextFormatting(ref message, splChar, pattern, "<b>", "</b>");

                    //For underline
                    //For one word
                    pattern = @"[.,;?\s](~)\S*(~)[.,;?\s]";
                    splChar = '~';
                    HandleTextFormatting(ref message, splChar, pattern, "<u>", "</u>");

                    //For multiple words
                    pattern = @"[.,;?\s](~)[\S\s]*(~)[.,;?\s]";
                    HandleTextFormatting(ref message, splChar, pattern, "<u>", "</u>");

                    HandleAtUserText(ref message);
                }

                //If message contain link, split the message
                if (message.Contains("<http"))
                {
                    string m = HandleMessageWithLink(message);
                    if (!string.IsNullOrEmpty(m))
                        outMessage += m;
                }
                else
                {
                    if (isBold)
                    {
                        m_Content.AppendLine(string.Format("<p><b>{0}</b></p>", message));
                    }
                    else
                    {
                        m_Content.AppendLine(string.Format("<p>{0}</p>", message));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(Log, ex);
                outMessage = ex.Message;
            }
            return outMessage;
        }

        private void HandleEmoticons(ref string message, ref string outMessage)
        {
            string pattern = @"(:)\S*(:)";
            Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);

            bool hasEmoticon = rgx.IsMatch(message);
            if (hasEmoticon)
            {
                //Replace emoticon symbol with image
                MatchCollection mc = Regex.Matches(message, pattern);

                foreach (Match m in mc)
                {
                    if (m_EmoticonDic.ContainsKey(m.Value))
                    {
                        message = message.Replace(m.Value, m_EmoticonDic[m.Value]);
                    }
                    else
                    {
                        //case of multiple emoticon together like :-1::-1:
                        char[] splitchar = { ':' };
                        string[] strArr = m.Value.Split(splitchar);
                        for (int count = 0; count <= strArr.Length - 1; count++)
                        {
                            //ignore skin tone as is not supported
                            if (string.IsNullOrEmpty(strArr[count]) || strArr[count].Contains("skin-tone") || strArr[count].Contains("/"))
                                continue;

                            string emoStrg = ":" + strArr[count] + ":";
                            if (m_EmoticonDic.ContainsKey(emoStrg))
                            {
                                message = message.Replace(emoStrg, m_EmoticonDic[emoStrg]);
                            }
                            else
                            {
                                outMessage += " Emoticon not translated: " + emoStrg;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// To check if the text has formatting like bold, underline, italics
        /// e.g _really_  should be rendered as 'really' in italics. *your text* bold. ~your text~ underline
        /// </summary>
        /// <param name="message">text</param>
        /// <param name="specialChar">Character to identify which format to use</param>
        /// <param name="pattern">regex pattern to identify the special text which needs formatting</param>
        /// <param name="startSplTag">the html start tag for formatting</param>
        /// <param name="endSplTag">the html end tag</param>
        private void HandleTextFormatting(ref string message, char specialChar, string pattern,
            string startSplTag, string endSplTag)
        {
            Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);

            bool found = rgx.IsMatch(message);
            if (found)
            {
                MatchCollection mc = Regex.Matches(message, pattern);

                foreach (Match m in mc)
                {
                    if (m.Value.Length > 2)
                    { 
                        //Extract the text to be formatted
                        int startInd = m.Value.IndexOf(specialChar);
                        int endInd = m.Value.LastIndexOf(specialChar);
                        string text = m.Value.Substring(startInd, endInd - startInd + 1 );
                        string replaceText = text.TrimStart(specialChar).TrimEnd(specialChar);
                        message = message.Replace(text, startSplTag + replaceText + endSplTag);
                    }
                    else
                    {
                        Logger.LogInfo(Log, $"@{m.Value} could not be formatted correctly.");
                    }
                }
            }
        }

        //To check if the text has @UserID in it, if so replace by @UserName, 
        //like <@U0535FH0Q> should be replace by @ronald
        private void HandleAtUserText(ref string message)
        {
            string pattern = @"(<@)\S*(>)";
            Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);

            bool hasAtUser = rgx.IsMatch(message);
            if (hasAtUser)
            {
                //Replace userid with username
                MatchCollection mc = Regex.Matches(message, pattern);

                foreach (Match m in mc)
                {
                    string userID = m.Value.Replace("<@","").Replace(">","");
                    if (m_UserDic.ContainsKey(userID))
                    {
                        message = message.Replace(m.Value, "@" + m_UserDic[userID].Real_name);
                    }
                    else
                    {
                        Logger.LogInfo(Log, $"@{userID} not found to be replace the name" );
                    }
                }
            }
        }

        private string HandleMessageWithLink(string message)
        {
            string outMessage = string.Empty;
            try
            {
                StringBuilder fullMessage = new StringBuilder();
                fullMessage.Append("<p>");

                bool hasHttp = true;
                do
                {
                    int startIndex = message.IndexOf("<http");
                    int endIndex = message.IndexOf(">");

                    if (startIndex != 0)//there is a text message between link
                    {
                        string startMessage = message.Substring(0, startIndex - 1);
                        fullMessage.Append(startMessage);
                        message = message.Substring(startIndex);
                    }
                    else
                    {
                        string link = message.Substring(startIndex + 1, endIndex - 1);

                        fullMessage.Append(string.Format("<a href = '{0}' > {0} </a>", link));

                        message = message.Substring(endIndex + 1);
                        hasHttp = message.Contains("<http");

                        if (!hasHttp)
                        {
                            fullMessage.Append(message);
                        }
                    }

                } while (hasHttp);

                fullMessage.Append("</p>");
                m_Content.AppendLine(string.Format("<p>{0}</p>", fullMessage.ToString()));
            }
            catch (Exception ex)
            {
                Logger.LogError(Log, ex);
                outMessage = ex.Message;
            }
            return outMessage;
        }

        public void AddReactionContent(Reaction react)
        {
            //Replace emoticon symbol with image
            string reactionTag= react.name;
            if (m_EmoticonDic.ContainsKey(":"+ react.name +":"))
            {
                reactionTag =  m_EmoticonDic[":" + react.name + ":"];
            }
            string userName= string.Empty;
            if (react.users != null)
            {
                int userCount = 0;
                foreach (string user in react.users)
                {
                    if (m_UserDic.ContainsKey(user))
                    {
                        userName += m_UserDic[user].Real_name;
                    }
                    else
                    {
                        userName += user;
                    }

                    userCount++;
                    if (userCount < react.users.Count && react.users.Count > 1)
                    {
                        userName += ", ";
                    }
                }
            }

           m_Content.AppendLine(string.Format("<p><b>{0} {1}</b></p>", userName, reactionTag));
        }
        
        public void AddLinkContent(string message, string url)
        {
            m_Content.AppendLine(string.Format("<p><a href = '{0}' ><b> {1} </b> </a></p>", url, message));
        }

        public string GetFullContent()
        {
            string full = string.Empty;
            full = m_FullContent.Replace("<body></body>", string.Format("<body>{0}</body>", m_Content.ToString()));
            return full;
        }
    }
}
