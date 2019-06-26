/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 2006-12-16
 * Time: 2:49
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;

namespace Shove.CharsetDetector
{
	/// <summary>
	/// Description of EUCStatistics.
	/// </summary>
	public abstract class EUCStatistics
	{
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
		public abstract float[] mFirstByteFreq() ;
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
     	public abstract float   mFirstByteStdDev();
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
     	public abstract float   mFirstByteMean();
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
     	public abstract float   mFirstByteWeight();
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
     	public abstract float[] mSecondByteFreq();
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
     	public abstract float   mSecondByteStdDev();
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
     	public abstract float   mSecondByteMean();
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
     	public abstract float   mSecondByteWeight();
     	/// <summary>
     	/// 
     	/// </summary>
		public EUCStatistics()
		{
		}
	}
}
