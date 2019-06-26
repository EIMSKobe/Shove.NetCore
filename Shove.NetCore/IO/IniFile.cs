using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Shove.IO
{
    /// <summary>
    /// IniFile 的摘要说明。
    /// </summary>
    public class IniFile
    {
        #region "Declarations"

        // *** Lock for thread-safe access to file and local cache ***
        private object m_Lock = new object();

        // *** File name ***
        private string m_FileName = null;

        /// <summary>
        /// Ini 文件名
        /// </summary>
        public string FileName
        {
            get
            {
                return m_FileName;
            }
        }

        // *** Lazy loading flag ***
        private bool m_Lazy = false;

        // *** Local cache ***
        private Dictionary<string, Dictionary<string, string>> m_Sections = new Dictionary<string, Dictionary<string, string>>();

        // *** Local cache modified flag ***
        private bool m_CacheModified = false;

        #endregion

        #region "Methods"

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="FileName"></param>
        public IniFile(string FileName)
        {
            Initialize(FileName, false);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="Lazy"></param>
        public IniFile(string FileName, bool Lazy)
        {
            Initialize(FileName, Lazy);
        }

        // *** Initialization ***
        private void Initialize(string FileName, bool Lazy)
        {
            m_FileName = FileName;
            m_Lazy = Lazy;
            if (!m_Lazy) Refresh();
        }

        // *** Read file contents into local cache ***
        private void Refresh()
        {
            lock (m_Lock)
            {
                StreamReader sr = null;
                try
                {
                    // *** Clear local cache ***
                    m_Sections.Clear();

                    // *** Open the INI file ***
                    try
                    {
                        sr = new StreamReader(m_FileName);
                    }
                    catch (FileNotFoundException)
                    {
                        return;
                    }

                    // *** Read up the file content ***
                    Dictionary<string, string> CurrentSection = null;
                    string s;
                    while ((s = sr.ReadLine()) != null)
                    {
                        s = s.Trim();

                        // *** Check for section names ***
                        if (s.StartsWith("[") && s.EndsWith("]"))
                        {
                            if (s.Length > 2)
                            {
                                string SectionName = s.Substring(1, s.Length - 2);

                                // *** Only first occurrence of a section is loaded ***
                                if (m_Sections.ContainsKey(SectionName))
                                {
                                    CurrentSection = null;
                                }
                                else
                                {
                                    CurrentSection = new Dictionary<string, string>();
                                    m_Sections.Add(SectionName, CurrentSection);
                                }
                            }
                        }
                        else if (CurrentSection != null)
                        {
                            // *** Check for key+value pair ***
                            int i;
                            if ((i = s.IndexOf('=')) > 0)
                            {
                                int j = s.Length - i - 1;
                                string Key = s.Substring(0, i).Trim();
                                if (Key.Length > 0)
                                {
                                    // *** Only first occurrence of a key is loaded ***
                                    if (!CurrentSection.ContainsKey(Key))
                                    {
                                        string Value = (j > 0) ? (s.Substring(i + 1, j).Trim()) : ("");
                                        CurrentSection.Add(Key, Value);
                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {
                    // *** Cleanup: close file ***
                    if (sr != null) sr.Close();
                    sr = null;
                }
            }
        }

        // *** Flush local cache content ***
        private void Flush()
        {
            lock (m_Lock)
            {
                // *** If local cache was not modified, exit ***
                if (!m_CacheModified) return;
                m_CacheModified = false;

                // *** Open the file ***
                StreamWriter sw = new StreamWriter(m_FileName);

                try
                {
                    // *** Cycle on all sections ***
                    bool First = false;
                    foreach (KeyValuePair<string, Dictionary<string, string>> SectionPair in m_Sections)
                    {
                        Dictionary<string, string> Section = SectionPair.Value;
                        if (First) sw.WriteLine();
                        First = true;

                        // *** Write the section name ***
                        sw.Write('[');
                        sw.Write(SectionPair.Key);
                        sw.WriteLine(']');

                        // *** Cycle on all key+value pairs in the section ***
                        foreach (KeyValuePair<string, string> ValuePair in Section)
                        {
                            // *** Write the key+value pair ***
                            sw.Write(ValuePair.Key);
                            sw.Write('=');
                            sw.WriteLine(ValuePair.Value);
                        }
                    }
                }
                finally
                {
                    // *** Cleanup: close file ***
                    if (sw != null) sw.Close();
                    sw = null;
                }
            }
        }

        /// <summary>
        /// Read a value from local cache
        /// </summary>
        /// <param name="SectionName"></param>
        /// <param name="Key"></param>
        /// <returns></returns>
        public string Read(string SectionName, string Key)
        {
            // *** Lazy loading ***
            if (m_Lazy)
            {
                m_Lazy = false;
                Refresh();
            }

            lock (m_Lock)
            {
                // *** Check if the section exists ***
                Dictionary<string, string> Section;
                if (!m_Sections.TryGetValue(SectionName, out Section)) return "";

                // *** Check if the key exists ***
                string Value;
                if (!Section.TryGetValue(Key, out Value)) return "";

                // *** Return the found value ***
                return Value;
            }
        }

        /// <summary>
        /// Insert or modify a value in local cache, And Flush.
        /// </summary>
        /// <param name="SectionName"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public bool Write(string SectionName, string Key, object Value)
        {
            // *** Lazy loading ***
            if (m_Lazy)
            {
                m_Lazy = false;
                Refresh();
            }

            lock (m_Lock)
            {
                // *** Flag local cache modification ***
                m_CacheModified = true;

                // *** Check if the section exists ***
                Dictionary<string, string> Section;
                if (!m_Sections.TryGetValue(SectionName, out Section))
                {
                    // *** If it doesn't, add it ***
                    Section = new Dictionary<string, string>();
                    m_Sections.Add(SectionName, Section);
                }

                // *** Modify the value ***
                if (Section.ContainsKey(Key)) Section.Remove(Key);
                Section.Add(Key, System.Convert.ToString(Value));
            }

            Flush();
            return true;
        }

        // *** Encode byte array ***
        private string EncodeByteArray(byte[] Value)
        {
            if (Value == null) return null;

            StringBuilder sb = new StringBuilder();
            foreach (byte b in Value)
            {
                string hex = System.Convert.ToString(b, 16);
                int l = hex.Length;
                if (l > 2)
                {
                    sb.Append(hex.Substring(l - 2, 2));
                }
                else
                {
                    if (l < 2) sb.Append("0");
                    sb.Append(hex);
                }
            }
            return sb.ToString();
        }

        // *** Decode byte array ***
        private byte[] DecodeByteArray(string Value)
        {
            if (Value == null) return null;

            int l = Value.Length;
            if (l < 2) return new byte[] { };

            l /= 2;
            byte[] Result = new byte[l];
            for (int i = 0; i < l; i++) Result[i] = System.Convert.ToByte(Value.Substring(i * 2, 2), 16);
            return Result;
        }

        #endregion
    }
}