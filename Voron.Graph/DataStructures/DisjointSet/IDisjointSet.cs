using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph.DataStructures.DisjointSet
{
    public interface IDisjointSet
    {
        void Make(long fromId);
        void Union(long fromId,long toId);
        long Find(long seekedId);        
        void InitEdges();
        void InitNodes();
    }
}
