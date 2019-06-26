using System;
using System.Net.Sockets;//用于处理网络连接 
using System.IO; //用于处理附件的包 
using System.Text;//用于处理文本编码 
using System.Data;
using System.Net;

namespace Shove.Net.Mail
{
    public class SMTP : TcpClient
    {
        /*SMTP服务器域名*/
        private string strServer;
        public string server
        {
            get
            {
                return strServer;
            }
            set
            {
                if (strServer != value)
                {
                    strServer = value;
                }
            }
        }

        /*SMTP服务器端口*/
        private int strPort;
        public int port
        {
            get
            {
                return strPort;
            }
            set
            {
                if (strPort != value)
                {
                    strPort = value;
                }
            }
        }

        /*用户名*/
        private string strUse = null;
        public string username
        {
            get
            {
                return strUse;
            }
            set
            {
                if (strUse != value)
                {
                    strUse = value;
                }
            }
        }

        /*密码*/
        private string strPass = null;
        public string password
        {
            get
            {
                return strPass;
            }
            set
            {
                if (strPass != value)
                {
                    strPass = value;
                }
            }
        }

        /*主题*/
        private string strSub = "";
        public string subject
        {
            get
            {
                return strSub;
            }
            set
            {
                if (strSub != value)
                {
                    strSub = value;
                }
            }
        }

        /*文本内容*/
        private string strBody = "";
        public string body
        {
            get
            {
                return strBody;
            }
            set
            {
                if (strBody != value)
                {
                    strBody = value;
                }
            }
        }

        /*超文本内容*/
        private string strHtm = "";
        public string htmlbody
        {
            get
            {
                return strHtm;
            }
            set
            {
                if (strHtm != value)
                {
                    strHtm = value;
                }
            }
        }

        /*发件人地址*/
        private string strFrom = "";
        public string from
        {
            get
            {
                return strFrom;
            }
            set
            {
                if (strFrom != value)
                {
                    strFrom = value;
                }
            }
        }

        /*收件人地址*/
        private string strTo = "";
        public string to
        {
            get
            {
                return strTo;
            }
            set
            {
                if (strTo != value)
                {
                    strTo = value;
                }
            }
        }

        /*发件人姓名*/
        private string strFName = "";
        public string fromname
        {
            get
            {
                return strFName;
            }
            set
            {
                if (strFName != value)
                {
                    strFName = value;
                }
            }
        }

        /*收件人姓名*/
        private string strTName = "";
        public string toname
        {
            get
            {
                return strTName;
            }
            set
            {
                if (strTName != value)
                {
                    strTName = value;
                }
            }
        }

        /*邮件编码*/
        private string strEncode = "8bit";
        public string encode
        {
            get
            {
                return strEncode;
            }
            set
            {
                if (strEncode != value)
                {
                    strEncode = value;
                }
            }
        }

        /*邮件优先级*/
        private int intPriority = 0;
        public int priority
        {
            get
            {
                return intPriority;
            }
            set
            {
                if (intPriority != value)
                {
                    intPriority = value;
                }
            }
        }

        /*语言编码*/
        private string strCharset = "gb2312";
        public string charset
        {
            get
            {
                return strCharset;
            }
            set
            {
                if (strCharset != value)
                {
                    strCharset = value;
                }
            }
        }


        /*html邮件标志*/
        private bool isHtml = false;
        public bool html
        {
            get
            {
                return isHtml;
            }
            set
            {
                if (isHtml != value)
                {
                    isHtml = value;
                }
            }
        }
        private DataTable filelist;//附件列表　 

        public SMTP()
        {
        }

        /*发送邮件*/
        public void Send()
        {
            filelist = new DataTable();//已定义变量，初始化操作 
            filelist.Columns.Add(new DataColumn("filename", typeof(string)));//文件名 
            filelist.Columns.Add(new DataColumn("filecontent", typeof(string)));//文件内容 

            /*准备发送*/
            WriteStream("mail From: " + strFrom);
            WriteStream("rcpt to: " + strTo);
            WriteStream("data");

            /*发送邮件头*/
            WriteStream("Date: " + DateTime.Now);//时间 
            WriteStream("From: " + strFName + "<" + strFrom + ">");//发件人 
            WriteStream("Subject: " + strSub);//主题 
            WriteStream("To:" + strTName + "<" + strTo + ">");//收件人 

            //邮件格式 
            WriteStream("Content-Type: multipart/mixed; boundary=\"unique-boundary-1\"");
            WriteStream("Reply-To:" + strFrom);//回复地址 
            WriteStream("X-Priority:" + intPriority);//优先级 
            WriteStream("MIME-Version:1.0");//MIME版本 

            //数据ID,随意 
            WriteStream("Message-Id: " + DateTime.Now.ToFileTime() + "@security.com");
            WriteStream("Content-Transfer-Encoding:" + strEncode);//内容编码 
            WriteStream("X-Mailer:DS Mail Sender V1.0");//邮件发送者 
            WriteStream("");

            WriteStream(AuthStream("This is a multi-part message in MIME format."));
            WriteStream("");

            //从此处开始进行分隔输入 
            WriteStream("--unique-boundary-1");

            //在此处定义第二个分隔符 
            WriteStream("Content-Type: multipart/alternative;Boundary=\"unique-boundary-2\"");
            WriteStream("");

            if (!isHtml)
            {
                //文本信息
                WriteStream("--unique-boundary-2");
                WriteStream("Content-Type: text/plain;charset=" + strCharset);
                WriteStream("Content-Transfer-Encoding:" + strEncode);
                WriteStream("");
                WriteStream(strBody);
                WriteStream("");//一个部分写完之后就写如空信息，分段 
                WriteStream("--unique-boundary-2--");//分隔符的结束符号，尾巴后面多了-- 
                WriteStream("");
            }

            else
            {
                //html信息 
                WriteStream("--unique-boundary-2");
                WriteStream("Content-Type: text/html;charset=" + strCharset);
                WriteStream("Content-Transfer-Encoding:" + strEncode);
                WriteStream("");
                WriteStream(strHtm);
                WriteStream("");
                WriteStream("--unique-boundary-2--");//分隔符的结束符号，尾巴后面多了-- 
                WriteStream("");
            }

            //增加附件 
            Attachment();//这个方法是我们在上面讲过的，实际上他放在这 
            WriteStream("");
            WriteStream("--unique-boundary-1--");
            if (!OperaStream(".", "250"))//最后写完了，输入"." 
            {
                this.Close(); //关闭连接 
            }
        }

        /*向服务器写入命令的方法*/
        private void WriteStream(string strCmd)
        {
            Stream TcpStream;//定义操作对象 
            strCmd = strCmd + "\r\n"; //加入换行符 
            TcpStream = this.GetStream();//获取数据流 

            //将命令行转化为byte[] 
            byte[] bWrite = Encoding.GetEncoding(strCharset).GetBytes(strCmd.ToCharArray());

            //由于每次写入的数据大小是有限制的，那么我们将每次写入的数据长度定在７５个字节，一旦命令长度超过了７５，就分步写入。 
            int start = 0;
            int length = bWrite.Length;
            int page = 0;
            int size = 75;
            int count = size;
            try
            {
                if (length > 75)
                {

                    //数据分页 
                    if ((length / size) * size < length)
                        page = length / size + 1;
                    else
                        page = length / size;
                    for (int i = 0; i < page; i++)
                    {
                        start = i * size;
                        if (i == page - 1)
                            count = length - (i * size);
                        TcpStream.Write(bWrite, start, count);//将数据写入到服务器上 
                    }
                }
                else
                    TcpStream.Write(bWrite, 0, bWrite.Length);
            }
            catch (Exception)
            {
            }
        }

        /*接收服务器的返回信息*/
        private string ReceiveStream()
        {
            string sp = null;
            byte[] by = new byte[1024];
            NetworkStream ns = this.GetStream();//此处即可获取服务器的返回数据流 
            int size = ns.Read(by, 0, by.Length);//读取数据流 
            if (size > 0)
            {
                sp = Encoding.Default.GetString(by);//转化为String 
            }
            return sp;
        }

        /*发出命令并判断返回信息是否正确*/
        private bool OperaStream(string strCmd, string state)
        {
            string sp = null;
            bool success = false;
            try
            {
                WriteStream(strCmd);//写入命令 
                sp = ReceiveStream();//接受返回信息 
                if (sp.IndexOf(state, StringComparison.Ordinal) != -1)//判断状态码是否正确 
                    success = true;
            }
            catch (Exception ex)
            { Console.Write(ex.ToString()); }
            return success;
        }

        /*取得服务器的连接*/
        public bool getMailServer()
        {
            try
            {
                //域名解析 
                System.Net.IPAddress ipaddress = (System.Net.IPAddress)Dns.GetHostEntry(strServer).AddressList.GetValue(0);
                System.Net.IPEndPoint endpoint = new IPEndPoint(ipaddress, strPort);
                Connect(endpoint);//连接Smtp服务器 
                ReceiveStream();//获取连接信息 
                if (strUse != null)
                {
                    //开始进行服务器认证 
                    //如果状态码是250则表示操作成功 
                    if (!OperaStream("EHLO Localhost", "250"))
                    {
                        this.Close();
                        return false;
                    }

                    if (!OperaStream("AUTH LOGIN", "334"))
                    {
                        this.Close();
                        return false;
                    }
                    strUse = AuthStream(strUse);//此处将username转换为Base64码 
                    if (!OperaStream(strUse, "334"))
                    {
                        this.Close();
                        return false;
                    }
                    strPass = AuthStream(strPass);//此处将password转换为Base64码 
                    if (!OperaStream(strPass, "235"))
                    {
                        this.Close();
                        return false;
                    }
                    return true;
                }
                else
                {
                    //如果服务器不需要认证 
                    if (OperaStream("HELO Localhost", "250"))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.Write(ex.Message);
                return false;
            }
        }

        /*将数据转化为Base64编码字符串*/
        private string AuthStream(string strCmd)
        {
            try
            {
                byte[] by = Encoding.Default.GetBytes(strCmd.ToCharArray());
                strCmd = System.Convert.ToBase64String(by);
            }
            catch (Exception ex)
            { return ex.ToString(); }
            return strCmd;
        }

        /*载入附件文件*/
        public void LoadAttFile(string path)
        {
            //根据路径读出文件流 
            FileStream fstr = new FileStream(path, FileMode.Open);//建立文件流对象 
            byte[] by = new byte[System.Convert.ToInt32(fstr.Length)];
            fstr.Read(by, 0, by.Length);//读取文件内容 
            fstr.Close();//关闭 

            //格式转换 
            string fileinfo = System.Convert.ToBase64String(by);//转化为base64编码 

            //增加到文件表中 
            DataRow dr = filelist.NewRow();
            dr[0] = Path.GetFileName(path);//获取文件名 
            dr[1] = fileinfo;//文件内容 
            filelist.Rows.Add(dr);//增加 
        }

        /*发送附件*/
        private void Attachment()
        {
            //对文件列表做循环 
            for (int i = 0; i < filelist.Rows.Count; i++)
            {
                DataRow dr = filelist.Rows[i];
                WriteStream("--unique-boundary-1");//邮件内容分隔符 
                WriteStream("Content-Type: application/octet-stream;name=\"" + dr[0].ToString() + "\"");//文件格式 
                WriteStream("Content-Transfer-Encoding: base64");//内容的编码 
                WriteStream("Content-Disposition:attachment;filename=\"" + dr[0].ToString() + "\"");//文件名 
                WriteStream("");
                string fileinfo = dr[1].ToString();
                WriteStream(fileinfo);//写入文件的内容 
                WriteStream("");
            }
        }

        /*退出服务器连接*/
        public bool quitMailServer()
        {
            if (!OperaStream("quit", "250"))
            {
                return false;
            }
            return true;
        }
    }
}