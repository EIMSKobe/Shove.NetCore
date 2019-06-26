/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 2006-12-16
 * Time: 3:08
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;

namespace Shove.CharsetDetector
{
	/// <summary>
	/// Description of Detector.
	/// </summary>
	public class Detector : PSMDetector,ICharsetDetector
	{
		ICharsetDetectionObserver mObserver = null ;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Detector()
            : base() 
        {
		}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Detector(int langFlag)
            : base(langFlag) 
        {
		}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public void Init(ICharsetDetectionObserver aObserver)
        {
		  	mObserver = aObserver ;
			return ;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool DoIt(byte[] aBuf, int aLen, bool oDontFeedMe)
        {
			if (aBuf == null || oDontFeedMe )
			    return false ;
	
			this.HandleData(aBuf, aLen) ;	
			return mDone ;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public void Done()
        {
			this.DataEnd() ;
			return ;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override void Report(string charset)
        {
			if (mObserver != null)
			    mObserver.Notify(charset)  ;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool isAscii(byte[] aBuf, int aLen)
        {
	                for(int i=0; i<aLen; i++) {
	                   if ((0x0080 & aBuf[i]) != 0) {
	                      return false ;
	                   }
	                }
			return true ;
		}
	}
}
