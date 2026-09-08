using System.Collections.Generic;

namespace Sensus.Captions;

// Fixed FIFO admission: adding one source never invalidates every other source.
internal sealed class BoundedCache<TKey,TValue> where TKey : notnull
{
    private readonly Dictionary<TKey,TValue> values;
    private readonly Queue<TKey> order;
    private readonly int capacity;
    internal BoundedCache(int capacity)
    {
        if(capacity<=0) throw new System.ArgumentOutOfRangeException(nameof(capacity));
        this.capacity=capacity; values=new(capacity); order=new(capacity);
    }
    internal int Count => values.Count;
    internal bool TryGetValue(TKey key,out TValue value) => values.TryGetValue(key,out value!);
    internal bool TryTakeOldest(out TValue value)
    {
        if(values.Count<capacity) { value=default!; return false; }
        var key=order.Dequeue(); value=values[key]; values.Remove(key); return true;
    }
    internal void Set(TKey key,TValue value)
    {
        if(!values.ContainsKey(key)) { TryTakeOldest(out _); order.Enqueue(key); }
        values[key]=value;
    }
    internal void Clear() { values.Clear(); order.Clear(); }
}
