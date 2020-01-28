using System;
using System.Collections.Generic;
using System.Linq;

namespace Come.CollectiveOAuth.Enums
{
    /// <summary>
    /// 数组，队列对象的列表类
    /// </summary>
    public static class ArrayExtensions
    {
        /// <summary>
        /// 扩展 Dictionary 根据Value反向查找Key的方法
        /// </summary>
        public static T1 Get<T1, T2>(this IEnumerable<KeyValuePair<T1, T2>> list, T2 t2)
        {
            foreach (KeyValuePair<T1, T2> obj in list)
                if (obj.Value.Equals(t2)) return obj.Key;
            return default(T1);
        }

        /// <summary>
        /// 扩展数组方法 可以在前或者后插入一个对象
        /// </summary>
        /// <param name="obj">要插入的对象</param>
        /// <param name="place">位置 after/before</param>
        /// <returns></returns>
        public static IEnumerable<T> Inject<T>(this IEnumerable<T> list, T obj, ArrayInjectPlace place = ArrayInjectPlace.Top)
        {
            T[] list2 = new T[list.Count() + 1];
            int index = 0;
            foreach (T t in list)
            {
                list2[place == ArrayInjectPlace.Bottom ? index : index + 1] = t;
            }
            list2[place == ArrayInjectPlace.Bottom ? list.Count() : 0] = obj;
            return list2;
        }

        /// <summary>
        /// 将数组合并成为一个字符串
        /// </summary>
        public static string Join<T>(this IEnumerable<T> list, char? c = ',')
        {
            return list.Join(c.ToString());
        }

        public static string Join<T>(this IEnumerable<T> list, string split)
        {
            return string.Join(split, list);
        }
        /// <summary>
        /// 按指定条件过滤数组
        /// </summary>
        /// <param name="ac">默认为过滤重复</param>
        public static IEnumerable<T> Filter<T>(this IEnumerable<T> list, ArrayFilterRule filterRule = ArrayFilterRule.NoRepeater)
        {
            List<T> list2 = new List<T>();
            foreach (T t in list)
            {
                if (!list2.Contains(t)) list2.Add(t);
            }
            return list2;
        }
        /// <summary>
        /// 合并数组 并且去除重复项
        /// </summary>
        /// <param name="converter">要比较的字段</param>
        public static List<T> Merge<T, TOutput>(this IEnumerable<T> objList, Converter<T, TOutput> converter, params IEnumerable<T>[] objs)
        {
            List<T> list = objList.ToList();
            foreach (var obj in objs)
            {
                list.AddRange(obj.ToList().FindAll(t => !list.Exists(t1 => converter(t1).Equals(converter(t)))));
            }
            return list;
        }



        /// <summary>
        /// 获取数组的索引项。 如果超出则返回类型的默认值
        /// </summary>
        public static T GetIndex<T>(this IEnumerable<T> list, int index)
        {
            if (list == null || index >= list.Count() || index < 0) return default(T);
            return list.ToArray()[index];
        }

        /// <summary>
        /// 把数组所有元素，按照“参数=参数值”的模式用“＆”字符拼接成字符串
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="list"></param>
        public static string ToQueryString<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> list)
        {
            return list.ToList().ConvertAll(t => string.Format("{0}={1}", t.Key, t.Value)).Join("&");
        }

        /// <summary>
        /// 除去数组中的空值和指定名称的参数并以字母a到z的顺序排序
        /// </summary>
        /// <param name="filter">过滤规则 默认做为空判断</param>
        public static IEnumerable<KeyValuePair<TKey, TValue>> Filter<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> list, Func<TKey, TValue, bool> filter = null)
        {
            if (filter == null)
            {
                filter = (key, value) =>
                {
                    return !string.IsNullOrEmpty(key.ToString()) && value != null;
                };
            }
            foreach (var item in list)
            {
                if (filter(item.Key, item.Value))
                    yield return new KeyValuePair<TKey, TValue>(item.Key, item.Value);
            }
        }

        /// <summary>
        /// 不包含指定的Key
        /// </summary>
        public static IEnumerable<KeyValuePair<TKey, TValue>> Filter<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> list, params TKey[] filter)
        {
            return list.Filter((key, value) =>
            {
                return !string.IsNullOrEmpty(key.ToString()) && value != null && !filter.Contains(key);
            });
        }

        /// <summary>
        /// 按照Key从小大大拍列
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="list"></param>
        public static void Sort<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> list)
        {
            list = list.ToList().OrderBy(t => t.Key);
        }

        /// <summary>
        /// 获取父级的继承树（最多支持32级）
        /// 包括自己
        /// </summary>
        /// <typeparam name="TValue">主键类型</typeparam>
        /// <param name="obj">要查找的数组对象</param>
        /// <param name="id">当前对象的主键值</param>
        /// <param name="value">主键字段</param>
        /// <param name="parent">父级字段</param>
        /// <returns></returns>
        public static List<T> GetParent<T, TValue>(this List<T> obj, TValue id, Func<T, TValue> value, Func<T, TValue> parent)
        {
            int count = 0;
            T t = obj.Find(m => value.Invoke(m).Equals(id));
            List<T> list = new List<T>();
            while (t != null)
            {
                if (count > 32) break;
                list.Add(t);
                t = obj.Find(m => value.Invoke(m).Equals(parent.Invoke(t)));
                count++;
            }
            return list;
        }

        /// <summary>
        /// 获取子集列表（包括自己）
        /// </summary>
        public static void GetChild<T, TValue>(this List<T> obj, TValue id, Func<T, TValue> value, Func<T, TValue> parent, ref List<T> list)
        {
            if (list == null) list = new List<T>();
            var objT = obj.Find(t => value.Invoke(t).Equals(id));
            if (objT != null)
            {
                list.Add(objT);
                foreach (T t in obj.FindAll(m => parent.Invoke(m).Equals(id)))
                {
                    obj.GetChild(value.Invoke(t), value, parent, ref list);
                }
            }
        }

        /// <summary>
        /// 获取树形结构的子集执行方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="obj">当前对象</param>
        /// <param name="id">当前父节点</param>
        /// <param name="value">获取主键的委托</param>
        /// <param name="parent">获取父值的委托</param>
        /// <param name="action">委托执行的方法 int 为当前的深度</param>
        /// <param name="depth">当前的深度</param>
        public static void GetTree<T, TValue>(this List<T> obj, TValue id, Func<T, TValue> value, Func<T, TValue> parent, Action<T, int> action, int depth = 0)
        {
            foreach (T t in obj.FindAll(m => parent.Invoke(m).Equals(id)))
            {
                action.Invoke(t, depth + 1);
                obj.GetTree(value.Invoke(t), value, parent, action, depth + 1);
            }
        }


    }
    /// <summary>
    /// 数组过滤规则
    /// </summary>
    public enum ArrayFilterRule
    {
        /// <summary>
        /// 过滤重复
        /// </summary>
        NoRepeater
    }

    /// <summary>
    /// 插入数组的位置
    /// </summary>
    public enum ArrayInjectPlace
    {
        /// <summary>
        /// 在顶部插入
        /// </summary>
        Top,
        /// <summary>
        /// 在尾部追加
        /// </summary>
        Bottom
    }
}