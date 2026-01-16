using System;
using System.ComponentModel;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Clockmaker0.Data;

/// <summary>
/// Wrapper allowing the passing of a property by reference
/// </summary>
/// <typeparam name="T">The type of the property</typeparam>
public class ReferenceProperty<T> : INotifyPropertyChanged
{
    private readonly Action<T> _setter;
    private readonly Func<T> _getter;
    private readonly string _propertyName = "";

    /// <summary>
    /// Creates a reference property from a function that returns a property or field
    /// </summary>
    /// <param name="expr">Expression that returns a property or field</param>
    /// <param name="propertyChangedObject">Parent instance if it implements INotifyPropertyChanged</param>
    public ReferenceProperty(Expression<Func<T>> expr, INotifyPropertyChanged? propertyChangedObject = null)
    {
        Type type = typeof(T);
        MemberExpression memberExpression = (MemberExpression)expr.Body;
        Expression? instanceExpression = memberExpression.Expression;
        ParameterExpression parameter = Expression.Parameter(type);

        switch (memberExpression.Member)
        {
            case PropertyInfo propertyInfo:
            {
                MethodInfo? setMethod = propertyInfo.GetSetMethod();
                _setter = setMethod is not null
                    ? Expression.Lambda<Action<T>>(Expression.Call(instanceExpression, setMethod, parameter),
                        parameter).Compile()
                    : t => throw new ReadOnlyException("Property is read only");
                    
                MethodInfo? getMethod = propertyInfo.GetGetMethod();
                _getter = getMethod is not null 
                    ? Expression.Lambda<Func<T>>(Expression.Call(instanceExpression, getMethod)).Compile()
                    : () => throw new AccessViolationException("Property is not readable");
                    
                _propertyName = propertyInfo.Name;
                break;
            }
            case FieldInfo fieldInfo:
            {
                _setter = Expression.Lambda<Action<T>>(Expression.Assign(memberExpression, parameter), parameter).Compile();
                _getter = Expression.Lambda<Func<T>>(Expression.Field(instanceExpression, fieldInfo)).Compile();
                break;
            }
            default:
            {
                throw new ArgumentException("Expression must return a member of an instance");
            }
        }

        if (propertyChangedObject is null)
        {
            return;
        }

        propertyChangedObject.PropertyChanged += PropertyChangedObject_PropertyChanged;

    }

    private void PropertyChangedObject_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_propertyName == e.PropertyName)
        {
            PropertyChanged?.Invoke(this, e);
        }
    }

    /// <summary>
    /// Set the value of the wrapped property
    /// </summary>
    /// <param name="value">New value</param>
    public void Set(T value) => _setter(value);

    /// <summary>
    /// Get the value of the wrapped property
    /// </summary>
    /// <returns>The value of the property</returns>
    public T Get() => _getter();


    /// <summary>
    /// Raised when the wrapped property is changed (if the property implements INotifyPropertyChanged
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
}