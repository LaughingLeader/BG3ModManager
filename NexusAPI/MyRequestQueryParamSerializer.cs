using System.Collections.Generic;
using RestEase;

namespace NexusAPI
{
    public sealed class MyRequestQueryParamSerializer: RequestQueryParamSerializer
    {
        public override IEnumerable<KeyValuePair<string, string>> SerializeQueryParam<T>(string name, T value, RequestQueryParamSerializerInfo info)
        {
            if (name == "include_unapproved" && typeof(T) == typeof(bool))
            {
                yield return new KeyValuePair<string, string>(name, value.Equals(true) ? "1" : "0");
            }
            else
            {
                foreach (var kv in base.SerializeQueryParam(name, value, info))
                {
                    yield return kv;
                }
            }
        }
    }
}