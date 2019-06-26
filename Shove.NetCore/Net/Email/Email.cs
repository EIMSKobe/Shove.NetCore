using System;
using System.Web;

namespace Shove.Net
{
    /// <summary>
    /// Email 的摘要说明。
    /// </summary>
    public class Email
    {
        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="MailFrom">从哪个邮箱发送，也就是对方看见的从哪发来的</param>
        /// <param name="MailTo">发到哪个邮箱，多个地址用,隔开</param>
        /// <param name="Subject">邮件主题</param>
        /// <param name="Body">邮件内容</param>
        /// <param name="MailServer">发送方邮件服务器，如：mail.163.com</param>
        /// <param name="MailUserName">发送方邮件用户名</param>
        /// <param name="MailUserPassword">发送方邮箱密码</param>
        /// <returns>0:OK； -1:失败</returns>
        public static int SendEmail(string MailFrom, string MailTo, string Subject, string Body, string MailServer, string MailUserName, string MailUserPassword)
        {
            string ErrorDescrption = "";
            return SendEmail(MailFrom, "", MailTo, "", "", Subject, Body, null, 2, MailServer, MailUserName, MailUserPassword, ref ErrorDescrption);
        }

        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="MailFrom">从哪个邮箱发送，也就是对方看见的从哪发来的</param>
        /// <param name="DisplayName">发送者名称，也就是对方看见是谁发来的</param>
        /// <param name="MailTo">发到哪个邮箱，多个地址用,隔开</param>
        /// <param name="Subject">邮件主题</param>
        /// <param name="Body">邮件内容</param>
        /// <param name="MailServer">发送方邮件服务器，如：mail.163.com</param>
        /// <param name="MailUserName">发送方邮件用户名</param>
        /// <param name="MailUserPassword">发送方邮箱密码</param>
        /// <returns>0:OK； -1:失败</returns>
        public static int SendEmail(string MailFrom, string DisplayName, string MailTo, string Subject, string Body, string MailServer, string MailUserName, string MailUserPassword)
        {
            string ErrorDescrption = "";
            return SendEmail(MailFrom, DisplayName, MailTo, "", "", Subject, Body, null, 2, MailServer, MailUserName, MailUserPassword, ref ErrorDescrption);
        }

        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="MailFrom">从哪个邮箱发送，也就是对方看见的从哪发来的</param>
        /// <param name="DisplayName">发送者名称，也就是对方看见是谁发来的</param>
        /// <param name="MailTo">发到哪个邮箱，多个地址用,隔开</param>
        /// <param name="CC">抄送的邮箱列表，多个地址用,隔开</param>
        /// <param name="Subject">邮件主题</param>
        /// <param name="Body">邮件内容</param>
        /// <param name="MailServer">发送方邮件服务器，如：mail.163.com</param>
        /// <param name="MailUserName">发送方邮件用户名</param>
        /// <param name="MailUserPassword">发送方邮箱密码</param>
        /// <returns>0:OK； -1:失败</returns>
        public static int SendEmail(string MailFrom, string DisplayName, string MailTo, string CC, string Subject, string Body, string MailServer, string MailUserName, string MailUserPassword)
        {
            string ErrorDescrption = "";
            return SendEmail(MailFrom, DisplayName, MailTo, CC, "", Subject, Body, null, 2, MailServer, MailUserName, MailUserPassword, ref ErrorDescrption);
        }

        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="MailFrom">从哪个邮箱发送，也就是对方看见的从哪发来的</param>
        /// <param name="DisplayName">发送者名称，也就是对方看见是谁发来的</param>
        /// <param name="MailTo">发到哪个邮箱，多个地址用,隔开</param>
        /// <param name="CC">抄送的邮箱列表，多个地址用,隔开</param>
        /// <param name="Subject">邮件主题</param>
        /// <param name="Body">邮件内容</param>
        /// <param name="Attachments">附件，绝对路径文件名列表数组，可以为 null</param>
        /// <param name="MailServer">发送方邮件服务器，如：mail.163.com</param>
        /// <param name="MailUserName">发送方邮件用户名</param>
        /// <param name="MailUserPassword">发送方邮箱密码</param>
        /// <returns>0:OK； -1:失败</returns>
        public static int SendEmail(string MailFrom, string DisplayName, string MailTo, string CC, string Subject, string Body, string[] Attachments, string MailServer, string MailUserName, string MailUserPassword)
        {
            string ErrorDescrption = "";
            return SendEmail(MailFrom, DisplayName, MailTo, CC, "", Subject, Body, null, 2, MailServer, MailUserName, MailUserPassword, ref ErrorDescrption);
        }

        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="MailFrom">从哪个邮箱发送，也就是对方看见的从哪发来的</param>
        /// <param name="DisplayName">发送者名称，也就是对方看见是谁发来的，不设置此值，自动取发送方邮件的 @ 的前面部分</param>
        /// <param name="MailTo">发到哪个邮箱，多个地址用,隔开</param>
        /// <param name="CC">抄送的邮箱列表，多个地址用,隔开</param>
        /// <param name="Bcc">暗送的邮箱列表，多个刂酚码隔开。暗送是指：收件后，不会出现在邮件列表中，但可以接收得到</param>
        /// <param name="Subject">邮件主题</param>
        /// <param name="Body">邮件内容</param>
        /// <param name="Attachments">附件，绝对路径文件名列表数组，可以为 null</param>
        /// <param name="Priority">优先级：0-普通，1-低，2-高，其他值为普通</param>
        /// <param name="MailServer">发送方邮件服务器，如：mail.163.com</param>
        /// <param name="MailUserName">发送方邮件用户名</param>
        /// <param name="MailUserPassword">发送方邮箱密码</param>
        /// <param name="ErrorDescrption">失败的原因</param>
        /// <returns>0:OK； -1:失败</returns>
        public static int SendEmail(string MailFrom, string DisplayName, string MailTo, string CC, string Bcc, string Subject, string Body, string[] Attachments, int Priority, string MailServer, string MailUserName, string MailUserPassword, ref string ErrorDescrption)
        {
            ErrorDescrption = "";
            
            if (string.IsNullOrEmpty(DisplayName))
            {
                DisplayName = MailFrom;
                if (DisplayName.IndexOf('@') >= 0)
                {
                    DisplayName = DisplayName.Substring(0, MailFrom.IndexOf('@'));
                }
            }

            System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage(MailFrom, MailTo);
            mail.From = new System.Net.Mail.MailAddress(MailFrom, DisplayName, System.Text.Encoding.UTF8);
            mail.Subject = Subject;
            mail.Body = Body;
            mail.IsBodyHtml = true;
            mail.BodyEncoding = System.Text.Encoding.UTF8;

            // 增加抄送
            if (!string.IsNullOrEmpty(CC))
            {
                mail.CC.Add(CC);
            }

            // 增加暗送
            if (!string.IsNullOrEmpty(Bcc))
            {
                mail.Bcc.Add(Bcc);
            }

            // 增加附件
            if ((Attachments != null) && (Attachments.Length > 0))
            {
                foreach (string s in Attachments)
                {
                    mail.Attachments.Add(new System.Net.Mail.Attachment(s));
                }
            }

            // 设置优先级
            switch (Priority)
            {
                case 1:
                    mail.Priority = System.Net.Mail.MailPriority.Low;
                    break;
                case 2:
                    mail.Priority = System.Net.Mail.MailPriority.High;
                    break;
                default:
                    mail.Priority = System.Net.Mail.MailPriority.Normal;
                    break;
            }

            System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient(MailServer);
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new System.Net.NetworkCredential(MailUserName, MailUserPassword);
            smtp.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;

            try
            {
                smtp.Send(mail);

                return 0; //OK
            }
            catch(Exception e)
            {
                ErrorDescrption = e.Message;

                return -1;
            }
        }
    }
}
