using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Dom.DefaultConcepts
{
    [DataContract]
    public abstract class EntityBase<T> : IEntity, IEquatable<T> where T : class, IEntity
    {
        [DataMember]
        public Guid ID { get; set; }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public override bool Equals(object o)
        {
            var other = o as T;
            return other != null && other.ID == ID;
        }

        public bool Equals(T other)
        {
            return other != null && other.ID == ID;
        }
    }
}
