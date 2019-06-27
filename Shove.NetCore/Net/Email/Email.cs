using System;

namespace Shove.Net
{
    /// <summary>
    /// Email ��ժҪ˵����
    /// </summary>
    public class Email
    {
        /// <summary>
        /// �����ʼ�
        /// </summary>
        /// <param name="mailFrom">���ĸ����䷢�ͣ�Ҳ���ǶԷ������Ĵ��ķ�����</param>
        /// <param name="mailTo">�����ĸ����䣬�����ַ��,����</param>
        /// <param name="subject">�ʼ�����</param>
        /// <param name="body">�ʼ�����</param>
        /// <param name="mailServer">���ͷ��ʼ����������磺mail.163.com</param>
        /// <param name="mailUserName">���ͷ��ʼ��û���</param>
        /// <param name="mailUserPassword">���ͷ���������</param>
        /// <returns>0:OK�� -1:ʧ��</returns>
        public static int SendEmail(string mailFrom, string mailTo, string subject, string body, string mailServer, string mailUserName, string mailUserPassword)
        {
            string descrption = "";
            return SendEmail(mailFrom, "", mailTo, "", "", subject, body, null, 2, mailServer, mailUserName, mailUserPassword, ref descrption);
        }

        /// <summary>
        /// �����ʼ�
        /// </summary>
        /// <param name="mailFrom">���ĸ����䷢�ͣ�Ҳ���ǶԷ������Ĵ��ķ�����</param>
        /// <param name="DisplayName">���������ƣ�Ҳ���ǶԷ�������˭������</param>
        /// <param name="mailTo">�����ĸ����䣬�����ַ��,����</param>
        /// <param name="subject">�ʼ�����</param>
        /// <param name="body">�ʼ�����</param>
        /// <param name="mailServer">���ͷ��ʼ����������磺mail.163.com</param>
        /// <param name="mailUserName">���ͷ��ʼ��û���</param>
        /// <param name="mailUserPassword">���ͷ���������</param>
        /// <returns>0:OK�� -1:ʧ��</returns>
        public static int SendEmail(string mailFrom, string DisplayName, string mailTo, string subject, string body, string mailServer, string mailUserName, string mailUserPassword)
        {
            string descrption = "";
            return SendEmail(mailFrom, DisplayName, mailTo, "", "", subject, body, null, 2, mailServer, mailUserName, mailUserPassword, ref descrption);
        }

        /// <summary>
        /// �����ʼ�
        /// </summary>
        /// <param name="mailFrom">���ĸ����䷢�ͣ�Ҳ���ǶԷ������Ĵ��ķ�����</param>
        /// <param name="DisplayName">���������ƣ�Ҳ���ǶԷ�������˭������</param>
        /// <param name="mailTo">�����ĸ����䣬�����ַ��,����</param>
        /// <param name="cc">���͵������б������ַ��,����</param>
        /// <param name="subject">�ʼ�����</param>
        /// <param name="body">�ʼ�����</param>
        /// <param name="mailServer">���ͷ��ʼ����������磺mail.163.com</param>
        /// <param name="mailUserName">���ͷ��ʼ��û���</param>
        /// <param name="mailUserPassword">���ͷ���������</param>
        /// <returns>0:OK�� -1:ʧ��</returns>
        public static int SendEmail(string mailFrom, string DisplayName, string mailTo, string cc, string subject, string body, string mailServer, string mailUserName, string mailUserPassword)
        {
            string descrption = "";
            return SendEmail(mailFrom, DisplayName, mailTo, cc, "", subject, body, null, 2, mailServer, mailUserName, mailUserPassword, ref descrption);
        }

        /// <summary>
        /// �����ʼ�
        /// </summary>
        /// <param name="mailFrom">���ĸ����䷢�ͣ�Ҳ���ǶԷ������Ĵ��ķ�����</param>
        /// <param name="DisplayName">���������ƣ�Ҳ���ǶԷ�������˭������</param>
        /// <param name="mailTo">�����ĸ����䣬�����ַ��,����</param>
        /// <param name="cc">���͵������б������ַ��,����</param>
        /// <param name="subject">�ʼ�����</param>
        /// <param name="body">�ʼ�����</param>
        /// <param name="attachments">����������·���ļ����б����飬����Ϊ null</param>
        /// <param name="mailServer">���ͷ��ʼ����������磺mail.163.com</param>
        /// <param name="mailUserName">���ͷ��ʼ��û���</param>
        /// <param name="mailUserPassword">���ͷ���������</param>
        /// <returns>0:OK�� -1:ʧ��</returns>
        public static int SendEmail(string mailFrom, string DisplayName, string mailTo, string cc, string subject, string body, string[] attachments, string mailServer, string mailUserName, string mailUserPassword)
        {
            string descrption = "";
            return SendEmail(mailFrom, DisplayName, mailTo, cc, "", subject, body, null, 2, mailServer, mailUserName, mailUserPassword, ref descrption);
        }

        /// <summary>
        /// �����ʼ�
        /// </summary>
        /// <param name="mailFrom">���ĸ����䷢�ͣ�Ҳ���ǶԷ������Ĵ��ķ�����</param>
        /// <param name="DisplayName">���������ƣ�Ҳ���ǶԷ�������˭�����ģ������ô�ֵ���Զ�ȡ���ͷ��ʼ��� @ ��ǰ�沿��</param>
        /// <param name="mailTo">�����ĸ����䣬�����ַ��,����</param>
        /// <param name="cc">���͵������б������ַ��,����</param>
        /// <param name="bcc">���͵������б�����ַ��������������ָ���ռ��󣬲���������ʼ��б��У������Խ��յõ�</param>
        /// <param name="subject">�ʼ�����</param>
        /// <param name="body">�ʼ�����</param>
        /// <param name="attachments">����������·���ļ����б����飬����Ϊ null</param>
        /// <param name="priority">���ȼ���0-��ͨ��1-�ͣ�2-�ߣ�����ֵΪ��ͨ</param>
        /// <param name="mailServer">���ͷ��ʼ����������磺mail.163.com</param>
        /// <param name="mailUserName">���ͷ��ʼ��û���</param>
        /// <param name="mailUserPassword">���ͷ���������</param>
        /// <param name="descrption">ʧ�ܵ�ԭ��</param>
        /// <returns>0:OK�� -1:ʧ��</returns>
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

            // ���ӳ���
            if (!string.IsNullOrEmpty(cc))
            {
                mail.CC.Add(cc);
            }

            // ���Ӱ���
            if (!string.IsNullOrEmpty(bcc))
            {
                mail.Bcc.Add(bcc);
            }

            // ���Ӹ���
            if ((attachments != null) && (attachments.Length > 0))
            {
                foreach (string s in attachments)
                {
                    mail.Attachments.Add(new System.Net.Mail.Attachment(s));
                }
            }

            // �������ȼ�
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
