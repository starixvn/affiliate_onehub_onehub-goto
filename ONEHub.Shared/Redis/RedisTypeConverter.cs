using StackExchange.Redis;

namespace ONEHub.Shared.Redis;

public static class RedisTypeConverter
{
    private static MessagePackSerializer serializer = new MessagePackSerializer();

    public static RedisValue ToRedisValue(object? value)
    {
        if (value == null)
        {
            return RedisValue.Null;
        }

        if (typeof(byte[]) == value.GetType())
        {
            return (byte[])value;
        }

        return serializer.Serialize(value);
    }

    public static T? ToObject<T>(RedisValue value)
    {
        if (value.IsNull)
        {
            return default;
        }

        var binary = (byte[]?)value;
        if (binary == null)
        {
            return default;
        }

        if (typeof(T) == typeof(byte[]))
        {
            return (T)Convert.ChangeType(binary, typeof(T));
        }

        return serializer.Deserialize<T>(binary);
    }
}
