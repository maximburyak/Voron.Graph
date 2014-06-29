using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph.DataStructures.DisjointSet
{
    public class BaseDisjointSet 
    {
        protected GraphStorage _baseGraph;
        protected GraphStorage _disjointGraph;        
        public BaseDisjointSet(GraphStorage baseGraph)
        {
            _baseGraph = baseGraph;            
        }

        public virtual void Initialize()
        {
            _disjointGraph = _baseGraph.CreateSubStorage("DisJointSet");            
        }                
    }
}
