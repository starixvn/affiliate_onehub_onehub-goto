using MessagePack.Resolvers;
using MP = MessagePack;

namespace ONEHub.Shared.Redis;

public class MessagePackSerializer
{
    public byte[] Serialize(object obj)
    => MP.MessagePackSerializer.Serialize(obj, ContractlessStandardResolver.Options);

    public T? Deserialize<T>(byte[] binaryData)
    {
        try
        {
            return MP.MessagePackSerializer.Deserialize<T>(binaryData, ContractlessStandardResolver.Options);
        }
        catch
        {
            return default;
        }
    }
}
