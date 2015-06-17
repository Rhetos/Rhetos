using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Dom.DefaultConcepts
{
    /// <summary>
    /// When saving an entity with lazy load enabled, the new records need to be detached in order to allow using
    /// the old data in Save function before the new records are written to the database.
    /// Detaching a record will clear all references from other records to the record to null. This interface will allow overriding that behavior.
    /// </summary>
    public interface IDetachOverride
    {
        bool Detaching { get; set; }
    }
}
