using System;
using System.Net.Sockets;//用于处理网络连接
using System.Text;//用于处理文本编码 
using System.Data;
using System.Net;
using System.IO;

namespace Shove.Net.Mail
{
    public class POP3 : TcpClient
    {
        /*POP3服务器域名*/
        private string strServer = "";
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

        /*POP3服务器端口*/
        private int intPort = 110;
        public int port
        {
            get
            {
                return intPort;
            }
            set
            {
                if (intPort != value)
                {
                    intPort = value;
                }
            }
        }

        /*用户名*/
        private string strUser = "";
        public string username
        {
            get
            {
                return strUser;
            }
            set
            {
                if (strUser != value)
                {
                    strUser = value;
                }
            }
        }

        /*密码*/
        private string strPass = "";
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

        /*邮件内容*/
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

        /*邮件编号*/
        private string strMId = "";
        public string mailId
        {
            get
            {
                return strMId;
            }
            set
            {
                if (strMId != value)
                {
                    strMId = value;
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

        /*回复地址*/
        private string strReply = "";
        public string reply
        {
            get
            {
                return strReply;
            }
            set
            {
                if (strReply != value)
                {
                    strReply = value;
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

        /*邮件日期*/
        private string strDate = "";
        public string mDate
        {
            get
            {
                return strDate;
            }
            set
            {
                if (strDate != value)
                {
                    strDate = value;
                }
            }
        }

        /*邮件类型*/
        private string strType = "";
        public string content_type
        {
            get
            {
                return strType;
            }
            set
            {
                if (strType != value)
                {
                    strType = value;
                }
            }
        }

        /*邮件编码*/
        private string strEncode = "";
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

        /*邮件的附件数量*/
        private int attachmentCount = 0;
        public int ACount
        {
            get
            {
                return attachmentCount;
            }
            set
            {
                if (attachmentCount != value)
                {
                    attachmentCount = value;
                }
            }
        }

        public DataTable filelist;//附件列表

        public POP3()
        {

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
                sp = Encoding.Default.GetString(by);//转化为string 
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
                System.Net.IPEndPoint endpoint = new IPEndPoint(ipaddress, intPort);
                Connect(endpoint);//连接Smtp服务器 
                string strRet = ReceiveStream();//获取连接信息 

                if (strRet.Substring(0, 3) == "+OK")
                {
                    //如果状态码是+OK则表示操作成功 
                    if (!OperaStream("user " + strUser, "+OK"))
                    {
                        this.Close();
                        return false;
                    }

                    if (!OperaStream("pass " + strPass, "+OK"))
                    {
                        this.Close();
                        return false;
                    }
                    return true;
                }
                else
                {
                    System.Console.Write("Cann't conect the mail server.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Console.Write(ex.Message);
                return false;
            }
        }

        /*获取邮件总数*/
        public int getMailCount()
        {
            WriteStream("stat");
            string strRet = ReceiveStream();
            if (!(strRet.Split(" ".ToCharArray())[0] == "+OK"))
            {
                return -1;
            }
            else
            {
                return System.Convert.ToInt16(strRet.Split(" ".ToCharArray())[1]);

            }
        }

        /*接收指定编号的邮件*/
        public bool getMail(int mIndex)
        {
            WriteStream("RETR " + System.Convert.ToString(mIndex));
            StreamReader sr;
            string strRet = "";
            string oldHead = "";
            string boundary = "";
            bool isBody = false;
            bool isAttachment = false;
            bool isBodyEnd = false;
            string strAm = "";
            string strAmName = "";
            string[] arrTmp;
            DataRow dr;
            string mailEncode = "8bit";
            filelist = new DataTable();
            filelist.Columns.Add(new DataColumn("filename", typeof(string)));//文件名 
            filelist.Columns.Add(new DataColumn("filecontent", typeof(string)));//文件内容 
            sr = new StreamReader(this.GetStream(), Encoding.Default);
            if (!(sr.ReadLine().Split(" ".ToCharArray())[0] == "+OK"))
            {
                return false;
            }
            while (sr.Peek() != 46)
            {
                strRet = sr.ReadLine().Trim();
                arrTmp = null;
                if (strRet == "")
                {
                    oldHead = "";
                    if (!isBody)
                    {
                        arrTmp = strType.Split(";".ToCharArray());
                        for (int i = 0; i < arrTmp.Length; i++)
                        {
                            if (arrTmp[i].Trim().Length > 10)
                            {
                                if (arrTmp[i].Trim().Substring(0, 10) == "boundary=\"")
                                {
                                    boundary = arrTmp[i].Trim();
                                    boundary = boundary.Substring(11, boundary.Length - 12);
                                }
                            }
                        }
                        arrTmp = null;
                        strSub = deCode(strSub);
                        strFrom = deCode(strFrom);
                        strFName = deCode(strFName);
                        strTo = deCode(strTo);
                        strTName = deCode(strTName);
                    }
                    isBody = true;
                }
                if (!isBody)
                {
                    arrTmp = strRet.Split(":".ToCharArray());
                    if (arrTmp.Length == 1)
                    {
                        switch (oldHead)
                        {
                            case ("Return-Path"):
                                strReply += arrTmp[0].Trim();
                                break;

                            case ("Date"):
                                strDate += arrTmp[0].Trim();
                                break;

                            case ("From"):
                                strFrom += arrTmp[0].Trim();
                                break;

                            case ("Message-Id"):
                                strMId += arrTmp[0].Trim();
                                break;

                            case ("To"):
                                strTo += arrTmp[0].Trim();
                                break;

                            case ("Subject"):
                                strSub += arrTmp[0].Trim();
                                break;

                            case ("Content-Type"):
                                strType += arrTmp[0].Trim();
                                break;

                            case ("Content-Transfer-Encoding"):
                                strEncode += arrTmp[0].Trim();
                                break;
                        }
                    }
                    else
                    {
                        switch (arrTmp[0].Trim())
                        {
                            case ("Return-Path"):
                                strReply = arrTmp[arrTmp.Length - 1].Trim();
                                break;

                            case ("Date"):
                                for (int i = 1; i < arrTmp.Length; i++)
                                {
                                    strDate += arrTmp[i].Trim();
                                }
                                break;

                            case ("From"):
                                strFrom = arrTmp[arrTmp.Length - 1].Trim();
                                break;

                            case ("Message-Id"):
                                strMId = arrTmp[arrTmp.Length - 1].Trim();
                                break;

                            case ("To"):
                                strTo = arrTmp[arrTmp.Length - 1].Trim();
                                break;

                            case ("Subject"):
                                strSub = arrTmp[arrTmp.Length - 1].Trim();
                                break;

                            case ("Content-Type"):
                                strType = arrTmp[arrTmp.Length - 1].Trim();
                                break;

                            case ("Content-Transfer-Encoding"):
                                strEncode = arrTmp[arrTmp.Length - 1].Trim();
                                break;
                        }
                        oldHead = arrTmp[0].Trim();
                    }
                }
                else
                {
                    strRet = strRet.Trim();
                    if (isBodyEnd)
                    {
                        if (!isAttachment)
                        {
                            if (strRet.IndexOf("name=", StringComparison.Ordinal) >= 0)
                            {
                                strRet = strRet.Substring(strRet.IndexOf("name=", StringComparison.Ordinal) + 6);
                                strAmName = deCode(strRet.Substring(0, strRet.Length).Trim());
                            }
                            if (strRet.IndexOf("Content-Transfer-Encoding", StringComparison.Ordinal) >= 0)
                            {
                                mailEncode = strRet.Substring(27).Trim();
                            }
                            if (strRet.IndexOf("Content-Disposition: attachment", StringComparison.Ordinal) >= 0)
                            {
                                attachmentCount += 1;
                            }
                        }
                        else
                        {
                            if (strRet == "" && attachmentCount > filelist.Rows.Count)
                            {
                                if (mailEncode == "base64")
                                {
                                    strAm = deCodeB64(strAm);
                                }
                                else
                                {
                                    if (mailEncode == "quoted-printable")
                                    {
                                        strAm = deCodeQP(strAm);
                                    }
                                }
                                dr = filelist.NewRow();
                                dr[0] = strAmName;
                                dr[1] = strAm;
                                this.filelist.Rows.Add(dr);
                                strAm = "";
                                strAmName = "";
                                mailEncode = "8bit";
                                isAttachment = false;
                            }
                            if (strRet != "" && attachmentCount > filelist.Rows.Count)
                            {
                                strAm += strRet;
                            }
                        }
                        if (strRet == "")
                        {
                            isAttachment = true;
                        }
                    }
                    if (strRet.IndexOf(boundary, StringComparison.Ordinal) > 0)
                    {
                        isBodyEnd = true;
                        isAttachment = false;
                    }
                }
                if (attachmentCount < 1)
                {
                    strBody += strRet;
                }
            }
            strBody += "--" + boundary;
            return true;
        }

        /*删除指定编号的邮件*/
        public bool delMail(int mIndex)
        {
            if (!OperaStream("DELE " + System.Convert.ToSingle(mIndex), "+OK"))
            {
                return false;
            }
            return true;
        }

        /*退出服务器连接*/
        public bool quitMailServer()
        {
            if (!OperaStream("quit", "+OK"))
            {
                return false;
            }
            return true;
        }

        /*解码*/
        private string deCode(string strSrc)
        {
            int start = strSrc.IndexOf("=?GB2312?", StringComparison.Ordinal);
            if (start == -1)
            {
                start = strSrc.IndexOf("=?gb2312?", StringComparison.Ordinal);
            }
            if (start >= 0)
            {
                string strHead = strSrc.Substring(0, start);
                string strMethod = strSrc.Substring(start + 9, 1);
                strSrc = strSrc.Substring(start + 11);
                int end = strSrc.IndexOf("?=", StringComparison.Ordinal);
                if (end == -1)
                {
                    end = strSrc.Length;
                }
                string strFoot = strSrc.Substring(end + 2, strSrc.Length - end - 2);
                strSrc = strSrc.Substring(0, end);
                if (strMethod == "B")
                    strSrc = strHead + deCodeB64(strSrc) + strFoot;
                else
                {
                    if (strMethod == "Q")
                        strSrc = strHead + deCodeQP(strSrc) + strFoot;
                    else
                        strSrc = strHead + strSrc + strFoot;
                }
                start = strSrc.IndexOf("=?GB2312?", StringComparison.Ordinal);
                if (start == -1)
                {
                    start = strSrc.IndexOf("=?gb2312?", StringComparison.Ordinal);
                }
                if (start >= 0)
                    strSrc = deCode(strSrc);
            }
            return strSrc;
        }

        /*Base64 解码*/
        private string deCodeB64(string strSrc)
        {
            try
            {
                if (strSrc != "")
                {
                    byte[] by = System.Convert.FromBase64String(strSrc);
                    strSrc = Encoding.Default.GetString(by);
                }
            }
            catch (Exception ex)
            { return ex.ToString(); }
            return strSrc;
        }

        /*Quoted-Printable 解码*/
        private string deCodeQP(string strSrc)
        {

            char ch, ch1, ch2;
            char[] hz;
            string strRet = "";
            hz = strSrc.ToCharArray();
            for (int i = 0; i < strSrc.Length; i++)
            {
                ch = hz[i];
                if (ch == '=')
                {
                    i++;
                    ch1 = hz[i];
                    if (ch1 == '\n') continue;
                    i++;
                    ch2 = hz[i];
                    int chint1, chint2;
                    if (ch1 > '9')
                    {
                        chint1 = (ch1 - 'A' + 10) * 16;
                    }
                    else
                    {
                        chint1 = (ch1 - '0') * 16;
                    }
                    if (ch2 > '9')
                    {
                        chint2 = ch1 - 'A' + 10;
                    }
                    else
                    {
                        chint2 = ch1 - '0';
                    }
                    ch = System.Convert.ToChar(chint1 + chint2);
                }
                strRet += ch.ToString();
            }
            return strRet;
        }

        /*获得指定附件文件名*/
        public string getAttachmentName(int AttachmentIndex)
        {
            return filelist.Rows[AttachmentIndex][0].ToString();
        }

        /*获得指定附件文件内容*/
        public string getAttachment(int AttachmentIndex)
        {
            return filelist.Rows[AttachmentIndex][1].ToString();
        }
    }
}