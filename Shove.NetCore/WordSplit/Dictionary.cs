using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Shove.WordSplit
{
    /// <summary>
    /// WordsList
    /// </summary>
    [Serializable]
    public struct WordsList
    {
        /// <summary>
        /// 
        /// </summary>
        public ArrayList m_List;
    }

    /// <summary>
    /// Dictionary
    /// </summary>
    public class Dictionary
    {
        /// <summary>
        /// 
        /// </summary>
        public WordsList[] m_WordsList;
        /// <summary>
        /// 
        /// </summary>
        public bool m_isOpen;
        /// <summary>
        /// 
        /// </summary>
        public int m_FlagNum = 20901;	//UniCode ����� 20901 ������
        /// <summary>
        /// 
        /// </summary>
        public int m_WordMaxLen;
        /// <summary>
        /// 
        /// </summary>
        private string ChineseDictionaryFileName;

        /// <summary>
        /// Dictionary
        /// </summary>
        public Dictionary()
        {
            m_WordMaxLen = 0;
            ChineseDictionaryFileName = "./ChineseDictionary.dat";
            m_isOpen = OpenDictionary();
        }

        /// <summary>
        /// Dictionary
        /// </summary>
        /// <param name="chineseDictionaryFileName"></param>
        public Dictionary(string chineseDictionaryFileName)
        {
            m_WordMaxLen = 0;
            ChineseDictionaryFileName = chineseDictionaryFileName;
            m_isOpen = OpenDictionary();
        }

        /// <summary>
        /// OpenDictionary
        /// </summary>
        /// <returns></returns>
        public bool OpenDictionary()
        {
            //���ʿ�
            BinaryFormatter bfs = new BinaryFormatter();
            Stream dfs = null;
            try
            {
                dfs = new FileStream(ChineseDictionaryFileName, FileMode.Open, FileAccess.Read);
                m_WordsList = (WordsList[])bfs.Deserialize(dfs);
            }
            catch(Exception e)
            {
                if (dfs != null)
                    dfs.Close();

                throw e;
                //return false;
            }
            dfs.Close();
            //if (m_WordsList.Length != 20901)
            //    return false;

            //ȡ��Ĵʵĳ���
            CalcWordMaxLen();

            return true;
        }

        /// <summary>
        /// FindWordsList
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public ArrayList FindWordsList(string ch)
        {
            int Unicode = (int)((char)ch[0]) - 19968;
            if ((Unicode < 0) || (Unicode > 20900))
                return null;
            return m_WordsList[Unicode].m_List;
        }

        /// <summary>
        /// GetWordsListAll
        /// </summary>
        /// <param name="WithFlag"></param>
        /// <param name="ReSort"></param>
        /// <returns></returns>
        public ArrayList GetWordsListAll(bool WithFlag, bool ReSort)
        {
            if (!m_isOpen)
                return null;

            ArrayList al = new ArrayList();
            for (int i = 0; i < 20901; i++)
            {
                if (m_WordsList[i].m_List != null)
                {
                    for (int j = 0; j < m_WordsList[i].m_List.Count; j++)
                    {
                        if (WithFlag)
                            al.Add(((char)(i + 19968)).ToString() + m_WordsList[i].m_List[j].ToString());
                        else
                            al.Add(m_WordsList[i].m_List[j]);
                    }
                }
            }
            if (ReSort)
            {
                CompareToAscii compare = new CompareToAscii();
                al.Sort(compare);
            }
            return al;
        }

        /// <summary>
        /// AddWord
        /// </summary>
        /// <param name="Word"></param>
        /// <returns></returns>
        public bool AddWord(string Word)
        {
            if (!m_isOpen)
                return false;

            Word = Word.Trim();
            if (Word.Length < 2)
                return false;

            int Unicode = (int)((char)Word[0]) - 19968;
            if ((Unicode < 0) || (Unicode > 20900))
                return false;

            if (m_WordsList[Unicode].m_List == null)
                m_WordsList[Unicode].m_List = new ArrayList();
            m_WordsList[Unicode].m_List.Add(Word.Substring(1, Word.Length - 1));
            WordSort(Unicode);
            return true;
        }

        /// <summary>
        /// DeleteWord
        /// </summary>
        /// <param name="Word"></param>
        /// <returns></returns>
        public bool DeleteWord(string Word)
        {
            if (!m_isOpen)
                return false;

            Word = Word.Trim();
            if (Word.Length < 2)
                return false;

            int Unicode = (int)((char)Word[0]) - 19968;
            if ((Unicode < 0) || (Unicode > 20900))
                return false;

            if (m_WordsList[Unicode].m_List == null)
                return false;

            int i;
            Word = Word.Substring(1, Word.Length - 1);
            for (i = 0; i < m_WordsList[Unicode].m_List.Count; i++)
            {
                if (m_WordsList[Unicode].m_List[i].ToString() == Word)
                {
                    m_WordsList[Unicode].m_List.RemoveAt(i);
                    CalcWordMaxLen();
                    return true;
                }
            }
            return false;
        }

        private class CompareToLength : IComparer
        {
            int IComparer.Compare(Object x, Object y)
            {
                return ((new CaseInsensitiveComparer()).Compare(((string)y).Length, ((string)x).Length));
            }
        }

        private class CompareToAscii : IComparer
        {
            int IComparer.Compare(Object x, Object y)
            {
                return ((new CaseInsensitiveComparer()).Compare(x, y));
            }
        }

        /// <summary>
        /// WordSort
        /// </summary>
        /// <returns></returns>
        public bool WordSort()
        {
            if (!m_isOpen)
                return false;

            CompareToLength compare = new CompareToLength();

            for (int i = 0; i < 20900; i++)
            {
                if (m_WordsList[i].m_List == null)
                    continue;
                m_WordsList[i].m_List.Sort(compare);
            }
            return true;
        }

        /// <summary>
        /// WordSort
        /// </summary>
        /// <param name="Unicode"></param>
        /// <returns></returns>
        public bool WordSort(int Unicode)
        {
            if (!m_isOpen)
                return false;
            if ((Unicode < 0) || (Unicode > 20900))
                return false;
            if (m_WordsList[Unicode].m_List == null)
                return false;

            CompareToLength compare = new CompareToLength();
            m_WordsList[Unicode].m_List.Sort(compare);
            return true;
        }

        /// <summary>
        /// WordExist
        /// </summary>
        /// <param name="sWord"></param>
        /// <returns></returns>
        public bool WordExist(string sWord)
        {
            if (!m_isOpen)
                return false;

            sWord = sWord.Trim();
            if (sWord.Length < 2)
                return false;

            string ch = sWord[0].ToString();
            ArrayList alWords = FindWordsList(ch);
            if (alWords == null)
                return false;

            sWord = sWord.Substring(1, sWord.Length - 1);
            for (int i = 0; i < alWords.Count; i++)
            {
                if (alWords[i].ToString() == sWord)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Save
        /// </summary>
        /// <returns></returns>
        public bool Save()
        {
            return Save(ChineseDictionaryFileName);
        }

        /// <summary>
        /// Save
        /// </summary>
        /// <param name="chineseDictionaryFileName"></param>
        /// <returns></returns>
        public bool Save(string chineseDictionaryFileName)
        {
            if (!m_isOpen)
                return false;

            IFormatter formatter = new BinaryFormatter();
            Stream stream = null;
            try
            {
                stream = new FileStream(chineseDictionaryFileName, FileMode.Create, FileAccess.Write, FileShare.None);
                formatter.Serialize(stream, m_WordsList);
                stream.Close();
                return true;
            }
            catch
            {
                if (stream != null)
                    stream.Close();
                return false;
            }
        }

        /// <summary>
        /// SaveToTxtFile
        /// </summary>
        /// <param name="TxtFileName"></param>
        /// <returns></returns>
        public bool SaveToTxtFile(string TxtFileName)
        {
            ArrayList al = GetWordsListAll(true, true);
            if (al == null)
                return false;

            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(TxtFileName);
            }
            catch
            {
                return false;
            }

            for (int i = 0; i < al.Count; i++)
                sw.WriteLine(al[i].ToString());
            sw.Close();
            return true;
        }

        /// <summary>
        /// LoadFromTxtFile
        /// </summary>
        /// <param name="TxtFileName"></param>
        /// <returns></returns>
        public bool LoadFromTxtFile(string TxtFileName)
        {
            StreamReader sr;
            try
            {
                sr = new StreamReader(TxtFileName);
            }
            catch
            {
                return false;
            }

            for (int i = 0; i < 20901; i++)
            {
                if (m_WordsList[i].m_List != null)
                    m_WordsList[i].m_List.Clear();
            }

            string Word = "";
            int Unicode = 0;
            for (Word = sr.ReadLine(); Word != null; Word = sr.ReadLine())
            {
                Word = Word.Trim();
                if (Word.Length < 2)
                    continue;

                Unicode = (int)((char)Word[0]) - 19968;
                if ((Unicode < 0) || (Unicode > 20900))
                    continue;

                if (m_WordsList[Unicode].m_List == null)
                    m_WordsList[Unicode].m_List = new ArrayList();
                m_WordsList[Unicode].m_List.Add(Word.Substring(1, Word.Length - 1));
            }
            sr.Close();
            WordSort();

            return true;
        }

        /// <summary>
        /// ���� sWord ����ǰ����Ĵ�(������ֿ�ͷ����Ĵ�)
        /// </summary>
        /// <param name="sWord"></param>
        /// <returns></returns>
        public string GetWordAtDictionary(string sWord)
        {
            /*
            if (!m_isOpen)
                return "";

            sWord = sWord.Trim();
            if (sWord.Length < 2)
                return "";
            */
            //Ϊ����߷ִʵ����ܣ���2���жϲ�Ҫ����Ϊ�ڷִʺ������涼�Ѿ������жϡ�����ʹ����Ҫע�⣬���ô˺���ǰ����֤�ʿ��Ѿ��򿪣����Ҵ����sWord�����ĳ�������Ϊ2
            string ch = sWord[0].ToString();
            ArrayList alWords = FindWordsList(ch);
            if (alWords == null)
                return "";

            sWord = sWord.Substring(1, sWord.Length - 1);
            for (int i = 0; i < alWords.Count; i++)
            {
                string sWordAtWords = alWords[i].ToString();
                int iLen = sWordAtWords.Length;
                if (iLen > sWord.Length)	//�ʱ��������ּ������������ּ������������϶����������
                    continue;
                if (sWord.Substring(0, iLen) == sWordAtWords)
                    return ch + sWordAtWords;
            }
            return "";
        }

        /// <summary>
        /// GetWordType
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public int GetWordType(char ch)	//�ַ������	-1 - ��Ӣ����ĸ�����������֣����֣�����+-*/�ȷ���(����������Χ֮��)
        //				0 - Ӣ����ĸ�����������֣�
        //				1 - ����!@#$%^&*()_-+=|\~`[]{}<>./�ȷ���
        //				10 - �������֣� 11 - �������ʣ� 12 - �������֣� 13 - ��ͨ����
        {
            string sWord = ch.ToString();
            int Unicode = (int)ch;
            string letters = "azAZ�������";

            if ((Unicode < letters[0] || Unicode > letters[1]) &&
                (Unicode < letters[2] || Unicode > letters[3]) &&
                (Unicode < letters[4] || Unicode > letters[5]) &&
                (Unicode < letters[6] || Unicode > letters[7]) &&
                "0123456789��������������������".IndexOf(sWord, StringComparison.Ordinal) < 0)
            {
                if ("!@#$%^&*()_-+=|\\~`[]{}<>./�ۣݣ�����������������������".IndexOf(sWord, StringComparison.Ordinal) >= 0)
                    return 1;

                if ((Unicode >= 19968) && (Unicode <= 40868))
                {
                    if ("��һ�������������߰˾�ʮҼ��������½��ƾ�ʰ���ǧ�����װ�Ǫئ����".IndexOf(sWord, StringComparison.Ordinal) >= 0)
                        return 10;
                    if ("Ԫ�Ƿ�ë��ʱ���������Ѻа���������ҳ���������������ֽ����Ǯ�����������ɳߴ�����ƿ��Ͱֻ֧������̨���������������ͷ�ݳ�λ����¥·������΢��Ƭ��˫ƥƱ�ѵ���յ���β����籭��̲���ȿ������������ŵ��Һ�����������Ⱥö�߿�֦�ܵ��ſ�������̽������������¼䴦������������׮�ڱ�ͨ����������ƪ�����ϯ���ͻضپ���ͦ�����������縳���������Ա������������������������»�����̥άƷ���ֻ������شԶ˶ѶӶԷ������������Ĩ��ǹ��Ȧȭ�������Ŵ�".IndexOf(sWord, StringComparison.Ordinal) >= 0)
                        return 11;
                    if ("�ͼ����ͬ�����Ѷ����ð�����".IndexOf(sWord, StringComparison.Ordinal) >= 0)
                        return 12;
                    return 13;
                }
                return -1;
            }

            return 0;
        }

        /// <summary>
        /// CalcWordMaxLen
        /// </summary>
        public void CalcWordMaxLen()
        {
            m_WordMaxLen = 0;
            int i, j;
            for (i = 0; i < 20901; i++)
            {
                if (m_WordsList[i].m_List != null)
                {
                    for (j = 0; j < m_WordsList[i].m_List.Count; j++)
                    {
                        if ((m_WordsList[i].m_List[j].ToString().Length + 1) > m_WordMaxLen)
                            m_WordMaxLen = m_WordsList[i].m_List[j].ToString().Length + 1;
                    }
                }
            }
        }
    }
}
