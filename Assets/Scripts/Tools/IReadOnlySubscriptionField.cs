using System;

public interface IReadOnlySubscriptionField<T>
{
    public T Value { get; }
    public void Subscribe(Action<T> func);
    public void UnSubscribe(Action<T> func);
}