using System;

public class SubscriptionField<T> : IReadOnlySubscriptionField<T>
{
    bool isNewValue;

    public virtual T Value
    {
        get => _value;
        set
        {
            if (_value == null || !_value.Equals(value))
            {
                _value = value;
                evtChange.Invoke(value);
            }
        }
    }

    public virtual T ValueForce
    {
        get => _value;
        set
        {
            _value = value;
            evtChange.Invoke(value);
        }
    }

    public virtual T ValueSilent
    {
        get => _value;
        set => _value = value;
    }

    private T _value;
    private event Action<T> evtChange = delegate { };

    public void Subscribe(Action<T> func)
    {
        evtChange += func;
    }

    public void UnSubscribe(Action<T> func)
    {
        evtChange -= func;
    }

    public void ClearSubscribe()
    {
        evtChange = delegate { };
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}