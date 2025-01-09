using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DocMonster.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DocMonster.Utilities
{
    public class CamelCaseAndIgnoreEmptyEnumerablesResolver : CamelCasePropertyNamesContractResolver
    {
        public static readonly CamelCaseAndIgnoreEmptyEnumerablesResolver Instance =
            new CamelCaseAndIgnoreEmptyEnumerablesResolver();

        public CamelCaseAndIgnoreEmptyEnumerablesResolver()
        {
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {            
            var property = base.CreateProperty(member, memberSerialization);

            if (property.PropertyType != typeof(string) &&
                typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
            {
                property.ShouldSerialize = instance =>
                {
                    IEnumerable enumerable = null;

                    // this value could be in a public field or public property
                    switch (member.MemberType)
                    {
                        case MemberTypes.Property:
                            enumerable = instance
                                .GetType()
                                .GetProperty(member.Name)
                                .GetValue(instance, null) as IEnumerable;
                            break;
                        case MemberTypes.Field:
                            enumerable = instance
                                .GetType()
                                .GetField(member.Name)
                                .GetValue(instance) as IEnumerable;
                            break;                        
                    }

                    if (enumerable != null)
                    {
                        // check to see if there is at least one item in the Enumerable
                        return enumerable.GetEnumerator().MoveNext();
                    }

                    // if the list is null, we defer the decision to NullValueHandling
                    return true;
                };
            }
            else
            {
                return  base.CreateProperty(member, memberSerialization);
            }

            return property;
        }
    }


    //public class CompositeContractResolver : IContractResolver, IEnumerable<IContractResolver>
    //{
    //    private readonly IList<IContractResolver> _contractResolvers = new List<IContractResolver>();

    //    public JsonContract ResolveContract(Type type)
    //    {
    //        return
    //            _contractResolvers
    //                .Select(x => x.ResolveContract(type))
    //                .FirstOrDefault( x=> x!= null);
    //    }

    //    public void Add([NotNull] IContractResolver contractResolver)
    //    {
    //        if (contractResolver == null) throw new ArgumentNullException(nameof(contractResolver));
    //        _contractResolvers.Add(contractResolver);
    //    }

    //    public IEnumerator<IContractResolver> GetEnumerator()
    //    {
    //        return _contractResolvers.GetEnumerator();
    //    }

    //    IEnumerator IEnumerable.GetEnumerator()
    //    {
    //        return GetEnumerator();
    //    }
    //}
}
