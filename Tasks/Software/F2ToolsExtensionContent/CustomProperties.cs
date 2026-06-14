using F2Tool.Common;
using F2Tool.Common.Exceptions;
using F2Tool.Core.Items.HardcodedItemClasses;
using F2Tool.Core.Items.HardcodedItemClasses.Types;
using F2Tool.Core.Items.HardcodedItemClasses.Types.Enumerations;
using System.Collections;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace F2Tool.Core.Items
{
  public interface IPropertiesDictionary: IReadOnlyDictionary<IPropertyDescription, object?>
  {
    IItem Owner { get; }

    object? GetValue(IPropertyDescription propertyInfo);
    T? GetValue<T>(IPropertyDescription propertyInfo);
    T? GetValue<T>(IdString propertyId);

    void SetValueOf(IdString propertyId, object? value);
    void SetValueOf(IPropertyDescription propertyInfo, object? value);

    void RefreshPropertyList();
  }

  public class CustomProperties : IPropertiesDictionary
  {
    private Dictionary<IdString, IPropertyDescription> _idToPropInfo = new();
    private readonly Dictionary<IPropertyDescription, object?> _propertyValues = new();

    public CustomProperties(IItem owner)
    {
      Owner = owner;
      RefreshPropertyList();
    }

    public void RefreshPropertyList()
    {
      var newPropertyInfos = Owner.Class.GetPropertyDescriptionsIncludingInherited().ToHashSet();
      foreach (var propertyInfo in newPropertyInfos)
      {
        if (!_propertyValues.ContainsKey(propertyInfo))        
          _propertyValues.Add(propertyInfo, PropertyDescription.GetDefaultPropertyValue(propertyInfo.PropertyType, Owner));
      }
      foreach (var (key, _) in _propertyValues.ToArray())
        if (!newPropertyInfos.Contains(key))
          _propertyValues.Remove(key);
      _idToPropInfo = Owner.Class.PropertyDescriptionsIncludingInheritedDictionary;      
    }

    public IItem Owner { get; private set; }

    public void SetValueOf(IdString propertyId, object? value)
    {
      if (!_idToPropInfo.TryGetValue(propertyId, out var info))
        throw new KeyNotFoundException();
      SetValueOf(info, value);
    }

    public void SetValueOf(IPropertyDescription propertyInfo, object? value)
    {      
      if (!_propertyValues.ContainsKey(propertyInfo))
        throw new KeyNotFoundException();

      //if (propertyInfo.IsValidValueForAssignment(value))
      //{
        if (propertyInfo.PropertyType.TypeKind == TypeKinds.List)
        {
          var targetList = _propertyValues[propertyInfo!];
          if (targetList is IList list)
          {
            list.Clear();
            if (value is ICollection collection)
              foreach (var item in collection) list.Add(item);
          }
          else if (targetList is IBasicItemList itemList)
          {
            itemList.Clear();
            if (value is ICollection collection)
              foreach (var item in collection) itemList.Add(item);
          }
          throw new CaseNotImplementedException(targetList?.GetType()?.Name ?? "null");
        }
        else
        {
          _propertyValues[propertyInfo] = value;

          if (value is IItem item && item.BelongsTo is null)
            item.BelongsTo = Owner;
        }
      //}
      //else
      //  throw new F2InvalidValueException(this.Owner.GetFullId(), propertyInfo.Id, value);
    }

    public object? GetValue(IPropertyDescription propertyInfo)
    {
      var value = _propertyValues[propertyInfo];
      if (value is IItemPointer<IItem> lazyLoaderItemReference)
      {
        var item = lazyLoaderItemReference.Item;
        _propertyValues[propertyInfo] = item;
        item.BelongsTo ??= Owner;
        return item;
      }
      else return value;
    }

    public T? GetValue<T>(IdString propertyId)
    {
      if (!_idToPropInfo.TryGetValue(propertyId, out var info))
        throw new InvalidOperationException();

      return GetValue<T>(info);
    }

    public T? GetValue<T>(IPropertyDescription propertyInfo)
    {
      return (T?)GetValue(propertyInfo);
    }

    public object? this[IPropertyDescription key] { get => GetValue(key); set => _propertyValues[key] = value; }

    public IEnumerable<IPropertyDescription> Keys => _propertyValues.Keys;

    public IEnumerable<object?> Values => _propertyValues.Keys.Select(key => GetValue(key));

    public int Count => _propertyValues.Count;


    public bool ContainsKey(IPropertyDescription key) => _propertyValues.ContainsKey(key);

    public bool TryGetValue(IPropertyDescription key, out object? value)
    {
      value = null;
      if (!_propertyValues.ContainsKey(key)) return false;
      value = GetValue(key);
      return true;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<KeyValuePair<IPropertyDescription, object?>> GetEnumerator() => _propertyValues.Keys.Select(key => new KeyValuePair<IPropertyDescription, object?>(key, GetValue(key))).GetEnumerator();

  }
}

