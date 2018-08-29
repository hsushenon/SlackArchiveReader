using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SlackReaderApp
{
    public class HtmlGenerator
    {
        private string m_FullContent;

        private StringBuilder m_Content;

        private Dictionary<string, string> m_EmoticonDic;
        private Dictionary<string, UserName> m_UserDic;

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
                               </head>
                               <body></body>
                            </ html > ";
            m_FullContent = m_FullContent.Replace("<title></title>", string.Format("<title>{0}</title>", title));
        }

        //To add more style
        public void AddContent(string message, bool isBold)
        {
            //check for emoticon
            string pattern = @"(:)\S*(:)";//@"\:[a-z0-9_\+-]\+\:";
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
                }
            }

            if (isBold)
                m_Content.AppendLine(string.Format("<p><b>{0}</b></p>",message));
            else
                m_Content.AppendLine(string.Format("<p>{0}</p>", message));
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



        public void AddLinkContent(string message)
        {
            m_Content.AppendLine(string.Format("<p><a href = '{0}' > {0} </a></p>", message));
        }

        public string GetFullContent()
        {
            string full = string.Empty;
            full = m_FullContent.Replace("<body></body>", string.Format("<body>{0}</body>", m_Content.ToString()));
            return full;
        }
    }
}
