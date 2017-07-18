using System.Text;

namespace SlackReaderApp
{
    public class HtmlGenerator
    {
        private string m_FullContent;

        private StringBuilder m_Content;

        public HtmlGenerator(string title)
        {
            AddTemplateContent(title);

            m_Content = new StringBuilder();
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
            if (isBold)
                m_Content.AppendLine(string.Format("<p><b>{0}</b></p>",message));
            else
                m_Content.AppendLine(string.Format("<p>{0}</p>", message));
        }

        public string GetFullContent()
        {
            string full = string.Empty;
            full = m_FullContent.Replace("<body></body>", string.Format("<body>{0}</body>", m_Content.ToString()));
            return full;
        }
    }
}
