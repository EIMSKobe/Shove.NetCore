using System;

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
        /// <param name="mailFrom">从哪个邮箱发送，也就是对方看见的从哪发来的</param>
        /// <param name="mailTo">发到哪个邮箱，多个地址用,隔开</param>
        /// <param name="subject">邮件主题</param>
        /// <param name="body">邮件内容</param>
        /// <param name="mailServer">发送方邮件服务器，如：mail.163.com</param>
        /// <param name="mailUserName">发送方邮件用户名</param>
        /// <param name="mailUserPassword">发送方邮箱密码</param>
        /// <returns>0:OK； -1:失败</returns>
        public static int SendEmail(string mailFrom, string mailTo, string subject, string body, string mailServer, string mailUserName, string mailUserPassword)
        {
            string descrption = "";
            return SendEmail(mailFrom, "", mailTo, "", "", subject, body, null, 2, mailServer, mailUserName, mailUserPassword, ref descrption);
        }

        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="mailFrom">从哪个邮箱发送，也就是对方看见的从哪发来的</param>
        /// <param name="DisplayName">发送者名称，也就是对方看见是谁发来的</param>
        /// <param name="mailTo">发到哪个邮箱，多个地址用,隔开</param>
        /// <param name="subject">邮件主题</param>
        /// <param name="body">邮件内容</param>
        /// <param name="mailServer">发送方邮件服务器，如：mail.163.com</param>
        /// <param name="mailUserName">发送方邮件用户名</param>
        /// <param name="mailUserPassword">发送方邮箱密码</param>
        /// <returns>0:OK； -1:失败</returns>
        public static int SendEmail(string mailFrom, string DisplayName, string mailTo, string subject, string body, string mailServer, string mailUserName, string mailUserPassword)
        {
            string descrption = "";
            return SendEmail(mailFrom, DisplayName, mailTo, "", "", subject, body, null, 2, mailServer, mailUserName, mailUserPassword, ref descrption);
        }

        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="mailFrom">从哪个邮箱发送，也就是对方看见的从哪发来的</param>
        /// <param name="DisplayName">发送者名称，也就是对方看见是谁发来的</param>
        /// <param name="mailTo">发到哪个邮箱，多个地址用,隔开</param>
        /// <param name="cc">抄送的邮箱列表，多个地址用,隔开</param>
        /// <param name="subject">邮件主题</param>
        /// <param name="body">邮件内容</param>
        /// <param name="mailServer">发送方邮件服务器，如：mail.163.com</param>
        /// <param name="mailUserName">发送方邮件用户名</param>
        /// <param name="mailUserPassword">发送方邮箱密码</param>
        /// <returns>0:OK； -1:失败</returns>
        public static int SendEmail(string mailFrom, string DisplayName, string mailTo, string cc, string subject, string body, string mailServer, string mailUserName, string mailUserPassword)
        {
            string descrption = "";
            return SendEmail(mailFrom, DisplayName, mailTo, cc, "", subject, body, null, 2, mailServer, mailUserName, mailUserPassword, ref descrption);
        }

        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="mailFrom">从哪个邮箱发送，也就是对方看见的从哪发来的</param>
        /// <param name="DisplayName">发送者名称，也就是对方看见是谁发来的</param>
        /// <param name="mailTo">发到哪个邮箱，多个地址用,隔开</param>
        /// <param name="cc">抄送的邮箱列表，多个地址用,隔开</param>
        /// <param name="subject">邮件主题</param>
        /// <param name="body">邮件内容</param>
        /// <param name="attachments">附件，绝对路径文件名列表数组，可以为 null</param>
        /// <param name="mailServer">发送方邮件服务器，如：mail.163.com</param>
        /// <param name="mailUserName">发送方邮件用户名</param>
        /// <param name="mailUserPassword">发送方邮箱密码</param>
        /// <returns>0:OK； -1:失败</returns>
        public static int SendEmail(string mailFrom, string DisplayName, string mailTo, string cc, string subject, string body, string[] attachments, string mailServer, string mailUserName, string mailUserPassword)
        {
            string descrption = "";
            return SendEmail(mailFrom, DisplayName, mailTo, cc, "", subject, body, null, 2, mailServer, mailUserName, mailUserPassword, ref descrption);
        }

        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="mailFrom">从哪个邮箱发送，也就是对方看见的从哪发来的</param>
        /// <param name="DisplayName">发送者名称，也就是对方看见是谁发来的，不设置此值，自动取发送方邮件的 @ 的前面部分</param>
        /// <param name="mailTo">发到哪个邮箱，多个地址用,隔开</param>
        /// <param name="cc">抄送的邮箱列表，多个地址用,隔开</param>
        /// <param name="bcc">暗送的邮箱列表，多个刂酚码隔开。暗送是指：收件后，不会出现在邮件列表中，但可以接收得到</param>
        /// <param name="subject">邮件主题</param>
        /// <param name="body">邮件内容</param>
        /// <param name="attachments">附件，绝对路径文件名列表数组，可以为 null</param>
        /// <param name="priority">优先级：0-普通，1-低，2-高，其他值为普通</param>
        /// <param name="mailServer">发送方邮件服务器，如：mail.163.com</param>
        /// <param name="mailUserName">发送方邮件用户名</param>
        /// <param name="mailUserPassword">发送方邮箱密码</param>
        /// <param name="descrption">失败的原因</param>
        /// <returns>0:OK； -1:失败</returns>
        public static int SendEmail(string mailFrom, string DisplayName, string mailTo, string cc, string bcc, string subject, string body, string[] attachments, int priority, string mailServer, string mailUserName, string mailUserPassword, ref string descrption)
        {
            descrption = "";
            
            if (string.IsNullOrEmpty(DisplayName))
            {
                DisplayName = mailFrom;
                if (DisplayName.IndexOf('@') >= 0)
                {
                    DisplayName = DisplayName.Substring(0, mailFrom.IndexOf('@'));
                }
            }

            System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage(mailFrom, mailTo);
            mail.From = new System.Net.Mail.MailAddress(mailFrom, DisplayName, System.Text.Encoding.UTF8);
            mail.Subject = subject;
            mail.Body = body;
            mail.IsBodyHtml = true;
            mail.BodyEncoding = System.Text.Encoding.UTF8;

            // 增加抄送
            if (!string.IsNullOrEmpty(cc))
            {
                mail.CC.Add(cc);
            }

            // 增加暗送
            if (!string.IsNullOrEmpty(bcc))
            {
                mail.Bcc.Add(bcc);
            }

            // 增加附件
            if ((attachments != null) && (attachments.Length > 0))
            {
                foreach (string s in attachments)
                {
                    mail.Attachments.Add(new System.Net.Mail.Attachment(s));
                }
            }

            // 设置优先级
            switch (priority)
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

            System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient(mailServer);
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new System.Net.NetworkCredential(mailUserName, mailUserPassword);
            smtp.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;

            try
            {
                smtp.Send(mail);

                return 0; //OK
            }
            catch(Exception e)
            {
                descrption = e.Message;

                return -1;
            }
        }
    }
}
