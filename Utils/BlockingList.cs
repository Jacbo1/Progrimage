//using System.Runtime.CompilerServices;

//namespace Progrimage.Utils
//{
//    public class BlockingList<T> : List<T>
//    {
//        [MethodImpl(MethodImplOptions.Synchronized)]
//        public new void Add(T item) => base.Add(item);

//        [MethodImpl(MethodImplOptions.Synchronized)]
//        public new void Remove(T item) => base.Remove(item);

//        [MethodImpl(MethodImplOptions.Synchronized)]
//        public new void RemoveAt(int index) => base.RemoveAt(index);

//        public new T this[int index]
//        {
//            [MethodImpl(MethodImplOptions.Synchronized)]
//            get => base[index];

//            [MethodImpl(MethodImplOptions.Synchronized)]
//            set => base[index] = value;
//        }
//    }
//}
