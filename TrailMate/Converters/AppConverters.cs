using System.Globalization;

namespace TrailMate.Converters;

public class InvertBoolConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c) => v is bool b && !b;
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => v is bool b && !b;
}

public class StringToBoolConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c)
        => !string.IsNullOrWhiteSpace(v as string);
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

public class IntToBoolConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c)
    {
        bool has = v is int i && i > 0;
        bool invert = p as string == "invert";
        return invert ? !has : has;
    }
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

public class StringToImageSourceConverter : IValueConverter
{
    public object? Convert(object? v, Type t, object? p, CultureInfo c)
        => v is string path && !string.IsNullOrWhiteSpace(path) && File.Exists(path)
           ? ImageSource.FromFile(path) : null;
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c)
        => v is bool b && b ? 1.0 : 0.4;
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

public class NullToBoolConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c) => v is not null;
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}