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
        public int m_FlagNum = 20901;	//UniCode 里面的 20901 个汉字
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
            //读词库
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

            //取最长的词的长度
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
        /// <param name="withFlag"></param>
        /// <param name="reSort"></param>
        /// <returns></returns>
        public ArrayList GetWordsListAll(bool withFlag, bool reSort)
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
                        if (withFlag)
                            al.Add(((char)(i + 19968)).ToString() + m_WordsList[i].m_List[j].ToString());
                        else
                            al.Add(m_WordsList[i].m_List[j]);
                    }
                }
            }
            if (reSort)
            {
                CompareToAscii compare = new CompareToAscii();
                al.Sort(compare);
            }
            return al;
        }

        /// <summary>
        /// AddWord
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public bool AddWord(string word)
        {
            if (!m_isOpen)
                return false;

            word = word.Trim();
            if (word.Length < 2)
                return false;

            int Unicode = (int)((char)word[0]) - 19968;
            if ((Unicode < 0) || (Unicode > 20900))
                return false;

            if (m_WordsList[Unicode].m_List == null)
                m_WordsList[Unicode].m_List = new ArrayList();
            m_WordsList[Unicode].m_List.Add(word.Substring(1, word.Length - 1));
            WordSort(Unicode);
            return true;
        }

        /// <summary>
        /// DeleteWord
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public bool DeleteWord(string word)
        {
            if (!m_isOpen)
                return false;

            word = word.Trim();
            if (word.Length < 2)
                return false;

            int Unicode = (int)((char)word[0]) - 19968;
            if ((Unicode < 0) || (Unicode > 20900))
                return false;

            if (m_WordsList[Unicode].m_List == null)
                return false;

            int i;
            word = word.Substring(1, word.Length - 1);
            for (i = 0; i < m_WordsList[Unicode].m_List.Count; i++)
            {
                if (m_WordsList[Unicode].m_List[i].ToString() == word)
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
        /// <param name="unicode"></param>
        /// <returns></returns>
        public bool WordSort(int unicode)
        {
            if (!m_isOpen)
                return false;
            if ((unicode < 0) || (unicode > 20900))
                return false;
            if (m_WordsList[unicode].m_List == null)
                return false;

            CompareToLength compare = new CompareToLength();
            m_WordsList[unicode].m_List.Sort(compare);
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
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool SaveToTxtFile(string fileName)
        {
            ArrayList al = GetWordsListAll(true, true);
            if (al == null)
                return false;

            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(fileName);
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
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool LoadFromTxtFile(string fileName)
        {
            StreamReader sr;
            try
            {
                sr = new StreamReader(fileName);
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
        /// 找与 sWord 串的前面最长的词(找这个字开头的最长的词)
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
            //为了提高分词的性能，这2句判断不要，因为在分词函数里面都已经做了判断。其他使用是要注意，调用此函数前，保证词库已经打开，而且传入的sWord参数的长度最少为2
            string ch = sWord[0].ToString();
            ArrayList alWords = FindWordsList(ch);
            if (alWords == null)
                return "";

            sWord = sWord.Substring(1, sWord.Length - 1);
            for (int i = 0; i < alWords.Count; i++)
            {
                string sWordAtWords = alWords[i].ToString();
                int iLen = sWordAtWords.Length;
                if (iLen > sWord.Length)	//词比这个这个字及后面所有文字加起来都长，肯定不是这个词
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
        public int GetWordType(char ch)	//字符号类别：	-1 - 非英文字母，阿拉伯数字，汉字，常用+-*/等符号(不在搜索范围之内)
        //				0 - 英文字母，阿拉伯数字，
        //				1 - 常用!@#$%^&*()_-+=|\~`[]{}<>./等符号
        //				10 - 汉字数字， 11 - 汉字量词， 12 - 汉字连字， 13 - 普通汉字
        {
            string sWord = ch.ToString();
            int Unicode = (int)ch;
            string letters = "azAZａｚＡＺ";

            if ((Unicode < letters[0] || Unicode > letters[1]) &&
                (Unicode < letters[2] || Unicode > letters[3]) &&
                (Unicode < letters[4] || Unicode > letters[5]) &&
                (Unicode < letters[6] || Unicode > letters[7]) &&
                "0123456789０１２３４５６７８９".IndexOf(sWord, StringComparison.Ordinal) < 0)
            {
                if ("!@#$%^&*()_-+=|\\~`[]{}<>./［］｛｝！·＃￥％…—＊（）".IndexOf(sWord, StringComparison.Ordinal) >= 0)
                    return 1;

                if ((Unicode >= 19968) && (Unicode <= 40868))
                {
                    if ("第一二两三四五六七八九十壹贰叁肆伍陆柒捌玖拾零百千万亿兆佰仟卅几整".IndexOf(sWord, StringComparison.Ordinal) >= 0)
                        return 10;
                    if ("元角分毛点时半刻秒岁个把盒包箱条件张页本块根套面幅付副吨斤克两钱磅米厘听毫丈尺寸里卷打瓶罐桶只支袋部次台辆年月日天号周折头份成位名种楼路层项级封度微升片粒双匹票盅碟碗盏方滴册箩坨杯口滩笼扇筐簸串吊挂捆团担家壶轮令栋发株沓窝群枚具棵枝管道颗款朵缕堂盘节贴剂服座幢堵间处所架艘趟爿手桩宗笔通场记喷则首篇门尊股席倍餐回顿句泓挺惯身叠锅束地绺出集段曲波员池声刀帘代边组届类批步等章环列期胎维品更局户季帮遍簇丛端堆队对番格伙栏篮例流抹排枪区圈拳提招桌着大".IndexOf(sWord, StringComparison.Ordinal) >= 0)
                        return 11;
                    if ("和及与跟同或最已而等用把上来".IndexOf(sWord, StringComparison.Ordinal) >= 0)
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
