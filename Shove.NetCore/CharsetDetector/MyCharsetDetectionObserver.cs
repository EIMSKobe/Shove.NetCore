/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 2007-1-27
 * Time: 22:34
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;

namespace Shove.CharsetDetector
{
	/// <summary>
	/// Description of MyCharsetDetectionObserver.
	/// </summary>
	public class MyCharsetDetectionObserver : ICharsetDetectionObserver
	{
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string Charset = null;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public void Notify(string charset)
		{
			Charset = charset;
		}
	}
}
