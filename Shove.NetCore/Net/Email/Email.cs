using System;
using System.Web;

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
        /// <param name="MailFrom">���ĸ����䷢�ͣ�Ҳ���ǶԷ������Ĵ��ķ�����</param>
        /// <param name="MailTo">�����ĸ����䣬�����ַ��,����</param>
        /// <param name="Subject">�ʼ�����</param>
        /// <param name="Body">�ʼ�����</param>
        /// <param name="MailServer">���ͷ��ʼ����������磺mail.163.com</param>
        /// <param name="MailUserName">���ͷ��ʼ��û���</param>
        /// <param name="MailUserPassword">���ͷ���������</param>
        /// <returns>0:OK�� -1:ʧ��</returns>
        public static int SendEmail(string MailFrom, string MailTo, string Subject, string Body, string MailServer, string MailUserName, string MailUserPassword)
        {
            string ErrorDescrption = "";
            return SendEmail(MailFrom, "", MailTo, "", "", Subject, Body, null, 2, MailServer, MailUserName, MailUserPassword, ref ErrorDescrption);
        }

        /// <summary>
        /// �����ʼ�
        /// </summary>
        /// <param name="MailFrom">���ĸ����䷢�ͣ�Ҳ���ǶԷ������Ĵ��ķ�����</param>
        /// <param name="DisplayName">���������ƣ�Ҳ���ǶԷ�������˭������</param>
        /// <param name="MailTo">�����ĸ����䣬�����ַ��,����</param>
        /// <param name="Subject">�ʼ�����</param>
        /// <param name="Body">�ʼ�����</param>
        /// <param name="MailServer">���ͷ��ʼ����������磺mail.163.com</param>
        /// <param name="MailUserName">���ͷ��ʼ��û���</param>
        /// <param name="MailUserPassword">���ͷ���������</param>
        /// <returns>0:OK�� -1:ʧ��</returns>
        public static int SendEmail(string MailFrom, string DisplayName, string MailTo, string Subject, string Body, string MailServer, string MailUserName, string MailUserPassword)
        {
            string ErrorDescrption = "";
            return SendEmail(MailFrom, DisplayName, MailTo, "", "", Subject, Body, null, 2, MailServer, MailUserName, MailUserPassword, ref ErrorDescrption);
        }

        /// <summary>
        /// �����ʼ�
        /// </summary>
        /// <param name="MailFrom">���ĸ����䷢�ͣ�Ҳ���ǶԷ������Ĵ��ķ�����</param>
        /// <param name="DisplayName">���������ƣ�Ҳ���ǶԷ�������˭������</param>
        /// <param name="MailTo">�����ĸ����䣬�����ַ��,����</param>
        /// <param name="CC">���͵������б������ַ��,����</param>
        /// <param name="Subject">�ʼ�����</param>
        /// <param name="Body">�ʼ�����</param>
        /// <param name="MailServer">���ͷ��ʼ����������磺mail.163.com</param>
        /// <param name="MailUserName">���ͷ��ʼ��û���</param>
        /// <param name="MailUserPassword">���ͷ���������</param>
        /// <returns>0:OK�� -1:ʧ��</returns>
        public static int SendEmail(string MailFrom, string DisplayName, string MailTo, string CC, string Subject, string Body, string MailServer, string MailUserName, string MailUserPassword)
        {
            string ErrorDescrption = "";
            return SendEmail(MailFrom, DisplayName, MailTo, CC, "", Subject, Body, null, 2, MailServer, MailUserName, MailUserPassword, ref ErrorDescrption);
        }

        /// <summary>
        /// �����ʼ�
        /// </summary>
        /// <param name="MailFrom">���ĸ����䷢�ͣ�Ҳ���ǶԷ������Ĵ��ķ�����</param>
        /// <param name="DisplayName">���������ƣ�Ҳ���ǶԷ�������˭������</param>
        /// <param name="MailTo">�����ĸ����䣬�����ַ��,����</param>
        /// <param name="CC">���͵������б������ַ��,����</param>
        /// <param name="Subject">�ʼ�����</param>
        /// <param name="Body">�ʼ�����</param>
        /// <param name="Attachments">����������·���ļ����б����飬����Ϊ null</param>
        /// <param name="MailServer">���ͷ��ʼ����������磺mail.163.com</param>
        /// <param name="MailUserName">���ͷ��ʼ��û���</param>
        /// <param name="MailUserPassword">���ͷ���������</param>
        /// <returns>0:OK�� -1:ʧ��</returns>
        public static int SendEmail(string MailFrom, string DisplayName, string MailTo, string CC, string Subject, string Body, string[] Attachments, string MailServer, string MailUserName, string MailUserPassword)
        {
            string ErrorDescrption = "";
            return SendEmail(MailFrom, DisplayName, MailTo, CC, "", Subject, Body, null, 2, MailServer, MailUserName, MailUserPassword, ref ErrorDescrption);
        }

        /// <summary>
        /// �����ʼ�
        /// </summary>
        /// <param name="MailFrom">���ĸ����䷢�ͣ�Ҳ���ǶԷ������Ĵ��ķ�����</param>
        /// <param name="DisplayName">���������ƣ�Ҳ���ǶԷ�������˭�����ģ������ô�ֵ���Զ�ȡ���ͷ��ʼ��� @ ��ǰ�沿��</param>
        /// <param name="MailTo">�����ĸ����䣬�����ַ��,����</param>
        /// <param name="CC">���͵������б������ַ��,����</param>
        /// <param name="Bcc">���͵������б�����ַ��������������ָ���ռ��󣬲���������ʼ��б��У������Խ��յõ�</param>
        /// <param name="Subject">�ʼ�����</param>
        /// <param name="Body">�ʼ�����</param>
        /// <param name="Attachments">����������·���ļ����б����飬����Ϊ null</param>
        /// <param name="Priority">���ȼ���0-��ͨ��1-�ͣ�2-�ߣ�����ֵΪ��ͨ</param>
        /// <param name="MailServer">���ͷ��ʼ����������磺mail.163.com</param>
        /// <param name="MailUserName">���ͷ��ʼ��û���</param>
        /// <param name="MailUserPassword">���ͷ���������</param>
        /// <param name="ErrorDescrption">ʧ�ܵ�ԭ��</param>
        /// <returns>0:OK�� -1:ʧ��</returns>
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

            // ���ӳ���
            if (!string.IsNullOrEmpty(CC))
            {
                mail.CC.Add(CC);
            }

            // ���Ӱ���
            if (!string.IsNullOrEmpty(Bcc))
            {
                mail.Bcc.Add(Bcc);
            }

            // ���Ӹ���
            if ((Attachments != null) && (Attachments.Length > 0))
            {
                foreach (string s in Attachments)
                {
                    mail.Attachments.Add(new System.Net.Mail.Attachment(s));
                }
            }

            // �������ȼ�
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
