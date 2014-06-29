using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voron.Graph.Extensions;
using Voron.Util.Conversion;

namespace Voron.Graph.DataStructures.DisjointSet
{
    public class UnionByRankNPathCompressionDisjointSet:BaseDisjointSet,IDisjointSet
    {

        public UnionByRankNPathCompressionDisjointSet(GraphStorage baseGraph, ushort edgeType):base( baseGraph)
        {

        }

        public void InitEdges()
        {
            using (var baseGraphTransaction = _baseGraph.NewTransaction(TransactionFlags.Read))
            using (var disJointsGraphTransaction = _disjointGraph.NewTransaction(TransactionFlags.ReadWrite))
            {                
                // if there are already edges in the graph, initialize the disjointGraph
                if (baseGraphTransaction.EdgeTree.State.EntriesCount > 0)
                {
                    using (var edgeIterator = baseGraphTransaction.EdgeTree.Iterate())
                    {
                        edgeIterator.Seek(Slice.BeforeAllKeys);

                        do
                        {
                            EdgeTreeKey edgeKey = edgeIterator.CurrentKey.ToEdgeTreeKey();
                            this.Union(edgeKey.NodeKeyFrom, edgeKey.NodeKeyTo);
                        }
                        while (edgeIterator.MoveNext());
                    }
                }
            }
        }       

        public void InitNodes()
        {
            using (var baseTrans = _baseGraph.NewTransaction(TransactionFlags.Read))
            {
                if (baseTrans.NodeCount == 0)
                {
                    throw new ArgumentException("target graph must have nodes");
                }

                using (var tr = _disjointGraph.NewTransaction(TransactionFlags.ReadWrite))
                {
                    using (var nodeIterator = baseTrans.NodeTree.Iterate())
                    {
                        nodeIterator.Seek(Slice.BeforeAllKeys);

                        do
                        {
                            var readResult = nodeIterator.CreateReaderForCurrent();
                            using (var readResultAsStream = readResult.AsStream())
                            {
                                Etag etag;
                                JObject value;
                                Util.EtagAndValueFromStream(readResultAsStream, out etag, out value);

                                Make(tr, nodeIterator.CurrentKey.CreateReader().ReadBigEndianInt64());
                                
                            }
                        }
                        while (nodeIterator.MoveNext());
                    }                    
                }
            }
        }

        // optioinal
        public void Make(long newKey)
        {
            using (var tx = _disjointGraph.NewTransaction(TransactionFlags.ReadWrite))
            {
                this.Make(tx,newKey);
                tx.Commit();
            }
        }

        // optioinal
        public void Make(Transaction tx,long newKey)
        {
            var newNode = _disjointGraph.Commands.CreateNode(tx, newKey, JObject.FromObject(new { Rank = 0}));
            _disjointGraph.Commands.CreateEdgeBetween(tx, newNode, newNode);
        }

        public void Union(long fromKey, long toKey)
        {
            using (var tx = _disjointGraph.NewTransaction(TransactionFlags.ReadWrite))
            {
                this.Union(tx, fromKey, toKey);
                tx.Commit();
            }
        }
        
        public void Union(Transaction tx, long fromKey, long toKey)
        {
            throw new NotImplementedException();

            Edge fromKeyEdgeToParent, toKeyEdgeToParent;
            Node fromKeyNode, toKeyNode;
            int fromKeyRootRank = 0, toKeyRootRank = 0;

            var fromKeyRoot = Find(tx, fromKey, out fromKeyEdgeToParent, out fromKeyNode);
            var toKeyRoot = Find(tx, toKey, out toKeyEdgeToParent, out fromKeyNode);
            if (fromKeyRoot.Key == toKeyRoot.Key)
            {
                return;
            }
            
            fromKeyRootRank = (int)fromKeyRoot.Data["Rank"];
            toKeyRootRank = (int)toKeyRoot.Data["Rank"];
            
            if ( fromKeyRootRank< toKeyRootRank)
            {
                _disjointGraph.Commands.Delete(tx,fromKeyEdgeToParent);

                fromKeyEdgeToParent.Key.NodeKeyTo = toKeyRoot.Key;
                _disjointGraph.Commands.CreateEdgeBetween(tx, fromKeyNode, toKeyNode);
            }
            else if (fromKeyRootRank< toKeyRootRank)
            {
                _disjointGraph.Commands.Delete(tx, toKeyEdgeToParent);
                toKeyEdgeToParent.Key.NodeKeyTo = fromKeyRoot.Key;
                _disjointGraph.Commands.CreateEdgeBetween(tx, toKeyNode, fromKeyNode);
            }
            else
            {
                _disjointGraph.Commands.Delete(tx, toKeyEdgeToParent);
                toKeyEdgeToParent.Key.NodeKeyTo = fromKeyRoot.Key;
                _disjointGraph.Commands.CreateEdgeBetween(tx, toKeyNode, fromKeyNode);
                fromKeyNode.Data["Rank"] = fromKeyRootRank + 1;

                _disjointGraph.Commands.TryUpdate(tx,fromKeyNode);
            }            
        }

        public Node Find(long initialKey, out Edge edgeToFirstParent, out Node firstNode)
        {
            using (var tx = _disjointGraph.NewTransaction(TransactionFlags.Read))
            {
                return this.Find(tx, initialKey, out edgeToFirstParent, out firstNode);
            }
        }

        public Node Find(Transaction tx, long initialKey, out Edge edgeToFirstParent, out Node firstNode)
        {
            long parentKey = -1;
            long currentKey = initialKey;
            Node curNode;
            edgeToFirstParent = null;
            firstNode = null;
            do
            {
                curNode = _disjointGraph.Queries.LoadNode(tx, currentKey);
                var parent = _disjointGraph.Queries.GetAdjacentOf(tx,curNode).FirstOrDefault();

                if (firstNode != null)
                {
                    firstNode = curNode;
                }
                if (parent!= null)
                {
                   parentKey =  parent.Node.Key;

                   if (edgeToFirstParent!= null)
                   {
                       edgeToFirstParent = parent.EdgeTo;
                   }
                }
                else
                {
                    throw new ArgumentException("disjoint set hierarchy must end with node pointing to itself");
                }
            }
            while (parentKey != currentKey);

            return curNode;
        }
    }
}
