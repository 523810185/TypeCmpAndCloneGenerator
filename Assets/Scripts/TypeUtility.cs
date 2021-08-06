﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace W3.TypeExtension
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using ILUtility;

    public static class TypeExtension
    {
        // TODO.. 参照一下Odin
        private static List<Type> BASIC_TYPE_LIST = new List<Type>()
        {
            typeof(float),
            typeof(double),
            typeof(sbyte),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(byte),
            typeof(ushort),
            typeof(uint),
            typeof(ulong),
            typeof(decimal),
            typeof(char),
            typeof(bool),
        };

        /// <summary>
        /// 返回是否是基本类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsBasicType(this Type type)
        {
            return BASIC_TYPE_LIST.Contains(type);
        }

        /// <summary>
        /// 返回是否是Unity的类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsUnityType(this Type type) 
        {
            return typeof(UnityEngine.Object).IsAssignableFrom(type);
        }

        /// <summary>
        /// 返回是否是数组类型。
        /// 注意：最好以后改成Odin的ImplementsOpenGenericInterface方法；否则一个实现了List<T>的类型会返回false；
        /// 另外这个方法不支持高维数组
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsList(this Type type) 
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) 
                || type.IsArray;
        }

        /// <summary>
        /// 返回 这个类型参数为 T Equals(T) 的方法
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static MethodInfo GetCurTypeEqualsMethodInfo(this Type type)
        {
            // 注意：这里需要加上 DeclaredOnly 来保证只在本类中找
            // 不知道为什么，如果这里不加会自动去找父类，但是 "op_Equality" 不加默认不会去找父类，可能是静态或者重载操作符的关系？
            return type.GetMethod("Equals", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, new Type[]{type}, null);
        }

        /// <summary>
        /// 返回当前Type "op_Equality" 方法，如果本类中没有，去父类中找
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static MethodInfo GetCurTypeOpEqualMethodInfoIncludeParent(this Type type) 
        {
            // 注意：这里 BindingFlags 不能加 DeclaredOnly，否则只会找本类
            // 同时，需要 FlattenHierarchy 去找父类的方法
            return type.GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy, null, new Type[]{type, type}, null);
        }

        /// <summary>
        /// 返回一个数组类型的元素类型。
        /// 注意：可能并不支持高维数组。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type GetListElementType(this Type type) 
        {
            if(!type.IsList()) 
            {
                Debug.LogErrorFormat(" GetListElementType 中传入了一个不是数组类型的type：{0}", type);
            }
            else 
            {
                if(type.IsArray) 
                {
                    return type.GetElementType();
                }
                else 
                {
                    foreach (var item in type.GetGenericArguments())
                    {
                        return item;
                    }
                }
            }

            return null;
        }
    }

    public static class TypeUtility
    {
        private struct ILCtxItem 
        {
            public OpCode opCodes;
            public FieldInfo fi;
            public MethodInfo mi, miex;
            public int varID0, varID1;
        }
        private static Dictionary<Type, object> m_mapTypeCmpCache = new Dictionary<Type, object>();
        /// <summary>
        /// TODO.. class null的时候处理
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Func<T, T, bool> GetTypeCmp<T>()
        {
            Type type = typeof(T);
            object cmpObj = null;
            if(m_mapTypeCmpCache.TryGetValue(type, out cmpObj)) 
            {
                return cmpObj as Func<T, T, bool>;
            }

            // // TODO.. 如果是基本类型或者string或者Unity类型，应该直接返回一个Action即可，不需要IL
            // if(type.IsBasicType() || type == typeof(string) || type.IsUnityType())
            // {
            //     Debug.LogErrorFormat("暂不支持这种类型的比较器，请直接比较，Type = {0}", type);
            //     return null;
            // }

            var dm = new DynamicMethod("", typeof(bool), new Type[]{type, type});
            var il = dm.GetILGenerator();
            Label lbFalse = il.DefineLabel();
            Label lbRet = il.DefineLabel();
            LocalBuilder localBool = il.DeclareLocal(typeof(bool)); // For ans
            int localVarInt = 1; // 记录局部变量使用的id
            // debug

            /// <summary>
            /// 加载参数0
            /// </summary>
            void LoadParm0()
            {
                il.Emit(OpCodes.Ldarg_0);
            }
            /// <summary>
            /// 加载参数1
            /// </summary>
            void LoadParm1()
            {
                il.Emit(OpCodes.Ldarg_1);
            }
            void RecursiveLoadParm0(List<ILCtxItem> ilCtxList)
            {
                if(ilCtxList != null) 
                {
                    Debug.Log(" ------------- 解析参数0 start");
                    // 加载参数0，并获取对应field
                    // LoadParm0();
                    // foreach (var item in ilCtxList)
                    // {
                    //     if(item.opCodes == OpCodes.Ldfld) 
                    //     {
                    //         Debug.Log("Ldfld, " + item.fi.Name);
                    //         il.Emit(OpCodes.Ldfld, item.fi); 
                    //     }
                    //     else if(item.opCodes == OpCodes.Ldloc) 
                    //     {
                    //         Debug.Log("Ldloc, " + item.varID0);
                    //         il.Emit(item.opCodes, item.varID0);
                    //     }
                    //     else 
                    //     {
                    //         Debug.LogErrorFormat("RecursiveLoadParm0 上下文item中混入了 无法解析的OpCodes：{0}", item.opCodes);
                    //     }
                    // }
                    int lastLoadLocInt = -1;
                    for(int i=ilCtxList.Count-1;i>=0;i--) 
                    {
                        if(ilCtxList[i].opCodes == OpCodes.Ldloc) 
                        {
                            lastLoadLocInt = i;
                            break;
                        }
                    }
                    if(lastLoadLocInt == -1) 
                    {
                        LoadParm0();
                        foreach (var item in ilCtxList)
                        {
                            if(item.opCodes == OpCodes.Ldfld) 
                            {
                                Debug.Log("Ldfld, " + item.fi.Name);
                                il.Emit(OpCodes.Ldfld, item.fi); 
                            }
                            else 
                            {
                                Debug.LogErrorFormat("RecursiveLoadParm0 上下文item中混入了 无法解析的OpCodes：{0}", item.opCodes);
                            }
                        }
                    }
                    else 
                    {
                        for(int i=lastLoadLocInt;i<ilCtxList.Count;i++)
                        {
                            var item = ilCtxList[i];
                            if(item.opCodes == OpCodes.Ldfld) 
                            {
                                Debug.Log("Ldfld, " + item.fi.Name);
                                il.Emit(OpCodes.Ldfld, item.fi); 
                            }
                            else if(item.opCodes == OpCodes.Ldloc) 
                            {
                                Debug.Log("Ldloc, " + item.varID0);
                                il.Emit(item.opCodes, item.varID0);
                            }
                            else 
                            {
                                Debug.LogErrorFormat("RecursiveLoadParm0 上下文item中混入了 无法解析的OpCodes：{0}", item.opCodes);
                            }
                        }
                    }
                    Debug.Log(" ------------- 解析参数0 end");
                }
            }
            void RecursiveLoadParm1(List<ILCtxItem> ilCtxList)
            {
                if(ilCtxList != null) 
                {
                    // 加载参数1，并获取对应field
                    // LoadParm1();
                    // foreach (var item in ilCtxList)
                    // {
                    //     if(item.opCodes == OpCodes.Ldfld) 
                    //     {
                    //         il.Emit(OpCodes.Ldfld, item.fi); 
                    //     }
                    //     else if(item.opCodes == OpCodes.Ldloc) 
                    //     {
                    //         il.Emit(item.opCodes, item.varID1);
                    //     }
                    //     else 
                    //     {
                    //         Debug.LogErrorFormat("RecursiveLoadParm1 上下文item中混入了 无法解析的OpCodes：{0}", item.opCodes);
                    //     }
                    // }
                    int lastLoadLocInt = -1;
                    for(int i=ilCtxList.Count-1;i>=0;i--) 
                    {
                        if(ilCtxList[i].opCodes == OpCodes.Ldloc) 
                        {
                            lastLoadLocInt = i;
                            break;
                        }
                    }
                    if(lastLoadLocInt == -1) 
                    {
                        LoadParm1();
                        foreach (var item in ilCtxList)
                        {
                            if(item.opCodes == OpCodes.Ldfld) 
                            {
                                il.Emit(OpCodes.Ldfld, item.fi); 
                            }
                            else 
                            {
                                Debug.LogErrorFormat("RecursiveLoadParm0 上下文item中混入了 无法解析的OpCodes：{0}", item.opCodes);
                            }
                        }
                    }
                    else 
                    {
                        for(int i=lastLoadLocInt;i<ilCtxList.Count;i++)
                        {
                            var item = ilCtxList[i];
                            if(item.opCodes == OpCodes.Ldfld) 
                            {
                                il.Emit(OpCodes.Ldfld, item.fi); 
                            }
                            else if(item.opCodes == OpCodes.Ldloc) 
                            {
                                il.Emit(item.opCodes, item.varID1);
                            }
                            else 
                            {
                                Debug.LogErrorFormat("RecursiveLoadParm0 上下文item中混入了 无法解析的OpCodes：{0}", item.opCodes);
                            }
                        }
                    }
                }
            }
            /// <summary>
            /// 比较某一个field，这个field是基本类型的
            /// </summary>
            void GenerateBasicType(List<ILCtxItem> ilCtxList)
            {
                // 加载参数0，并获取对应field
                RecursiveLoadParm0(ilCtxList);
                // 加载参数1，并获取对应field
                RecursiveLoadParm1(ilCtxList);
                // 作比较
                il.Emit(OpCodes.Ceq);
                // 如果不同，则跳到lbFalse
                il.Emit(OpCodes.Brfalse, lbFalse);
            }

            void GenerateHaveOpEqualType(Type nowType, List<ILCtxItem> ilCtxList)
            {
                // 加载参数0，并获取对应field
                RecursiveLoadParm0(ilCtxList);
                // 加载参数1，并获取对应field
                RecursiveLoadParm1(ilCtxList);
                // 获取比较函数
                var opMethod = nowType.GetCurTypeOpEqualMethodInfoIncludeParent();
                il.Emit(OpCodes.Call, opMethod);
                // 比较结果和true作比较
                il.Emit(OpCodes.Ldc_I4_1);
                // 作比较
                il.Emit(OpCodes.Ceq);
                // 如果不同，则跳到lbFalse
                il.Emit(OpCodes.Brfalse, lbFalse);
            }

            void GenerateCanEqualType(Type nowType, List<ILCtxItem> ilCtxList)
            {
                // 加载参数0，并获取对应field
                RecursiveLoadParm0(ilCtxList);
                // 加载参数1，并获取对应field
                RecursiveLoadParm1(ilCtxList);
                // 获取比较函数
                var opMethod = nowType.GetCurTypeEqualsMethodInfo();
                il.Emit(OpCodes.Callvirt, opMethod);
                // 比较结果和true作比较
                il.Emit(OpCodes.Ldc_I4_1);
                // 作比较
                il.Emit(OpCodes.Ceq);
                // 如果不同，则跳到lbFalse
                il.Emit(OpCodes.Brfalse, lbFalse);
            }

            /// <summary>
            /// 比较一个class类型的field
            /// </summary>
            void GenerateClass(Type nowType, List<ILCtxItem> ilCtxList) 
            {
                foreach (var field in nowType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    // Debug.Log(nowFi.Name + " 中的 " + field.Name);
                    var ilCtxItem = new ILCtxItem(); ilCtxItem.opCodes = OpCodes.Ldfld; ilCtxItem.fi = field;
                    ilCtxList.Add(ilCtxItem);
                    GenerateField(field.FieldType, ilCtxList);
                    ilCtxList.RemoveAt(ilCtxList.Count - 1);
                }
            }

            void GenerateList(Type listType, List<ILCtxItem> ilCtxList) 
            {
                var itemType = listType.GetListElementType();
                if(itemType == null) 
                {
                    Debug.LogErrorFormat(" GetTypeCmp 的 GenerateList 中传入了一个无法生成的List类型：{0}", listType);
                    return;
                }
                
                // 定义变量
                var idList0 = localVarInt++;
                il.DeclareLocal(listType);
                var idList1 = localVarInt++;
                il.DeclareLocal(listType);
                var idListNullCnt = localVarInt++;
                il.DeclareLocal(typeof(int));
                var idList0Cnt = localVarInt++;
                il.DeclareLocal(typeof(int));
                var idList1Cnt = localVarInt++;
                il.DeclareLocal(typeof(int));
                var idForI = localVarInt++;
                il.DeclareLocal(typeof(int));
                var idItem0 = localVarInt++;
                il.DeclareLocal(itemType);
                var idItem1 = localVarInt++;
                il.DeclareLocal(itemType);
                // 定义标签
                var endLabel = il.DefineLabel();
                var cmpSecondListNullLabel = il.DefineLabel();
                var endFinishCmpListNullLabel = il.DefineLabel();
                var cmpItemLabel = il.DefineLabel();
                var iPPLabel = il.DefineLabel(); // for 的 i++
                var iIsLessCntLabel = il.DefineLabel(); // for 的 i < cnt

                // 初始化变量
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Stloc, idListNullCnt);

                // 加载参数0，并获取对应field
                RecursiveLoadParm0(ilCtxList);
                il.Emit(OpCodes.Stloc, idList0);
                // 加载参数1，并获取对应field
                RecursiveLoadParm1(ilCtxList);
                il.Emit(OpCodes.Stloc, idList1);

                // 比较是否是null
                // 比较第一个List是否为null
                il.Emit(OpCodes.Ldloc, idList0);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brfalse, cmpSecondListNullLabel);

                // 为list null计数器+1
                il.Emit(OpCodes.Ldloc, idListNullCnt);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stloc, idListNullCnt);

                // 比较第二个List是否为null
                il.MarkLabel(cmpSecondListNullLabel);
                il.Emit(OpCodes.Ldloc, idList1);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brfalse, endFinishCmpListNullLabel);

                // 为list null计数器+1
                il.Emit(OpCodes.Ldloc, idListNullCnt);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stloc, idListNullCnt);

                // 结束比较List是否为null
                il.MarkLabel(endFinishCmpListNullLabel);
                il.Emit(OpCodes.Ldloc, idListNullCnt);
                il.Emit(OpCodes.Ldc_I4_2);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brtrue, endLabel); // 如果都为null，就表示相同，直接结束了比较List

                // 如果恰好只有一个为null，那么说明不同，直接结束整个比较
                il.Emit(OpCodes.Ldloc, idListNullCnt);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brtrue, lbFalse);

                // 到这里说明两个List都不为null
                // 开始比较列表个数是否相同
                var listGetCountMethod = listType.GetMethod("get_Count");

                il.Emit(OpCodes.Ldloc, idList0);
                il.Emit(OpCodes.Callvirt, listGetCountMethod);
                il.Emit(OpCodes.Stloc, idList0Cnt);

                il.Emit(OpCodes.Ldloc, idList1);
                il.Emit(OpCodes.Callvirt, listGetCountMethod);
                il.Emit(OpCodes.Stloc, idList1Cnt);

                il.Emit(OpCodes.Ldloc, idList0Cnt);
                il.Emit(OpCodes.Ldloc, idList1Cnt);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brfalse, lbFalse); // 不相同直接结束cmp

                // 开始for递归比较
                // i = 0
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Stloc, idForI);
                il.Emit(OpCodes.Br, iIsLessCntLabel);
                // 内部item比较代码
                il.MarkLabel(cmpItemLabel);
                {
                    var listGetItemMethod = listType.GetMethod("get_Item");
                    // 存储item0
                    il.Emit(OpCodes.Ldloc, idList0);
                    il.Emit(OpCodes.Ldloc, idForI);
                    il.Emit(OpCodes.Callvirt, listGetItemMethod);
                    il.Emit(OpCodes.Stloc, idItem0);
                    // 存储item1
                    il.Emit(OpCodes.Ldloc, idList1);
                    il.Emit(OpCodes.Ldloc, idForI);
                    il.Emit(OpCodes.Callvirt, listGetItemMethod);
                    il.Emit(OpCodes.Stloc, idItem1);
                    // 构建item上下文
                    var ilCtxItem = new ILCtxItem(); ilCtxItem.opCodes = OpCodes.Ldloc; ilCtxItem.varID0 = idItem0; ilCtxItem.varID1 = idItem1;
                    ilCtxList.Add(ilCtxItem);
                    // 递归解析
                    GenerateField(itemType, ilCtxList);
                    ilCtxList.RemoveAt(ilCtxList.Count - 1);
                }
                // i++
                il.MarkLabel(iPPLabel);
                {
                    il.Emit(OpCodes.Ldloc, idForI);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Stloc, idForI);
                }
                // i < cnt
                il.MarkLabel(iIsLessCntLabel);
                {
                    il.Emit(OpCodes.Ldloc, idForI);
                    il.Emit(OpCodes.Ldloc, idList0Cnt);
                    il.Emit(OpCodes.Clt);
                    il.Emit(OpCodes.Brtrue, cmpItemLabel); // 如果 i < cnt 则进入内部for
                }

                // 结束标签
                il.MarkLabel(endLabel);

            }

            /// <summary>
            /// 比较一个field
            /// </summary>
            /// <param name="fiList"></param>
            void GenerateField(Type nowType, List<ILCtxItem> ilCtxList) 
            {
                if(nowType.IsList())
                {
                    Debug.Log(" IsListType " + nowType);
                    GenerateList(nowType, ilCtxList);
                }
                // 基本类型
                else if(nowType.IsBasicType())
                {
                    Debug.Log(" IsBasicType " + nowType);
                    GenerateBasicType(ilCtxList);
                }
                // Unity Type, 使用 op_Equality
                else if(nowType.IsUnityType()) 
                {
                    Debug.Log(" Unity Type " + nowType);
                    GenerateHaveOpEqualType(nowType, ilCtxList);
                }
                // 其他一些带有 op_Equality 的（例如string，Vector3）
                else if(nowType.GetCurTypeOpEqualMethodInfoIncludeParent() != null)
                {
                    Debug.Log(" op_Equality ==== " + nowType);
                    GenerateHaveOpEqualType(nowType, ilCtxList);
                }
                // 重写了 T Equal(T)
                else if(nowType.GetCurTypeEqualsMethodInfo() != null)
                {
                    Debug.Log(" Equals ==== " + nowType);
                    GenerateCanEqualType(nowType, ilCtxList);
                }
                else // 递归生成
                {
                    Debug.Log(" 递归生成 " + nowType);
                    // TODO.. 这里现在是给编辑器的序列化数据使用，所以class不会为null
                    GenerateClass(nowType, ilCtxList);
                }
            }

            // var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            // foreach (var field in fields)
            // {
            //     Debug.Log(field);
            //     var ilCtxList = new List<ILCtxItem>();
            //     var ilCtxItem = new ILCtxItem(); ilCtxItem.opCodes = OpCodes.Ldfld; ilCtxItem.fi = field;
            //     ilCtxList.Add(ilCtxItem);
            //     GenerateField(field.FieldType, ilCtxList);
            // }
            GenerateField(type, new List<ILCtxItem>());

            // 默认压入true作为返回值
            {
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Stloc_0);
                // 直接往lbRet跳，以免被赋值false
                il.Emit(OpCodes.Br, lbRet);
            }
            // 表示不同
            {
                il.MarkLabel(lbFalse);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Br, lbRet);
            }
            // 退出代码
            {
                il.MarkLabel(lbRet);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ret);
            }

            var cmp = dm.CreateDelegate(typeof(Func<T, T, bool>)) as Func<T, T, bool>;
            m_mapTypeCmpCache.Add(type, cmp);
            return cmp;
        }

        private static Dictionary<Type, object> m_mapTypeCloneCache = new Dictionary<Type, object>();
        /// <summary>
        /// TODO.. class null的时候处理
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Action<T, T> GetTypeClone<T>()
        {
            Type type = typeof(T);
            object cmpObj = null;
            if(m_mapTypeCloneCache.TryGetValue(type, out cmpObj)) 
            {
                return cmpObj as Action<T, T>;
            }

            var dm = new DynamicMethod("", null, new Type[]{type, type});
            var il = dm.GetILGenerator();
            Label lbFalse = il.DefineLabel();
            Label lbRet = il.DefineLabel();
            LocalBuilder localBool = il.DeclareLocal(typeof(bool)); // For ans
            int localVarInt = 1; // 记录局部变量使用的id
            // debug

            /// <summary>
            /// 加载参数0
            /// </summary>
            void LoadParm0()
            {
                Debug.Log("Ldarg_0");
                il.Emit(OpCodes.Ldarg_0);
            }
            /// <summary>
            /// 加载参数1
            /// </summary>
            void LoadParm1()
            {
                Debug.Log("Ldarg_1");
                il.Emit(OpCodes.Ldarg_1);
            }
            void MakeSetField(FieldInfo fi) 
            {
                if(fi != null) 
                {
                    Debug.Log("Ldfld " + fi);
                    il.Emit(OpCodes.Ldfld, fi);
                    Debug.Log("Stfld " + fi);
                    il.Emit(OpCodes.Stfld, fi);
                }
            }
            void MakeSetItem(MethodInfo getItemMI, MethodInfo setItemMI) 
            {
                Debug.Log("Callvirt " + getItemMI);
                il.Emit(OpCodes.Callvirt, getItemMI);
                Debug.Log("Callvirt " + setItemMI);
                il.Emit(OpCodes.Callvirt, setItemMI);
            }
            void RecursiveLoadParm0(List<ILCtxItem> ilCtxList, bool ignoreLast = true)
            {
                LoadParm0();
                if(ilCtxList != null && ilCtxList.Count > 0) 
                {
                    // 加载参数0，并获取对应field
                    int lastLoadLocInt = -1;
                    var cnt = ilCtxList.Count - (ignoreLast ? 1 : 0);
                    // for(int i=cnt-1;i>=0;i--) 
                    // {
                    //     if(ilCtxList[i].opCodes == OpCodes.Ldloc) 
                    //     {
                    //         lastLoadLocInt = i;
                    //         break;
                    //     }
                    // }
                    if(lastLoadLocInt == -1) 
                    {
                        // LoadParm0();
                        for(int i=0;i<cnt;i++)
                        {
                            var item = ilCtxList[i];
                            if(item.opCodes == OpCodes.Ldfld) 
                            {
                                Debug.Log("Ldfld, " + item.fi.Name);
                                il.Emit(OpCodes.Ldfld, item.fi); 
                            }
                            else if(item.opCodes == OpCodes.Ldloc)
                            {
                                Debug.Log("Ldloc, " + item.varID0);
                                il.Emit(OpCodes.Ldloc, item.varID0); 
                            }
                            else if(item.opCodes == OpCodes.Callvirt) 
                            {
                                Debug.Log("Callvirt, " + item.mi);
                                il.Emit(OpCodes.Callvirt, item.mi); 
                            }
                            else 
                            {
                                Debug.LogErrorFormat("RecursiveLoadParm0 上下文item中混入了 无法解析的OpCodes：{0}", item.opCodes);
                            }
                        }
                    }
                    else 
                    {
                        for(int i=lastLoadLocInt;i<cnt;i++)
                        {
                            var item = ilCtxList[i];
                            if(item.opCodes == OpCodes.Ldfld) 
                            {
                                Debug.Log("Ldfld, " + item.fi.Name);
                                il.Emit(OpCodes.Ldfld, item.fi); 
                            }
                            else if(item.opCodes == OpCodes.Ldloc) 
                            {
                                Debug.Log("Ldloc, " + item.varID0);
                                // il.Emit(item.opCodes, item.varID0);
                            }
                            else 
                            {
                                Debug.LogErrorFormat("RecursiveLoadParm0 上下文item中混入了 无法解析的OpCodes：{0}", item.opCodes);
                            }
                        }
                    }
                }
            }
            void RecursiveLoadParm1(List<ILCtxItem> ilCtxList, bool ignoreLast = true)
            {
                LoadParm1();
                if(ilCtxList != null && ilCtxList.Count > 0) 
                {
                    int lastLoadLocInt = -1;
                    var cnt = ilCtxList.Count - (ignoreLast ? 1 : 0);
                    // for(int i=cnt-1;i>=0;i--) 
                    // {
                    //     if(ilCtxList[i].opCodes == OpCodes.Ldloc) 
                    //     {
                    //         lastLoadLocInt = i;
                    //         break;
                    //     }
                    // }
                    if(lastLoadLocInt == -1) 
                    {
                        // LoadParm1();
                        for(int i=0;i<cnt;i++)
                        {
                            var item = ilCtxList[i];
                            if(item.opCodes == OpCodes.Ldfld) 
                            {
                                Debug.Log("Ldfld, " + item.fi.Name);
                                il.Emit(OpCodes.Ldfld, item.fi); 
                            }
                            else if(item.opCodes == OpCodes.Ldloc)
                            {
                                Debug.Log("Ldloc, " + item.varID1);
                                il.Emit(OpCodes.Ldloc, item.varID1); 
                            }
                            else if(item.opCodes == OpCodes.Callvirt) 
                            {
                                Debug.Log("Callvirt, " + item.mi);
                                il.Emit(OpCodes.Callvirt, item.mi); 
                            }
                            else 
                            {
                                Debug.LogErrorFormat("RecursiveLoadParm1 上下文item中混入了 无法解析的OpCodes：{0}", item.opCodes);
                            }
                        }
                    }
                    else 
                    {
                        for(int i=lastLoadLocInt;i<cnt;i++)
                        {
                            var item = ilCtxList[i];
                            if(item.opCodes == OpCodes.Ldfld) 
                            {
                                il.Emit(OpCodes.Ldfld, item.fi); 
                            }
                            else if(item.opCodes == OpCodes.Ldloc) 
                            {
                                il.Emit(item.opCodes, item.varID1);
                            }
                            else 
                            {
                                Debug.LogErrorFormat("RecursiveLoadParm0 上下文item中混入了 无法解析的OpCodes：{0}", item.opCodes);
                            }
                        }
                    }
                }
            }
            /// <summary>
            /// 比较某一个field，这个field是基本类型的
            /// </summary>
            void GenerateBasicType(List<ILCtxItem> ilCtxList)
            {
                // 加载参数0，并获取对应field
                RecursiveLoadParm0(ilCtxList);
                // 加载参数1，并获取对应field
                RecursiveLoadParm1(ilCtxList);
                // 作比较
                il.Emit(OpCodes.Ceq);
                // 如果不同，则跳到lbFalse
                il.Emit(OpCodes.Brfalse, lbFalse);
            }

            void GenerateStraightSetType(List<ILCtxItem> ilCtxList)
            {
                if(ilCtxList != null && ilCtxList.Count > 0) 
                {
                    Debug.Log(" ------------------- GenerateStraightSetType start");
                    // 加载参数0，并获取对应field
                    RecursiveLoadParm0(ilCtxList);
                    // 加载参数1，并获取对应field
                    RecursiveLoadParm1(ilCtxList);
                    // Set
                    var lastItem = ilCtxList[ilCtxList.Count - 1];
                    if(lastItem.opCodes == OpCodes.Ldfld) 
                    {
                        MakeSetField(lastItem.fi);
                    }
                    else if(lastItem.opCodes == OpCodes.Callvirt)
                    {
                        MakeSetItem(lastItem.mi, lastItem.miex);
                    }
                    Debug.Log(" ------------------- GenerateStraightSetType end");
                }
                else 
                {
                    // 没有上下文，说明需要的Clone就是直接拷贝
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Starg, 0);
                }
            }

            void GenerateCanEqualType(Type nowType, List<ILCtxItem> ilCtxList)
            {
                // 加载参数0，并获取对应field
                RecursiveLoadParm0(ilCtxList);
                // 加载参数1，并获取对应field
                RecursiveLoadParm1(ilCtxList);
                // 获取比较函数
                var opMethod = nowType.GetCurTypeEqualsMethodInfo();
                il.Emit(OpCodes.Callvirt, opMethod);
                // 比较结果和true作比较
                il.Emit(OpCodes.Ldc_I4_1);
                // 作比较
                il.Emit(OpCodes.Ceq);
                // 如果不同，则跳到lbFalse
                il.Emit(OpCodes.Brfalse, lbFalse);
            }

            /// <summary>
            /// 比较一个class类型的field
            /// </summary>
            void GenerateClass(Type nowType, List<ILCtxItem> ilCtxList) 
            {
                foreach (var field in nowType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    // Debug.Log(nowFi.Name + " 中的 " + field.Name);
                    var ilCtxItem = new ILCtxItem(); ilCtxItem.opCodes = OpCodes.Ldfld; ilCtxItem.fi = field;
                    ilCtxList.Add(ilCtxItem);
                    GenerateField(field.FieldType, ilCtxList);
                    ilCtxList.RemoveAt(ilCtxList.Count - 1);
                }
            }

            void GenerateList(Type listType, List<ILCtxItem> ilCtxList) 
            {
                var itemType = listType.GetListElementType();
                if(itemType == null) 
                {
                    Debug.LogErrorFormat(" GetTypeCmp 的 GenerateList 中传入了一个无法生成的List类型：{0}", listType);
                    return;
                }

                // 定义变量
                var idList0 = localVarInt++;
                il.DeclareLocal(listType);
                var idList1 = localVarInt++;
                il.DeclareLocal(listType);
                var idList0IsNull = localVarInt++;
                il.DeclareLocal(typeof(bool));
                var idList1IsNull = localVarInt++;
                il.DeclareLocal(typeof(bool));
                var idList0Cnt = localVarInt++;
                il.DeclareLocal(typeof(int));
                var idList1Cnt = localVarInt++;
                il.DeclareLocal(typeof(int));
                var idForI = localVarInt++;
                il.DeclareLocal(typeof(int));
                var idItem0 = localVarInt++;
                il.DeclareLocal(itemType);
                var idItem1 = localVarInt++;
                il.DeclareLocal(itemType);
                // 定义标签
                var endLabel = il.DefineLabel();
                var list0IsNullLabel = il.DefineLabel();
                var cmpItemLabel = il.DefineLabel();
                var iPPLabel = il.DefineLabel(); // for 的 i++
                var iIsLessCntLabel = il.DefineLabel(); // for 的 i < cnt
                var beginSetLabel = il.DefineLabel(); // 开始设置List
                var beginForLabel = il.DefineLabel(); // 开始for
                // method
                var listGetCountMethod = listType.GetMethod("get_Count");
                var listRemoveAtMethod = listType.GetMethod("RemoveAt", BindingFlags.Public | BindingFlags.Instance, null, new Type[]{typeof(int)}, null);
                var listAddMethod = listType.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance, null, new Type[]{itemType}, null);
                var listGetItemMethod = listType.GetMethod("get_Item");
                var listSetItemMethod = listType.GetMethod("set_Item");

                FieldInfo nowField = null;
                if(ilCtxList != null && ilCtxList.Count > 0) 
                {
                    nowField = ilCtxList[ilCtxList.Count - 1].fi;
                }
                if(nowField == null) 
                {
                    // Debug.LogErrorFormat("暂不支持最外层是List的结构，Type = {0}", listType);
                    // 特殊逻辑
                    var _cmpSecondParmIsNullLabel = il.DefineLabel();
                    LoadParm0();
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Ceq);
                    il.Emit(OpCodes.Brfalse, _cmpSecondParmIsNullLabel);

                    // 第一个参数是null
                    {
                        il.GenUnityError("第一个参数是null，clone没有意义！");
                        il.Emit(OpCodes.Br, lbRet);
                    }

                    // 第一个参数不是null
                    il.MarkLabel(_cmpSecondParmIsNullLabel);
                    {
                        LoadParm1();
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Ceq);
                        // 第二个也不是null，进入正式比较
                        il.Emit(OpCodes.Brfalse, beginSetLabel);
                    }

                    // 第二个参数是null
                    {
                        il.GenUnityError("第二个参数是null，clone没有意义！");
                        il.Emit(OpCodes.Br, lbRet);
                    }
                }
                else 
                {
                    // 初始化变量
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Stloc, idList0IsNull);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Stloc, idList1IsNull);

                    // 加载参数0，并获取对应field
                    RecursiveLoadParm0(ilCtxList, false);
                    il.Emit(OpCodes.Stloc, idList0);
                    // 加载参数1，并获取对应field
                    RecursiveLoadParm1(ilCtxList, false);
                    il.Emit(OpCodes.Stloc, idList1);

                    // 比较是否是null
                    // 比较第一个List是否为null
                    il.Emit(OpCodes.Ldloc, idList0);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Ceq);
                    il.Emit(OpCodes.Stloc, idList0IsNull);

                    // 比较第二个List是否为null
                    il.Emit(OpCodes.Ldloc, idList1);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Ceq);
                    il.Emit(OpCodes.Stloc, idList1IsNull);

                    // 判断第一个List是否为null
                    il.Emit(OpCodes.Ldloc, idList0IsNull);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Ceq);
                    il.Emit(OpCodes.Brtrue, list0IsNullLabel);

                    // 第一个不是null
                    {
                        // 判断第二个List是不是null
                        il.Emit(OpCodes.Ldloc, idList1IsNull);
                        il.Emit(OpCodes.Ldc_I4_1);
                        il.Emit(OpCodes.Ceq);
                        // 第二个不是null，进入正式赋值阶段
                        Debug.Log(" Brfalse " + beginSetLabel);
                        il.Emit(OpCodes.Brfalse, beginSetLabel);
                    }

                    // 第一个不是null，第二个为null
                    {
                        // 为第一个set为null
                        RecursiveLoadParm0(ilCtxList);
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Stfld, nowField);
                        il.Emit(OpCodes.Br, endLabel);
                    }

                    // 第一个为null的情况
                    il.MarkLabel(list0IsNullLabel);
                    {
                        // 判断第二个List是不是null
                        il.Emit(OpCodes.Ldloc, idList1IsNull);
                        il.Emit(OpCodes.Ldc_I4_1);
                        il.Emit(OpCodes.Ceq);
                        // 如果第二个也是null，表示双方是相同的，直接退出了
                        il.Emit(OpCodes.Brtrue, endLabel);
                        // 否则，第一个List新建一个
                        RecursiveLoadParm0(ilCtxList);
                        var listctor = listType.GetConstructor(new Type[]{});
                        il.Emit(OpCodes.Newobj, listctor);
                        il.Emit(OpCodes.Stfld, nowField);
                        il.Emit(OpCodes.Br, beginSetLabel);
                    }

                }

                // 正式set部分
                il.MarkLabel(beginSetLabel);
                {
                    // 到这里说明两个List都不为null
                    // 使得两个list的数目相同
                    var itemctor = itemType.GetConstructor(new Type[]{});
                    // 注意，不能直接拿原来的变量，因为可能之前的局部变量是null，现在新建过了，因此现在要重新获取list
                    // if(nowField != null) 
                    // {
                    //     RecursiveLoadParm0(ilCtxList, false);
                    // }
                    // else 
                    // {
                    //     // TODO.. 合并到上面的方法
                    //     LoadParm0();
                    // }
                    RecursiveLoadParm0(ilCtxList, false);
                    il.Emit(OpCodes.Stloc, idList0);
                    // if(nowField != null) 
                    // {
                    //     RecursiveLoadParm1(ilCtxList, false);
                    // }
                    // else 
                    // {
                    //     // TODO.. 合并到上面的方法
                    //     LoadParm1();
                    // }
                    RecursiveLoadParm1(ilCtxList, false);
                    il.Emit(OpCodes.Stloc, idList1);

                    il.Emit(OpCodes.Ldloc, idList0);
                    il.Emit(OpCodes.Callvirt, listGetCountMethod);
                    il.Emit(OpCodes.Stloc, idList0Cnt);

                    il.Emit(OpCodes.Ldloc, idList1);
                    il.Emit(OpCodes.Callvirt, listGetCountMethod);
                    il.Emit(OpCodes.Stloc, idList1Cnt);

                    il.Emit(OpCodes.Ldloc, idList0Cnt);
                    il.Emit(OpCodes.Ldloc, idList1Cnt);
                    il.Emit(OpCodes.Ceq);
                    il.Emit(OpCodes.Brtrue, beginForLabel); // 数目相同的情况直接进入for

                    // 数目不同
                    {
                        // 标签
                        var list0CntGreatlist1CntLabel = il.DefineLabel();

                        il.Emit(OpCodes.Ldloc, idList0Cnt);
                        il.Emit(OpCodes.Ldloc, idList1Cnt);
                        il.Emit(OpCodes.Clt); // < 
                        il.Emit(OpCodes.Brfalse, list0CntGreatlist1CntLabel);

                        // list0的cnt < list1的cnt
                        {
                            il.GenFor(
                                () => {
                                    il.Emit(OpCodes.Ldloc, idList1Cnt);
                                    il.Emit(OpCodes.Ldloc, idList0Cnt);
                                    il.Emit(OpCodes.Sub);
                                }, 
                                (idLoopIter) => {
                                    il.Emit(OpCodes.Ldloc, idList0);
                                    if(itemctor == null) 
                                    {
                                        // il.Emit(OpCodes.Newobj, itemctor);
                                        il.Emit(OpCodes.Ldc_I4_0);
                                        il.Emit(OpCodes.Callvirt, listAddMethod);
                                    }
                                    else 
                                    {
                                        il.Emit(OpCodes.Newobj, itemctor);
                                        il.Emit(OpCodes.Callvirt, listAddMethod);
                                    }
                                }, 
                                ref localVarInt);
                        }

                        // list0的cnt > list1的cnt
                        il.MarkLabel(list0CntGreatlist1CntLabel);
                        {
                            // 变量
                            var idLoopCnt = localVarInt++;
                            il.DeclareLocal(typeof(int));
                            var idLoopIter = localVarInt++;
                            il.DeclareLocal(typeof(int));
                            // 标签
                            var innerIIsLessCntLabel = il.DefineLabel();
                            var innerForLabel = il.DefineLabel();

                            il.Emit(OpCodes.Ldloc, idList0Cnt);
                            il.Emit(OpCodes.Ldloc, idList1Cnt);
                            il.Emit(OpCodes.Sub);
                            il.Emit(OpCodes.Stloc, idLoopCnt);
                            // i = 0
                            il.Emit(OpCodes.Ldc_I4_0);
                            il.Emit(OpCodes.Stloc, idLoopIter);
                            il.Emit(OpCodes.Br, innerIIsLessCntLabel);

                            // for
                            il.MarkLabel(innerForLabel);
                            {
                                il.Emit(OpCodes.Ldloc, idList0);
                                il.Emit(OpCodes.Ldloc, idList0);
                                il.Emit(OpCodes.Callvirt, listGetCountMethod);
                                il.Emit(OpCodes.Ldc_I4_1);
                                il.Emit(OpCodes.Sub);
                                il.Emit(OpCodes.Callvirt, listRemoveAtMethod);
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
                        }
                    }

                    // for阶段
                    il.MarkLabel(beginForLabel);
                    {
                        il.GenFor(
                            () => {
                                il.Emit(OpCodes.Ldloc, idList0);
                                il.Emit(OpCodes.Callvirt, listGetCountMethod);
                            }, 
                            (idLoopIter) => {
                                var ilCtxItem1 = new ILCtxItem(); ilCtxItem1.opCodes = OpCodes.Ldloc; ilCtxItem1.varID0 = idLoopIter; ilCtxItem1.varID1 = idLoopIter;
                                ilCtxList.Add(ilCtxItem1);
                                var ilCtxItem2 = new ILCtxItem(); ilCtxItem2.opCodes = OpCodes.Callvirt; ilCtxItem2.mi = listGetItemMethod; ilCtxItem2.miex = listSetItemMethod;
                                ilCtxList.Add(ilCtxItem2);
                                {
                                    GenerateField(itemType, ilCtxList);
                                    // GenerateStraightSetType(ilCtxList);
                                }
                                ilCtxList.RemoveAt(ilCtxList.Count - 1);
                                ilCtxList.RemoveAt(ilCtxList.Count - 1);
                            }, 
                            ref localVarInt);
                    }
                }

                // 结束标签
                il.MarkLabel(endLabel);

            }

            /// <summary>
            /// 比较一个field
            /// </summary>
            /// <param name="fiList"></param>
            void GenerateField(Type nowType, List<ILCtxItem> ilCtxList) 
            {
                if(nowType.IsList())
                {
                    Debug.Log(" IsListType " + nowType);
                    GenerateList(nowType, ilCtxList);
                }
                // 基本类型
                else if(nowType.IsBasicType())
                {
                    Debug.Log(" IsBasicType " + nowType);
                    GenerateStraightSetType(ilCtxList);
                }
                // Unity Type, 使用 op_Equality
                else if(nowType.IsUnityType()) 
                {
                    Debug.Log(" Unity Type " + nowType);
                    GenerateStraightSetType(ilCtxList);
                }
                // 其他一些带有 op_Equality 的（例如string，Vector3）
                else if(nowType.GetCurTypeOpEqualMethodInfoIncludeParent() != null)
                {
                    Debug.Log(" op_Equality ==== " + nowType);
                    GenerateStraightSetType(ilCtxList);
                }
                // // 重写了 T Equal(T)
                // else if(nowType.GetCurTypeEqualsMethodInfo() != null)
                // {
                //     Debug.Log(" Equals ==== " + nowType);
                //     GenerateCanEqualType(nowType, ilCtxList);
                // }
                else // 递归生成
                {
                    Debug.Log(" 递归生成 " + nowType);
                    // TODO.. 这里现在是给编辑器的序列化数据使用，所以class不会为null
                    GenerateClass(nowType, ilCtxList);
                }
            }

            GenerateField(type, new List<ILCtxItem>());

            // 退出代码
            {
                il.MarkLabel(lbRet);
                il.Emit(OpCodes.Ret);
            }

            var clone = dm.CreateDelegate(typeof(Action<T, T>)) as Action<T, T>;
            m_mapTypeCloneCache.Add(type, clone);
            return clone;
        }
    }
}