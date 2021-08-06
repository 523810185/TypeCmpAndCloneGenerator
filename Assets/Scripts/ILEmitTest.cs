/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Reflection.Emit;
using ILUtility;

public class ILEmitTest : MonoBehaviour
{
    public class AAAType 
    {
        public int int_1;
        public string str_1;
        public Vector3 vec3_1;
    }
    public string logstr;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(" ??? ");
        // var a = GetIfElse();
        // a(true);
        // a(false);

        // var f = GetLog();
        // f();

        // var logFunc = this.GetLog(logstr);
        // logFunc();

        // Type mytype = typeof(AAAType);
        // var members = mytype.GetFields(BindingFlags.Public | BindingFlags.Instance);
        // foreach (var item in members)
        // {
        //     Debug.Log(item);
        // }
        // int x = 7;

        var func = GetTypeCmp<AAAType>();
        var aaa = new AAAType(); aaa.int_1 = 5;
        var bbb = new AAAType(); bbb.int_1 = 5;

        Debug.Log("Ans " + func(aaa, bbb));
        aaa.int_1 = 6;
        Debug.Log("Ans " + func(aaa, bbb));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void GenILMethod()
    {
        
    }

    Action<bool> GetIfElse()
    {
        var dm = new DynamicMethod("", null, new Type[] { typeof(bool) });
        var gen = dm.GetILGenerator();

        Label lbFalse = gen.DefineLabel();
        Label lbRet = gen.DefineLabel();

        //判断
        gen.Emit(OpCodes.Ldarg_0);
        gen.Emit(OpCodes.Ldc_I4, 1);
        gen.Emit(OpCodes.Ceq);
        //如果false: 跳至false代码
        gen.Emit(OpCodes.Brfalse, lbFalse);
        //true代码
        // gen.EmitWriteLine("真");
        gen.Emit(OpCodes.Ldstr, "Hello, Kitty!");
        gen.Emit(OpCodes.Call, typeof(Debug).GetMethod("Log", new Type[] {typeof(string)}) );
        // gen.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }));
        //跳至退出
        gen.Emit(OpCodes.Br, lbRet);
        //false代码
        gen.MarkLabel(lbFalse);
        gen.EmitWriteLine("假");
        // gen.Emit(OpCodes.Call, typeof(Console).GetMethod("ReadLine"));
        //退出代码
        gen.MarkLabel(lbRet);
        gen.Emit(OpCodes.Ret);

        // dm.Invoke(null, new object[] { true });

        var f = dm.CreateDelegate(typeof(Action<bool>)) as Action<bool>;

        return f;
    }

    Action GetLog(string logstr)
    {
        var dm = new DynamicMethod("", null, null);
        var il = dm.GetILGenerator();
        
        il.GenUnityLog(logstr);

        il.Emit(OpCodes.Ret);

        return dm.CreateDelegate(typeof(Action)) as Action;
    }

    Func<T, T, bool> GetTypeCmp<T>()
    {
        var type = typeof(T);
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        FieldInfo field = null;
        foreach (var item in fields)
        {
            field = item;
            Debug.Log(item);
            break;
        }

        var dm = new DynamicMethod("", typeof(bool), new Type[]{type, type});
        var il = dm.GetILGenerator();

        Label lbFalse = il.DefineLabel();
        Label lbRet = il.DefineLabel();
        LocalBuilder localBool = il.DeclareLocal(typeof(bool));

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, field);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldfld, field);

        il.Emit(OpCodes.Ceq);

        // il.Emit(OpCodes.Stloc_0);
        // il.Emit(OpCodes.Ldloc_0);

        //如果false: 跳至false代码
        il.Emit(OpCodes.Brfalse, lbFalse);
        //true代码
        il.GenUnityLog("相同");
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Stloc_0);
        //跳至退出
        il.Emit(OpCodes.Br, lbRet);
        //false代码
        il.MarkLabel(lbFalse);
        il.GenUnityLog("不同");
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stloc_0);
        // gen.Emit(OpCodes.Call, typeof(Console).GetMethod("ReadLine"));
        //退出代码
        il.MarkLabel(lbRet);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ret);

        return dm.CreateDelegate(typeof(Func<T, T, bool>)) as Func<T, T, bool>;
    }
}*/



using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Reflection.Emit;
using ILUtility;
using W3.TypeExtension;

public class ILEmitTest : MonoBehaviour
{
    [Serializable]
    public class EEEType : IEquatable<EEEType>
    {
        public int iii;
        public string sss;

        public bool Equals(EEEType other) 
        {
            return iii == other.iii && sss == other.sss;
        }
    }
    [Serializable]
    public class InnerClass 
    {
        public int inner_int;
        public GameObject gameObject;

        public Vector3 inner_vec3;

        public List<int> int_list;

        // public bool Equals(InnerClass obj)
        // {
        //     return true;
        // }
    }
    [Serializable]
    public class AAAType 
    {
        // public int int_1;
        // public int int_2;
        // public bool bool_1;
        // public string str_1;
        // public InnerClass innerClass = new InnerClass();

        // public EEEType eEE = new EEEType();

        public List<InnerClass> string_list = new List<InnerClass>();

        // public List<int> int_list;

        // public Struct2 structIt = new Struct2();
    }

    public class A 
    {
        public override bool Equals(object o) 
        {
            return true;
        }

        public static A operator== (A par1, A par2) 
        {
            return new A();
        }

        public static A operator!= (A par1, A par2) 
        {
            return new A();
        }
    }

    public class B : A
    {
        // public bool Equals(B o) 
        // {
        //     return true;
        // }

        // public bool Equals(A o) 
        // {
        //     return true;
        // }

        public static B operator== (B par1, B par2) 
        {
            return new B();
        }

        public static B operator!= (B par1, B par2) 
        {
            return new B();
        }
    }
    
    [Serializable]
    public struct TestStruct 
    {
        public int x;
        public string str;
        public Struct2 sss;
        public GameObject gameObject;
    }

    [Serializable]
    public struct Struct2 
    {
        public int inner_int;
    }

    public class impListString : List<string> 
    {

    }

    private Func<List<AAAType>, List<AAAType>, bool> cmp;
    private Action<List<AAAType>, List<AAAType>> clone;
    public string logstr;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(" ??? ");
        // var a = GetIfElse();
        // a(true);
        // a(false);

        // var f = GetLog();
        // f();

        // var logFunc = this.GetLog(logstr);
        // logFunc();

        // Type mytype = typeof(AAAType);
        // var members = mytype.GetFields(BindingFlags.Public | BindingFlags.Instance);
        // foreach (var item in members)
        // {
        //     Debug.Log(item);
        // }
        // int x = 7;

        // var func = TypeUtility.GetTypeCmp<AAAType>();
        // var aaa = new AAAType(); aaa.int_1 = 5; aaa.innerClass.inner_int = 7;
        // var bbb = new AAAType(); bbb.int_1 = 5; bbb.innerClass.inner_int = 4;

        // Debug.Log("Equal " + func(aaa, bbb));
        
        // foreach (var item in typeof(GameObject).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        // {
        //     Debug.Log(" ---- " + item);
        // }
        // Debug.Log(" op_Equality == null ? " + (fi == null));

        var testType = typeof(List<string>);
        var cons = testType.GetConstructor(new Type[]{});
        var removeAt = testType.GetMethod("RemoveAt", BindingFlags.Public | BindingFlags.Instance, null, new Type[]{typeof(int)}, null);
        // var aaa = testType.GetMethod("Equals", BindingFlags.Instance | BindingFlags.Public, null, new Type[]{testType}, null);

        // var testType = typeof(GameObject);
        // bool isUnityType = typeof(UnityEngine.Object).IsAssignableFrom(testType);
        // var fi = testType.GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
        // foreach (var item in aaa)
        {
            // Debug.Log(" sss " + aaa);
        }
        var aaa = testType.GetGenericTypeDefinition();
        var bbb = typeof(List<>);
        // Debug.Log("ele " + (aaa == typeof(List<>)));
        foreach (var item in testType.GetGenericArguments())
        {
            // Debug.Log(" ??? " + item);
            // var _ = item.GetGenericParameterConstraints();
        }
        // foreach (var item in testType.GetGenericParameterConstraints())
        // {
        //     Debug.Log(" !!! " + item);
        // }
        // Debug.Log(" isUnityType " + (isUnityType) + (fi == null));

        // List<TestStruct> list = new List<TestStruct>();
        // TestStruct _first = new TestStruct(); _first.x = 5; _first.str = "??"; _first.sss = new Struct2(); _first.sss.inner_int = 8;
        // list.Add(_first);
        // var _get = list[0];
        // _get.x = 3;
        // Debug.Log(" ?? " + _get.sss.inner_int);

        var dm = new DynamicMethod("", null, new Type[]{typeof(AAAType)});
        var il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, typeof(AAAType).GetField("string_list"));
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Callvirt, typeof(List<InnerClass>).GetMethod("get_Item"));
        il.Emit(OpCodes.Newobj, typeof(List<int>).GetConstructor(new Type[]{}));
        il.Emit(OpCodes.Stfld, typeof(InnerClass).GetField("int_list"));
        il.Emit(OpCodes.Ret);
        var fc = dm.CreateDelegate(typeof(Action<AAAType>)) as Action<AAAType>;
        var aaatest = new AAAType();
        aaatest.string_list.Add(new InnerClass());
        Debug.Log("aaa111 " + (aaatest.string_list[0].int_list == null));
        fc(aaatest);
        Debug.Log("aaa222 " + (aaatest.string_list[0].int_list == null));

        this.cmp = TypeUtility.GetTypeCmp<List<AAAType>>();
        this.clone = TypeUtility.GetTypeClone<List<AAAType>>();
    }

    public void ZZY(AAAType type1, AAAType type2)
{
    int num;
    bool flag3 = false;
    List<InnerClass> objA = type1.string_list;
    List<InnerClass> list2 = type2.string_list;
    flag3 = ReferenceEquals(list2, null);
    if (ReferenceEquals(objA, null))
    {
        if (!flag3)
        {
            type1.string_list = new List<InnerClass>();
            goto TR_0024;
        }
    }
    else if (!flag3)
    {
        goto TR_0024;
    }
    else
    {
        type1.string_list = null;
    }
    return;
TR_0024:
    num = objA.Count;
    int count = list2.Count;
    if (num != count)
    {
        if (num < count)
        {
            int num4 = count - num;
            for (int j = 0; j < num4; j++)
            {
                objA.Add(new InnerClass());
            }
        }
        int num6 = num - count;
        for (int i = 0; i < num6; i++)
        {
            objA.RemoveAt(objA.Count - 1);
        }
    }
    int num8 = objA.Count;
    int num9 = 0;
    while (true)
    {
        while (true)
        {
            if (num9 < num8)
            {
                type1.string_list[num9].inner_int = type2.string_list[num9].inner_int;
                bool flag5 = false;
                List<int> list3 = type1.string_list[num9].int_list;
                List<int> list4 = type2.string_list[num9].int_list;
                flag5 = ReferenceEquals(list4, null);
                if (!ReferenceEquals(list3, null))
                {
                    if (flag5)
                    {
                        type1.string_list[num9].int_list = null;
                        break;
                    }
                }
                else
                {
                    if (flag5)
                    {
                        break;
                    }
                    type1.string_list[num9].int_list = new List<int>();
                }
                int num10 = list3.Count;
                int num11 = list4.Count;
                if (num10 != num11)
                {
                    if (num10 < num11)
                    {
                        int num15 = num11 - num10;
                        for (int k = 0; k < num15; k++)
                        {
                            list3.Add(0);
                        }
                    }
                    int num17 = num10 - num11;
                    for (int j = 0; j < num17; j++)
                    {
                        list3.RemoveAt(list3.Count - 1);
                    }
                }
                int num19 = list3.Count;
                for (int i = 0; i < num19; i++)
                {
                    type1.string_list[num9].int_list[i] = type2.string_list[num9].int_list[i];
                }
            }
            else
            {
                return;
            }
            break;
        }
        num9++;
    }
}

 




    public bool ABCDEF(AAAType para, AAAType parb, string ccc, string ddd)
    {
        // var listA = para.string_list;
        // var listB = parb.string_list;
        // if(listA == null && listB == null) 
        // {
        //     return true;
        // }
        // if(listA == null || listB == null) 
        // {
        //     return false;
        // }
        // if(listA.Count != listB.Count) 
        // {
        //     return false;
        // }
        // int cnt = listA.Count;
        // for(int i=0;i<cnt;i++) 
        // {
        //     var itema = listA[i];
        //     var itemb = listB[i];
        //     if(itema.inner_int != itemb.inner_int) 
        //     {
        //         return false;
        //     }
        //     // if(itema.inner_vec3 != itemb.inner_vec3) 
        //     // {
        //     //     return false;
        //     // }
        //     // if(itema.gameObject != itemb.gameObject) 
        //     // {
        //     //     return false;
        //     // }
        // }
        return true;
    }

    public void QQQClone(ref List<int> alist, List<int> blist, ref int uu)
    {
        if(alist == null && blist != null) 
        {
            alist = new List<int>();
        }

        for(int i=0;i<alist.Count;i++) 
        {
            alist[i] = blist[i];
        }

        // if(alist == null && blist != null) 
        // {
        //     alist = new List<int>(); // 无意义
        // }
        // if()
    }

    public List<AAAType> aaa, bbb;
    public TestStruct qqq, www;
    public List<string> aaaList, bbbList;
    public bool isSame = false;
    // Update is called once per frame
    void Update()
    {
        // isSame = cmp(aaa, bbb);
        if(cmp(aaa, bbb) == false)
        {
            Debug.Log("不同了！");
            clone(aaa, bbb);
        }
        // ZZY(aaa, bbb);
        // qqq = www;
    }

    private void GenILMethod()
    {
        
    }

    Action<bool> GetIfElse()
    {
        var dm = new DynamicMethod("", null, new Type[] { typeof(bool) });
        var gen = dm.GetILGenerator();

        Label lbFalse = gen.DefineLabel();
        Label lbRet = gen.DefineLabel();

        //判断
        gen.Emit(OpCodes.Ldarg_0);
        gen.Emit(OpCodes.Ldc_I4, 1);
        gen.Emit(OpCodes.Ceq);
        //如果false: 跳至false代码
        gen.Emit(OpCodes.Brfalse, lbFalse);
        //true代码
        // gen.EmitWriteLine("真");
        gen.Emit(OpCodes.Ldstr, "Hello, Kitty!");
        gen.Emit(OpCodes.Call, typeof(Debug).GetMethod("Log", new Type[] {typeof(string)}) );
        // gen.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }));
        //跳至退出
        gen.Emit(OpCodes.Br, lbRet);
        //false代码
        gen.MarkLabel(lbFalse);
        gen.EmitWriteLine("假");
        // gen.Emit(OpCodes.Call, typeof(Console).GetMethod("ReadLine"));
        //退出代码
        gen.MarkLabel(lbRet);
        gen.Emit(OpCodes.Ret);

        // dm.Invoke(null, new object[] { true });

        var f = dm.CreateDelegate(typeof(Action<bool>)) as Action<bool>;

        return f;
    }

    Action GetLog(string logstr)
    {
        var dm = new DynamicMethod("", null, null);
        var il = dm.GetILGenerator();
        
        il.GenUnityLog(logstr);

        il.Emit(OpCodes.Ret);

        return dm.CreateDelegate(typeof(Action)) as Action;
    }

    Func<T, T, bool> GetTypeCmp<T>()
    {
        Type type = typeof(T);
        // TODO.. 如果是基本类型或者string，应该直接返回一个Action即可，不需要IL
        var dm = new DynamicMethod("", typeof(bool), new Type[]{type, type});
        var il = dm.GetILGenerator();
        Label lbFalse = il.DefineLabel();
        Label lbRet = il.DefineLabel();
        LocalBuilder localBool = il.DeclareLocal(typeof(bool));
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
        /// <summary>
        /// 比较某一个field，这个field是基本类型的
        /// </summary>
        /// <param name="fi"></param>
        void GenerateBasicType(List<FieldInfo> fiList)
        {
            // 加载参数0，并获取对应field
            LoadParm0();
            foreach (var item in fiList)
            {
                il.Emit(OpCodes.Ldfld, item);   
            }
            // 加载参数1，并获取对应field
            LoadParm1();
            foreach (var item in fiList)
            {
                il.Emit(OpCodes.Ldfld, item);   
            }
            // 作比较
            il.Emit(OpCodes.Ceq);
            // 如果不同，则跳到lbFalse
            il.Emit(OpCodes.Brfalse, lbFalse);
        }

        /// <summary>
        /// 比较一个class类型的field
        /// </summary>
        /// <param name="nowFi"></param>
        /// <param name="fiList"></param>
        void GenerateClass(FieldInfo nowFi, List<FieldInfo> fiList) 
        {
            foreach (var field in nowFi.FieldType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                Debug.Log(nowFi.Name + " 中的 " + field.Name);
                fiList.Add(field);
                GenerateField(field, fiList);
                fiList.RemoveAt(fiList.Count - 1);
            }
        }

        /// <summary>
        /// 比较一个field
        /// </summary>
        /// <param name="nowFi"></param>
        /// <param name="fiList"></param>
        void GenerateField(FieldInfo nowFi, List<FieldInfo> fiList) 
        {
            if(nowFi.FieldType.IsClass)
            {
                // TODO.. 这里现在是给编辑器的序列化数据使用，所以class不会为null
                GenerateClass(nowFi, fiList);
            }
            else // TODO.. 这里应该判断为基本类型
            {
                // TODO.. string应该也被放在这里
                GenerateBasicType(fiList);
            }
        }

        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields)
        {
            Debug.Log(field);
            var fiList = new List<FieldInfo>();
            fiList.Add(field);
            GenerateField(field, fiList);
        }

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

        return dm.CreateDelegate(typeof(Func<T, T, bool>)) as Func<T, T, bool>;
    }
}
