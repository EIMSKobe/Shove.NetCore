/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 2006-12-16
 * Time: 1:05
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;

namespace Shove.CharsetDetector
{
	/// <summary>
	/// Description of Verifier.
	/// </summary>
	public abstract class Verifier
	{
		internal static readonly byte eStart = (byte)0;
		internal static readonly byte eError = (byte)1;
		internal static readonly byte eItsMe = (byte)2;
		internal static readonly int eidxSft4bits = 3;
	    internal static readonly int eSftMsk4bits = 7;
     	internal static readonly int eBitSft4bits = 2;
     	internal static readonly int eUnitMsk4bits = 0x0000000F;
     	
        /// <summary>
        /// 
        /// </summary>
		public Verifier()
		{
		}
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
		public abstract string charset();
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
    	public abstract int stFactor();
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
     	public abstract int[] cclass();
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
     	public abstract int[] states();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
     	public abstract bool isUCS2();
     
        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <param name="b"></param>
        /// <param name="s"></param>
        /// <returns></returns>
     	public static byte getNextState(Verifier v, byte b, byte s) {

         return (byte) ( 0xFF & 
	     (((v.states()[((
		   (s*v.stFactor()+(((v.cclass()[((b&0xFF)>>eidxSft4bits)]) 
		   >> ((b & eSftMsk4bits) << eBitSft4bits)) 
		   & eUnitMsk4bits ))&0xFF)
		>> eidxSft4bits) ]) >> (((
		   (s*v.stFactor()+(((v.cclass()[((b&0xFF)>>eidxSft4bits)]) 
		   >> ((b & eSftMsk4bits) << eBitSft4bits)) 
		   & eUnitMsk4bits ))&0xFF) 
		& eSftMsk4bits) << eBitSft4bits)) & eUnitMsk4bits )
	 	) ;

     	}
	}
}
