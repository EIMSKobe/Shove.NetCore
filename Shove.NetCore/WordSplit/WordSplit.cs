using System;
using System.Collections;

namespace Shove.WordSplit
{
    /// <summary>
    /// 中文分词
    /// </summary>
    public class ShoveWordSplit
    {
        /// <summary>
        /// 词典库
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
        /// 构造、装载词库
        /// </summary>
        /// <param name="chineseDictionaryFileName">绝对路径文件名</param>
        public ShoveWordSplit(string chineseDictionaryFileName)
        {
            if (chineseDictionaryFileName.Trim() == "")
                m_Dict = new Dictionary();
            else
                m_Dict = new Dictionary(chineseDictionaryFileName);
        }

        /*
        分词使用方法：
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
        /// 分词
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
            int iLocate = 0; //遍历原串
            int i;
            while (iLocate < sSource.Length)
            {
                string sWords = sSource.Substring(iLocate, 1); //词
                string sWord = sSource.Substring(iLocate, 1); //字

                int iWordType = m_Dict.GetWordType(sWord[0]);	//字符号类别：	-1 - 非英文字母，阿拉伯数字，汉字，常用!@#$%^&*()_-+=|\~`[]{}<>./等符号(不在搜索范围之内)
                //				0 - 英文字母，阿拉伯数字，常用!@#$%^&*()_-+=|\~`[]{}<>./等符号
                //				10 - 汉字数字， 11 - 汉字量词， 12 - 汉字连字， 13 - 普通汉字

                if (iWordType < 0)
                {
                    iLocate++;
                    continue;
                }

                if ((iWordType == 0) || (iWordType == 1)) //如果是字母、阿拉伯数字、常用!@#$%^&*()_-+=|\~`[]{}<>./等符号，继续取后面的相连，直到不是这些为止。可以拆分 MP3, MP4, P3, 256M, PIII,C++,c# 等..
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

                if (iWordType >= 10) //如果是汉字
                {
                    int Len = sSource.Length - iLocate;
                    if (Len > m_Dict.m_WordMaxLen) Len = m_Dict.m_WordMaxLen;
                    string sFoundWord = m_Dict.GetWordAtDictionary(sSource.Substring(iLocate, Len));
                    if (sFoundWord != "")	//是词，添加，返回。再继续找后面
                    {
                        al.Add(sFoundWord);
                        iLocate += sFoundWord.Length;
                        continue;
                    }
                    else	//不是词的话，再做2种情况：1、是否是数字、量词 2、前面一个词是否可以减后面1个字依然是词，剩单字就看是否连词(和及与之类或数字量词)，减的字再和后面能否是词。
                    {
                        if ((iWordType == 10) || (iWordType == 11))	//如果是中文数字、量词，不断取到后面不是中文数字或量词
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

                        bool CanSubsidy = false;	//分析能否前面词补贴
                        if (al.Count > 0)	//前面有词，才能分析能否补贴
                        {
                            string PreWords = al[al.Count - 1].ToString();
                            if (PreWords.Length > 1)	//前面的词最少2个子，才能分析能否补贴
                            {
                                if (m_Dict.GetWordType(PreWords[PreWords.Length - 1]) >= 10)	//上一个词的最后是汉字，才能分析能否补贴
                                {
                                    string PreWord = PreWords.Substring(PreWords.Length - 1, 1);	//取出补贴的字
                                    PreWords = PreWords.Substring(0, PreWords.Length - 1);			//去掉补贴字后的上一个词

                                    if (PreWords.Length == 1)	//如果补贴后只剩一个字，如果不是中文数字、量词或连词，不能补贴
                                    {
                                        int PreWordType = m_Dict.GetWordType(PreWords[0]);
                                        if ((PreWordType == 10) || (PreWordType == 11) || (PreWordType == 12))
                                            CanSubsidy = true;
                                    }
                                    else
                                    {
                                        if (m_Dict.WordExist(PreWords))	//如果补贴后仍是词
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
                        if (CanSubsidy)	//能够补贴的话，看补贴后，后面能否成词
                        {
                            string sFoundWordNext = "";
                            Len = sSource.Length - iLocate + 1;
                            if (Len > m_Dict.m_WordMaxLen) Len = m_Dict.m_WordMaxLen;
                            sFoundWordNext = m_Dict.GetWordAtDictionary(sSource.Substring(iLocate - 1, Len));
                            if (sFoundWordNext != "")	//补贴后可以为词，则补贴结束。
                            {
                                string PreWords = al[al.Count - 1].ToString();
                                al[al.Count - 1] = PreWords.Substring(0, PreWords.Length - 1);
                                al.Add(sFoundWordNext);
                                iLocate += sFoundWordNext.Length - 1;
                                continue;
                            }
                        }
                        //不能补贴或不需要补贴
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
