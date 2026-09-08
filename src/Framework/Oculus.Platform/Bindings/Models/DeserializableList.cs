// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using Newtonsoft.Json;
using Oculus.Platform;
using System.Collections.Generic;
[JsonConverter(typeof(DeserializableListConverter))]
public class DeserializableList<T>
{
    public DeserializableList()
    {
        Data = new List<T>();
        Paging = new PagingJSON();
        Summary = new SummaryJSON();
    }

    [JsonProperty("data")]
    public List<T> Data { get; set; }

    [JsonProperty("paging")]
    public PagingJSON Paging { get; set; }

    [JsonProperty("summary")]
    public SummaryJSON Summary { get; set; }


    // Added to enable running foreach on this class which dev frequently uses.
    // Making LeaderboarEntryList implements IEnumerator directly breaks json deserialization.
    // C# compiler supports patterned based foreach loop, hence adding just this is enough.
    public IEnumerator<T> GetEnumerator()
    {
        if (Data == null)
        {
            PlatformLog.LogError("[DeserializableList] Data is NULL in GetEnumerator!");
            Data = new List<T>();
        }
        return Data.GetEnumerator();
    }

    public T this[int index]
    {
        get
        {
            if (Data == null)
            {
                PlatformLog.LogError("[DeserializableList] Data is NULL in indexer!");
                Data = new List<T>();
            }
            return Data[index];
        }
        set
        {
            if (Data == null)
            {
                PlatformLog.LogError("[DeserializableList] Data is NULL in indexer setter!");
                Data = new List<T>();
            }
            Data[index] = value;
        }
    }

    public int Count
    {
        get
        {
            if (Data == null)
            {
                PlatformLog.LogError("[DeserializableList] Data is NULL in Count!");
                Data = new List<T>();
            }
            return Data.Count;
        }
    }

    public int IndexOf(T item)
    {
        return Data.IndexOf(item);
    }

    public void Add(T item)
    {
        Data.Add(item);
    }

    public void Clear()
    {
        Data.Clear();
    }

    public bool Contains(T item)
    {
        return Data.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        Data.CopyTo(array, arrayIndex);
    }

    public void Insert(int index, T item)
    {
        Data.Insert(index, item);
    }

    public bool Remove(T item)
    {
        return Data.Remove(item);
    }

    public void RemoveAt(int index)
    {
        Data.RemoveAt(index);
    }

    public bool HasNextPage
    {
        get { return !System.String.IsNullOrEmpty(Paging?.next); }
    }

    public bool HasPreviousPage
    {
        get { return !System.String.IsNullOrEmpty(Paging?.previous); }
    }

    public string NextUrl
    {
        get { return Paging?.next; }
    }

    public string PreviousUrl
    {
        get { return Paging?.previous; }
    }

    /// <summary>
    /// The total count of entries across all pages, from the summary field.
    /// </summary>
    public ulong TotalCount
    {
        get { return Summary?.total_count ?? 0; }
    }
}
public class PagingJSON
{
    public string next;
    public string previous;
    public CursorsJson cursors;
}
public class SummaryJSON
{
    public ulong total_count;
}
public class CursorsJson
{
    public string before;
    public string after;
}
