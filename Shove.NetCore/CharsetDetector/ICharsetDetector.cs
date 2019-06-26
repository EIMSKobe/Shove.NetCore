/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 2006-12-16
 * Time: 2:15
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;

namespace Shove.CharsetDetector
{
	/// <summary>
	/// Description of ICharsetDetector.
	/// </summary>
	public interface ICharsetDetector
	{
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        void Init(ICharsetDetectionObserver observer);
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        bool DoIt(byte[] aBuf, int aLen, bool oDontFeedMe);
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        void Done();
	}
}
