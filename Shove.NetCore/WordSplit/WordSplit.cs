using System;
using System.Collections;

namespace Shove.WordSplit
{
    /// <summary>
    /// ���ķִ�
    /// </summary>
    public class ShoveWordSplit
    {
        /// <summary>
        /// �ʵ��
        /// </summary>
        public Dictionary m_Dict;

        /// <summary>
        /// 
        /// </summary>
        public ShoveWordSplit()
        {
            m_Dict = new Dictionary();
        }

        /// <summary>
        /// ���졢װ�شʿ�
        /// </summary>
        /// <param name="chineseDictionaryFileName">����·���ļ���</param>
        public ShoveWordSplit(string chineseDictionaryFileName)
        {
            if (chineseDictionaryFileName.Trim() == "")
                m_Dict = new Dictionary();
            else
                m_Dict = new Dictionary(chineseDictionaryFileName);
        }

        /*
        �ִ�ʹ�÷�����
        Shove.WordSplit.ShoveWordSplit cws = new Shove.WordSplit.ShoveWordSplit(@"D:/My Work Spaces/Temp/WindowsFormsApplication2/WindowsFormsApplication2/bin/Debug/ChineseDictionary.dat");
        string[] Result = cws.GetSlpitWords(textBox1.Text);
        if ((Result == null) || (Result.Length < 1))
        {
            textBox2.Text = "No";
            return;
        }
        StringBuilder sb = new StringBuilder();
        foreach (string str in Result)
        {
            sb.Append(str + "\r\n");
        }
        textBox2.Text = sb.ToString();
        */
        /// <summary>
        /// �ִ�
        /// </summary>
        /// <param name="sSource"></param>
        /// <returns></returns>
        public string[] GetSlpitWords(string sSource)
        {
            if (!m_Dict.m_isOpen)
                return null;

            sSource = sSource.Trim();
            if (sSource == "") return null;

            string[] sResult;

            if (sSource.Length == 1)
            {
                int iWordType = m_Dict.GetWordType(sSource[0]);
                if ((iWordType == 0) || (iWordType > 10))
                {
                    sResult = new string[1];
                    sResult[0] = sSource;
                    return sResult;
                }
                else
                    return null;
            }

            ArrayList al = new ArrayList();
            int iLocate = 0; //����ԭ��
            int i;
            while (iLocate < sSource.Length)
            {
                string sWords = sSource.Substring(iLocate, 1); //��
                string sWord = sSource.Substring(iLocate, 1); //��

                int iWordType = m_Dict.GetWordType(sWord[0]);	//�ַ������	-1 - ��Ӣ����ĸ�����������֣����֣�����!@#$%^&*()_-+=|\~`[]{}<>./�ȷ���(����������Χ֮��)
                //				0 - Ӣ����ĸ�����������֣�����!@#$%^&*()_-+=|\~`[]{}<>./�ȷ���
                //				10 - �������֣� 11 - �������ʣ� 12 - �������֣� 13 - ��ͨ����

                if (iWordType < 0)
                {
                    iLocate++;
                    continue;
                }

                if ((iWordType == 0) || (iWordType == 1)) //�������ĸ�����������֡�����!@#$%^&*()_-+=|\~`[]{}<>./�ȷ��ţ�����ȡ�����������ֱ��������ЩΪֹ�����Բ�� MP3, MP4, P3, 256M, PIII,C++,c# ��..
                {
                    int LetterAndNumericCount = 0;
                    if (iWordType == 0) LetterAndNumericCount++;
                    while (++iLocate < sSource.Length)
                    {
                        sWord = sSource.Substring(iLocate, 1);
                        iWordType = m_Dict.GetWordType(sWord[0]);
                        if ((iWordType == 0) || (iWordType == 1))
                        {
                            sWords += sWord;
                            if (iWordType == 0)
                                LetterAndNumericCount++;
                        }
                        else
                            break;
                    }
                    if (LetterAndNumericCount > 0)
                        al.Add(sWords);
                    continue;
                }

                if (iWordType >= 10) //����Ǻ���
                {
                    int Len = sSource.Length - iLocate;
                    if (Len > m_Dict.m_WordMaxLen) Len = m_Dict.m_WordMaxLen;
                    string sFoundWord = m_Dict.GetWordAtDictionary(sSource.Substring(iLocate, Len));
                    if (sFoundWord != "")	//�Ǵʣ���ӣ����ء��ټ����Һ���
                    {
                        al.Add(sFoundWord);
                        iLocate += sFoundWord.Length;
                        continue;
                    }
                    else	//���ǴʵĻ�������2�������1���Ƿ������֡����� 2��ǰ��һ�����Ƿ���Լ�����1������Ȼ�Ǵʣ�ʣ���־Ϳ��Ƿ�����(�ͼ���֮�����������)���������ٺͺ����ܷ��Ǵʡ�
                    {
                        if ((iWordType == 10) || (iWordType == 11))	//������������֡����ʣ�����ȡ�����治���������ֻ�����
                        {
                            while (++iLocate < sSource.Length)
                            {
                                sWord = sSource.Substring(iLocate, 1);
                                iWordType = m_Dict.GetWordType(sWord[0]);
                                if ((iWordType == 10) || (iWordType == 11))
                                    sWords += sWord;
                                else
                                    break;
                            }
                            al.Add(sWords);
                            continue;
                        }

                        bool CanSubsidy = false;	//�����ܷ�ǰ��ʲ���
                        if (al.Count > 0)	//ǰ���дʣ����ܷ����ܷ���
                        {
                            string PreWords = al[al.Count - 1].ToString();
                            if (PreWords.Length > 1)	//ǰ��Ĵ�����2���ӣ����ܷ����ܷ���
                            {
                                if (m_Dict.GetWordType(PreWords[PreWords.Length - 1]) >= 10)	//��һ���ʵ�����Ǻ��֣����ܷ����ܷ���
                                {
                                    string PreWord = PreWords.Substring(PreWords.Length - 1, 1);	//ȡ����������
                                    PreWords = PreWords.Substring(0, PreWords.Length - 1);			//ȥ�������ֺ����һ����

                                    if (PreWords.Length == 1)	//���������ֻʣһ���֣���������������֡����ʻ����ʣ����ܲ���
                                    {
                                        int PreWordType = m_Dict.GetWordType(PreWords[0]);
                                        if ((PreWordType == 10) || (PreWordType == 11) || (PreWordType == 12))
                                            CanSubsidy = true;
                                    }
                                    else
                                    {
                                        if (m_Dict.WordExist(PreWords))	//������������Ǵ�
                                            CanSubsidy = true;
                                        else
                                        {
                                            int PrePreWordType = m_Dict.GetWordType(PreWords[PreWords.Length - 1]);
                                            if ((PrePreWordType == 10) || (PrePreWordType == 11))
                                                CanSubsidy = true;
                                        }
                                    }
                                }
                            }
                        }
                        if (CanSubsidy)	//�ܹ������Ļ����������󣬺����ܷ�ɴ�
                        {
                            string sFoundWordNext = "";
                            Len = sSource.Length - iLocate + 1;
                            if (Len > m_Dict.m_WordMaxLen) Len = m_Dict.m_WordMaxLen;
                            sFoundWordNext = m_Dict.GetWordAtDictionary(sSource.Substring(iLocate - 1, Len));
                            if (sFoundWordNext != "")	//���������Ϊ�ʣ�����������
                            {
                                string PreWords = al[al.Count - 1].ToString();
                                al[al.Count - 1] = PreWords.Substring(0, PreWords.Length - 1);
                                al.Add(sFoundWordNext);
                                iLocate += sFoundWordNext.Length - 1;
                                continue;
                            }
                        }
                        //���ܲ�������Ҫ����
                        al.Add(sWord);
                        iLocate++;
                        continue;
                    }
                }
            }

            if (al.Count == 0)
                return null;

            sResult = new string[al.Count];
            for (i = 0; i < al.Count; i++)
                sResult[i] = al[i].ToString();
            return sResult;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sWordsList"></param>
        /// <returns></returns>
        public string[] ReBuildWords(string[] sWordsList)
        {
            if (sWordsList == null)
                return null;

            ArrayList al = new ArrayList();
            for (int i = 0; i < sWordsList.Length; i++)
            {
                bool isExist = false;
                for (int j = 0; j < al.Count; j++)
                {
                    if (al[j].ToString() == sWordsList[i])
                    {
                        isExist = true;
                        break;
                    }
                }
                if (!isExist)
                    al.Add(sWordsList[i]);
            }

            if (al.Count == 0)
                return null;

            string[] sResult = new string[al.Count];
            for (int i = 0; i < al.Count; i++)
                sResult[i] = al[i].ToString();
            return sResult;
        }
    }
}
