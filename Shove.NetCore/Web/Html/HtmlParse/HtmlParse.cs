using System;
using System.Collections;

namespace Shove.HTML.HtmlParse
{
	public class Attribute: ICloneable
	{
		private string m_name;
		private string m_value;
		private char m_delim;

		public Attribute(string name,string value,char delim)
		{
			m_name = name;
			m_value = value;
			m_delim = delim;
		}

		public Attribute():this("","",(char)0)
		{
		}


		public Attribute(string name,string value):this(name,value,(char)0)
		{
		}

		public char Delim
		{
			get 
			{
				return m_delim;
			}

			set 
			{
				m_delim = value;
			}
		}

		public string Name
		{
			get 
			{
				return m_name;
			}

			set 
			{
				m_name = value;
			}
		}

		public string Value
		{
			get 
			{
				return m_value;
			}

			set 
			{
				m_value = value;
			}
		}
		#region ICloneable Members
		public virtual object Clone()
		{
			return new Attribute(m_name,m_value,m_delim);		
		}
		#endregion
	}
			
	public class AttributeList:Attribute
	{
		protected ArrayList m_list;

		public override Object Clone()
		{
			AttributeList rtn = new AttributeList();			

			for ( int i=0;i<m_list.Count;i++ )
				rtn.Add( (Attribute)this[i].Clone() );

			return rtn;
		}

		public AttributeList():base("","")
		{
			m_list = new ArrayList();
		}

		public void Add(Attribute a)
		{
			m_list.Add(a);
		}

		public void Clear()
		{
			m_list.Clear();
		}

		public bool IsEmpty()
		{
			return( m_list.Count<=0);
		}

		public void Set(string name,string value)
		{
			if (name == null)
				return;
			if (value == null)
				value = "";

			Attribute a = this[name];

			if (a == null) 
			{
				a = new Attribute(name, value);
				Add(a);
			} 
			else
				a.Value = value;
		}

		public int Count
		{
			get 
			{
				return m_list.Count;
			}
		}

		public ArrayList List
		{
			get 
			{
				return m_list;
			}
		}

		public Attribute this[int index]
		{
			get 
			{
				if ( index < m_list.Count )
					return (Attribute)m_list[index];
				else
					return null;
			}
		}

		public Attribute this[string index]
		{
			get 
			{
				int i = 0;

				while (this[i] != null) 
				{
					if (this[i].Name.ToLower().Equals( (index.ToLower())))
						return this[i];
					i++;
				}
				return null;
			}
		}
	}

	public class Parse:AttributeList 
	{
		private string m_source;
		private int m_idx;
		private char m_parseDelim;
		private string m_parseName;
		private string m_parseValue;
		public string m_tag;

		public static bool IsWhiteSpace(char ch)
		{
			return( "\t\n\r ".IndexOf(ch) != -1 );
		}

		public void EatWhiteSpace()
		{
			while ( !Eof() ) 
			{
				if ( !IsWhiteSpace(GetCurrentChar()) )
					return;
				m_idx++;
			}
		}

		public bool Eof()
		{
			return(m_idx>=m_source.Length );
		}

		public void ParseAttributeName()
		{
			EatWhiteSpace();
			// get attribute name
			while ( !Eof() ) 
			{
				if ( IsWhiteSpace(GetCurrentChar()) ||
					(GetCurrentChar()=='=') ||
					(GetCurrentChar()=='>') )
					break;
				m_parseName+=GetCurrentChar();
				m_idx++;
			}

			EatWhiteSpace();
		}

		public void ParseAttributeValue()
		{
			if ( m_parseDelim!=0 )
				return;

			if ( GetCurrentChar()=='=' ) 
			{
				m_idx++;
				EatWhiteSpace();
				if ( (GetCurrentChar()=='\'') ||
					(GetCurrentChar()=='\"') ) 
				{
					m_parseDelim = GetCurrentChar();
					m_idx++;
					while ( GetCurrentChar()!=m_parseDelim ) 
					{
						m_parseValue+=GetCurrentChar();
						m_idx++;
					}
					m_idx++;
				} 
				else 
				{
					while ( !Eof() &&
						!IsWhiteSpace(GetCurrentChar()) &&
						(GetCurrentChar()!='>') ) 
					{
						m_parseValue+=GetCurrentChar();
						m_idx++;
					}
				}
				EatWhiteSpace();
			}
		}

		public void AddAttribute()
		{
			Attribute a = new Attribute(m_parseName,
				m_parseValue,m_parseDelim);
			Add(a);
		}

		public char GetCurrentChar()
		{
			return GetCurrentChar(0);
		}

		public char GetCurrentChar(int peek)
		{
			if( (m_idx+peek)<m_source.Length )
				return m_source[m_idx+peek];
			else
				return (char)0;
		}

		public char AdvanceCurrentChar()
		{
			return m_source[m_idx++];
		}

		public void Advance()
		{
			m_idx++;
		}

		public string ParseName
		{
			get 
			{
				return m_parseName;
			}

			set 
			{
				m_parseName = value;
			}
		}

		public string ParseValue
		{
			get 
			{
				return m_parseValue;
			}

			set 
			{
				m_parseValue = value;
			}
		}

		public char ParseDelim
		{
			get 
			{
				return m_parseDelim;
			}

			set 
			{
				m_parseDelim = value;
			}
		}

		public string Source
		{
			get 
			{
				return m_source;
			}

			set 
			{
				m_source = value;
			}
		}
	}

	public class ParseHTML:Parse 
	{
		public AttributeList GetTag()
		{
			AttributeList tag = new AttributeList();
			tag.Name = m_tag;

			foreach(Attribute x in List)
			{
				tag.Add((Attribute)x.Clone());
			}

			return tag;
		}

		public string BuildTag()
		{
			string buffer="<";
			buffer+=m_tag;
			int i=0;
			while ( this[i]!=null ) 
			{// has attributes
				buffer+=" ";
				if ( this[i].Value == null ) 
				{
					if ( this[i].Delim!=0 )
						buffer+=this[i].Delim;
					buffer+=this[i].Name;
					if ( this[i].Delim!=0 )
						buffer+=this[i].Delim;
				} 
				else 
				{
					buffer+=this[i].Name;
					if ( this[i].Value!=null ) 
					{
						buffer+="=";
						if ( this[i].Delim!=0 )
							buffer+=this[i].Delim;
						buffer+=this[i].Value;
						if ( this[i].Delim!=0 )
							buffer+=this[i].Delim;
					}
				}
				i++;
			}
			buffer+=">";
			return buffer;
		}

		protected void ParseTag()
		{
			m_tag="";
			Clear();

			// Is it a comment?
			if ( (GetCurrentChar()=='!') &&
				(GetCurrentChar(1)=='-')&&
				(GetCurrentChar(2)=='-') ) 
			{
				while ( !Eof() ) 
				{
					if ( (GetCurrentChar()=='-') &&
						(GetCurrentChar(1)=='-')&&
						(GetCurrentChar(2)=='>') )
						break;
					if ( GetCurrentChar()!='\r' )
						m_tag+=GetCurrentChar();
					Advance();
				}
				m_tag+="--";
				Advance();
				Advance();
				Advance();
				ParseDelim = (char)0;
				return;
			}

			// Find the tag name
			while ( !Eof() ) 
			{
				if ( IsWhiteSpace(GetCurrentChar()) || (GetCurrentChar()=='>') )
					break;
				m_tag+=GetCurrentChar();
				Advance();
			}

			EatWhiteSpace();

			// Get the attributes
			while ( GetCurrentChar()!='>' ) 
			{
				ParseName = "";
				ParseValue = "";
				ParseDelim = (char)0;

				ParseAttributeName();

				if ( GetCurrentChar()=='>' ) 
				{
					AddAttribute();
					break;
				}

				// Get the value(if any)
				ParseAttributeValue();
				AddAttribute();
			}
			Advance();
		}

		public char Parse()
		{
			if( GetCurrentChar()=='<' ) 
			{
				Advance();

				char ch=char.ToUpper(GetCurrentChar());
				if ( (ch>='A') && (ch<='Z') || (ch=='!') || (ch=='/') ) 
				{
					ParseTag();
					return (char)0;
				} 
				else return(AdvanceCurrentChar());
			} 
			else return(AdvanceCurrentChar());
		}
	}				
}