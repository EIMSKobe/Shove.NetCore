using System;
using System.Collections.Generic;
using System.Text;

namespace Shove.Net.Ftp
{
    /// <summary>
    /// ����״̬��ö��
    /// </summary>
    public enum Status
    {
        /// <summary>
        /// ��δ��ʼ���䣬����ȴ���
        /// </summary>
        Waiting,

        /// <summary>
        /// ���� FTP ��������
        /// </summary>
        Connecting,

        /// <summary>
        /// ���ڴ�����
        /// </summary>
        Transfering,

        /// <summary>
        /// ����������ɹ���
        /// </summary>
        Finished,

        /// <summary>
        /// ����ʧ��(������ϵ�����ԭ��)
        /// </summary>
        Failed,

        /// <summary>
        /// ����ʧ��(�ļ���ռ��)
        /// </summary>
        Using,

        /// <summary>
        /// �ļ��Ѿ����ڣ��ȴ���һָ��
        /// </summary>
        Exists,

        /// <summary>
        /// ������ȡ��
        /// </summary>
        Canceled
    }
}
