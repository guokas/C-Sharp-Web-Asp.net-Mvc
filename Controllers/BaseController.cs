using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace ClientCheckWeb.Controllers
{
    public class BaseController : Controller
    {
        private Dictionary<String,String> m_para = null;

        protected Dictionary<String, String> Para
        {
            get
            {
                if (m_para != null)
                {
                    return m_para;
                }
                m_para = new Dictionary<string, string>();
                //parsing url
                String uri = Request.Url.AbsoluteUri;
                if (uri == null)
                {
                    return null;
                }
                int quotIndex = uri.IndexOf("?");
                //GET
                if (quotIndex >= 0)
                {
                    String m_uri = String.Empty;
                    try
                    {
                        m_uri = uri.Substring(quotIndex + 1);
                    }
                    catch (ArgumentOutOfRangeException)
                    {//if extract failed,then restore
                        quotIndex = -1;
                        m_uri = uri;
                    }
                    if (quotIndex != -1)
                    {
                        String[] uri_param = m_uri.Split(new Char[] { '&' });
                        foreach (String uri_p in uri_param)
                        {
                            if (uri_p.IndexOf("=") >= 0)
                            {
                                String[] e_param = uri_p.Split(new Char[] { '=' });
                                if (String.IsNullOrEmpty(e_param[0].Trim()))
                                {
                                    continue;
                                }
                                else
                                {
                                    //escape processing
                                    m_para.Add(HttpUtility.UrlDecode(e_param[0]), HttpUtility.UrlDecode(e_param[1]));
                                }
                            }
                        }
                    }
                }
                //POST
                foreach (String key in Request.Form.Keys)
                {
                    m_para.Add(key, Request.Form[key]);
                }
                //STREAM
                if (m_para.Keys.Count == 0)
                {
                    System.IO.Stream streamReceive = this.Request.InputStream;
                    Encoding encoding = Encoding.UTF8;
                    System.IO.StreamReader streamReader = new System.IO.StreamReader(streamReceive, encoding);
                    string textRead = streamReader.ReadToEnd();
                    //1 textRead include xml，parsing xml
                    //2 textRead include Json，parsing Json
                }
                return m_para;
            }
        }
        internal String GetPara(String key,String defaultValue = "")
        {
            Para.TryGetValue(key, out defaultValue);
            return defaultValue ;
        }
    }
}
