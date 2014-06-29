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

                                var newNode = new Node(nodeIterator.CurrentKey.CreateReader().ReadBigEndianInt64(), value, etag);
                                _disjointGraph.Commands.CreateNode(tr,newNode.Key, value);
                                _disjointGraph.Commands.CreateEdgeBetween(tr, newNode, newNode);
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
            var newNode = _disjointGraph.Commands.CreateNode(tx, newKey, JObject.FromObject(new { Key = newKey}));
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
        }

        public long Find(long initialKey)
        {
            using (var tx = _disjointGraph.NewTransaction(TransactionFlags.Read))
            {
                return this.Find(tx, initialKey);
            }
        }

        public long Find(Transaction tx, long initialKey)
        {
            long parentKey = -1;
            long currentKey = initialKey;
            Node curNode;
            do
            {
                curNode = _disjointGraph.Queries.LoadNode(tx, currentKey);
                curNode.Data["Parent"]
            }
            while (parentKey != currentKey);

            return currentKey;
        }
    }
}
