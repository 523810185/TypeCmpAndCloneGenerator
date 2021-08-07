using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ILUtility
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

    public static class ILGeneratorExtension
    {
        // Debug.Log
        private static MethodInfo m_stUnityDebugLogMF = typeof(UnityEngine.Debug).GetMethod("Log", new Type[] {typeof(string)});
        private static MethodInfo m_stUnityDebugLogErrorMF = typeof(UnityEngine.Debug).GetMethod("LogError", new Type[] {typeof(string)});
        public static ILGenerator GenUnityLog(this ILGenerator il, string logstr)
        {
            il.Emit(OpCodes.Ldstr, logstr);
            il.Emit(OpCodes.Call, m_stUnityDebugLogMF);
            return il;
        }

        public static ILGenerator GenUnityError(this ILGenerator il, string logstr)
        {
            il.Emit(OpCodes.Ldstr, logstr);
            il.Emit(OpCodes.Call, m_stUnityDebugLogErrorMF);
            return il;
        }

        /// <summary>
        /// 上面要接入上下文，没用过，暂时不知道能不能用
        /// </summary>
        /// <param name="il"></param>
        /// <returns></returns>
        public static ILGenerator GenUnityLog(this ILGenerator il)
        {
            il.Emit(OpCodes.Call, m_stUnityDebugLogMF);
            return il;
        }

        /// <summary>
        /// 生成一个For循环
        /// </summary>
        /// <param name="il"></param>
        /// <param name="loopCntFc">需要把loopCnt生成好并放在IL栈上</param>
        /// <param name="forBodyFc"></param>
        /// <param name="localVarInt"></param>
        /// <returns></returns>
        public static ILGenerator GenFor(this ILGenerator il, Action loopCntFc, Action<int> forBodyFc, ref int localVarInt) 
        {
            // 变量
            var idLoopCnt = localVarInt++;
            il.DeclareLocal(typeof(int));
            var idLoopIter = localVarInt++;
            il.DeclareLocal(typeof(int));
            // 标签
            var innerIIsLessCntLabel = il.DefineLabel();
            var innerForLabel = il.DefineLabel();

            // il.Emit(OpCodes.Ldloc, idList0Cnt);
            // il.Emit(OpCodes.Ldloc, idList1Cnt);
            // il.Emit(OpCodes.Sub);
            loopCntFc();
            il.Emit(OpCodes.Stloc, idLoopCnt);
            // i = 0
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, idLoopIter);
            il.Emit(OpCodes.Br, innerIIsLessCntLabel);

            // for
            il.MarkLabel(innerForLabel);
            {
                // il.Emit(OpCodes.Ldloc, idList0);
                // il.Emit(OpCodes.Ldloc, idList0);
                // il.Emit(OpCodes.Callvirt, listGetCountMethod);
                // il.Emit(OpCodes.Ldc_I4_1);
                // il.Emit(OpCodes.Sub);
                // il.Emit(OpCodes.Callvirt, listRemoveAtMethod);
                forBodyFc(idLoopIter);
            }

            // i++
            {
                il.Emit(OpCodes.Ldloc, idLoopIter);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stloc, idLoopIter);
            }

            // i < cnt
            il.MarkLabel(innerIIsLessCntLabel);
            {
                il.Emit(OpCodes.Ldloc, idLoopIter);
                il.Emit(OpCodes.Ldloc, idLoopCnt);
                il.Emit(OpCodes.Clt);
                il.Emit(OpCodes.Brtrue, innerForLabel);
            }
            return il;
        }
    }
}
