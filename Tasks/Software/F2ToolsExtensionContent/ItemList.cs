using F2Tool.Common.Enumerations;
using F2Tool.Common.Utils;
using F2Tool.Core.Items.HardcodedItemClasses;
using System.Collections;

namespace F2Tool.Core.Items
{
  public interface IBasicItemList
  {
    void Add(object o);
    void Clear();
  }

  public interface IItemListRemover
  {
    void RemoveAll(IItem item);
  }  

  public interface IItemList<TItem>: ICollection<TItem>, IReadOnlyCollection<TItem>, IItemListRemover, IBasicItemList, ICollection
    where TItem : IItem
  {
    CollectionFlags ListOptions { get; }
    IItem PartOfItem { get; }

    event Action<ItemListChangeType, TItem>? CollectionChanged;

    void AddRange(IEnumerable<TItem> items);
    void RemoveAll(TItem item);
    void ResetTo(IEnumerable<TItem> items);
  }

  public class ItemList<TIItem> : IItemList<TIItem> 
    where TIItem : IItem
  {
    public IItem PartOfItem { get; }
    public CollectionFlags ListOptions { get; }

    private readonly ICollection<TIItem> _items;
    private bool _suppressEvents;

    public ItemList(IItem partOfItem, CollectionFlags options = CollectionFlags.None, IEqualityComparer<TIItem>? comparer = null)
    {
      PartOfItem = partOfItem;
      ListOptions = options;

      if (options.HasFlag(CollectionFlags.NoDuplicates))
        _items = new OrderedSet<TIItem>(comparer ?? EqualityComparer<TIItem>.Default);
      else _items = new List<TIItem>();
    }

    public event Action<ItemListChangeType, TIItem>? CollectionChanged;

    protected virtual void OnCollectionChanged(ItemListChangeType changeType, TIItem item)
    {
      if (!_suppressEvents && CollectionChanged is not null)
        CollectionChanged(changeType, item);
    }

    public void ResetTo(IEnumerable<TIItem> items)
    {
      Clear();
      AddRange(items);
    }

    public void AddRange(IEnumerable<TIItem> items)
    {

      foreach (var item in items)
        Add(item);
    }

    public void RemoveAll(IItem item)
    {
      if (item is TIItem titem)
        RemoveAll(titem);
    }

    public int Count => _items.Count;

    public bool IsReadOnly => false;

    public bool IsSynchronized => false;

    private readonly object _syncRoot = new();
    public object SyncRoot => _syncRoot;

    void ICollection<TIItem>.Add(TIItem item)
    {
      Add(item);
    }

    public bool Add(TIItem item)
    {
      if (_items is OrderedSet<TIItem> orderedSet)
      {
        if (!orderedSet.Add(item))
          return false;
      }
      else _items.Add(item);

      if (item.IsBelongsToEmptyRef) 
        item.BelongsTo = PartOfItem;
      item.InItemLists.Add(this);
      OnCollectionChanged(ItemListChangeType.Add, item);
      return true;
    }

    public bool Remove(TIItem item)
    {
      item.InItemLists.Remove(this);
      var result = _items.Remove(item);
      OnCollectionChanged(ItemListChangeType.Remove, item);
      return result;
    }

    public void RemoveAll(TIItem item)
    {
      item.InItemLists.Remove(this);
      while (_items.Remove(item)) ;
      OnCollectionChanged(ItemListChangeType.Remove, item);
    }

    public void Clear()
    {
      foreach (var item in _items.ToArray())
        RemoveAll(item);
    }

    public bool Contains(TIItem item) => _items.Contains(item);

    public void CopyTo(TIItem[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

    public IEnumerator<TIItem> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void SupressEvents() => _suppressEvents = true;

    internal void ResumeEvents() => _suppressEvents = false;

    public void Add(object o)
    {
      if (o is not TIItem item)
        throw GetArgumentExceptionOfInvalidType(o);
      _items.Add(item);
    }

    //public TItem this[int index] { get => _items[index]; set => _items[index] = value; }

    //public int IndexOf(TItem item) => _items.IndexOf(item);

    //public void Insert(int index, TItem item)
    //{
    //  item.BelongsTo ??= PartOfItem;
    //  _items.Insert(index, item);
    //  //item.InItemLists.Add(this);
    //}

    //public void RemoveAt(int index) => _items.RemoveAt(index);

    //#region IList members

    //public bool IsFixedSize => false;

    //public bool IsSynchronized => false;

    //public object SyncRoot => this;

    private static ArgumentException GetArgumentExceptionOfInvalidType(object? value) => new($"The value \"{value}\" is not of type \"{typeof(TIItem)}\" and cannot be used in this generic collection.", nameof(value));

    public void CopyTo(Array array, int index)
    {
      throw new NotImplementedException();
    }

    //object? IList.this[int index]
    //{
    //  get => _items[index]; 
    //  set
    //  {
    //    if (value is TItem item) _items[index] = item;
    //    else throw GetArgumentExceptionOfInvalidType(value);
    //  }
    //}

    //public int Add(object? value)
    //{
    //  if (value is not TItem item) throw GetArgumentExceptionOfInvalidType(value); 

    //  _items.Add(item);
    //  return _items.Count - 1;
    //}

    //public bool Contains(object? value)
    //{
    //  if (value is not TItem item) 
    //    throw GetArgumentExceptionOfInvalidType(value);
    //  return _items.Contains(item);
    //}

    //public int IndexOf(object? value)
    //{
    //  if (value is not TItem item)
    //    throw GetArgumentExceptionOfInvalidType(value);
    //  return _items.IndexOf(item);
    //}

    //public void Insert(int index, object? value)
    //{
    //  if (value is not TItem item)
    //    throw GetArgumentExceptionOfInvalidType(value);
    //  _items.Insert(index, item);
    //}

    //public void Remove(object? value)
    //{
    //  if (value is not TItem item)
    //    throw GetArgumentExceptionOfInvalidType(value);
    //  _items.Remove(item);
    //}

    //public void CopyTo(Array array, int index)
    //{
    //  var targetIndex = index;
    //  foreach (var item in _items) array.SetValue(item, targetIndex++);
    //}

    //#endregion
  }
}
