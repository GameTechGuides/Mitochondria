﻿using Mitochondria.Api.Owner;

namespace Mitochondria.Api.Options;

public interface ICustomOption<TValue> : ICustomOption
    where TValue : notnull
{
    public TValue Value { get; set; }
    
    public TValue DefaultValue { get; }

    public delegate void ValueChangedHandler(ICustomOption customOption, TValue oldValue, TValue newValue);

    public event ValueChangedHandler? OnValueChanged;
}

public interface ICustomOption : IOwned
{
    public StringNames TitleName { get; }
    
    public string Title { get; }
    
    public string ValueString { get; }
    
    public Type ValueType { get; }
    
    public string FormatString { get; }

    public delegate void ChangedHandler(ICustomOption customOption);

    public event ChangedHandler? OnChanged;

    public void ResetValue();

    public bool HasDefaultValue();
}