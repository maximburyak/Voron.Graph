﻿using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using Voron.Graph.Extensions;
using Voron.Graph.Impl;

namespace Voron.Graph
{
    public class GraphStorage : IDisposable
    {
        private readonly StorageEnvironment _storageEnvironment;
        private readonly string _nodeTreeName;
        private readonly string _edgeTreeName;
        private readonly string _disconnectedNodesTreeName;
        private readonly string _keyByEtagTreeName;
        private readonly string _graphMetadataKey;
        private readonly string _graphName;

        private long _nextId;

        public GraphStorage(string graphName, StorageEnvironment storageEnvironment)
        {
            if (String.IsNullOrWhiteSpace(graphName)) throw new ArgumentNullException("graphName");
            if (storageEnvironment == null) throw new ArgumentNullException("storageEnvironment");
            _graphName = graphName;
            _nodeTreeName = graphName + Constants.NodeTreeNameSuffix;
            _edgeTreeName = graphName + Constants.EdgeTreeNameSuffix;
            _disconnectedNodesTreeName = graphName + Constants.DisconnectedNodesTreeNameSuffix;
            _keyByEtagTreeName = graphName + Constants.KeyByEtagTreeNameSuffix;

            _storageEnvironment = storageEnvironment;
            _graphMetadataKey = graphName + Constants.GraphMetadataKeySuffix;

            CreateConventions();
            CreateSchema();
            CreateCommandAndQueryInstances();
            _nextId = GetLatestStoredNodeKey();
        }

        public GraphStorage CreateSubStorage(string graphName)
        {            
            return new GraphStorage(_graphName + "_" + graphName, _storageEnvironment);
        }

        public Transaction NewTransaction(TransactionFlags flags, TimeSpan? timeout = null)
        {
            var voronTransaction = _storageEnvironment.NewTransaction(flags, timeout);
            return new Transaction(voronTransaction, 
                _nodeTreeName, 
                _edgeTreeName, 
                _disconnectedNodesTreeName, 
                _keyByEtagTreeName, 
                _graphMetadataKey,
                _nextId);
        }

        private long GetLatestStoredNodeKey()
        {
            using(var tx = _storageEnvironment.NewTransaction(TransactionFlags.Read))
            {
                var tree = tx.ReadTree(_nodeTreeName);
                using(var iterator = tree.Iterate())
                {
                    if (!iterator.Seek(Slice.AfterAllKeys))
                        return 0;

                    return iterator.CurrentKey.CreateReader().ReadBigEndianInt64();
                }
            }
        }     

        private void CreateConventions()
        {
            Conventions = new Conventions
            {
                GetNextNodeKey = () => Interlocked.Increment(ref _nextId)
            };
        }

        public Conventions Conventions { get; private set; }

        public GraphCommands Commands { get; private set; }

        public GraphQueries Queries { get; private set; }

        public GraphAdminQueries AdminQueries { get; private set; }

        private void CreateSchema()
        {
            using (var tx = _storageEnvironment.NewTransaction(TransactionFlags.ReadWrite))
            {
                _storageEnvironment.CreateTree(tx, _nodeTreeName);
                _storageEnvironment.CreateTree(tx, _edgeTreeName);
                _storageEnvironment.CreateTree(tx, _disconnectedNodesTreeName);
                _storageEnvironment.CreateTree(tx, _keyByEtagTreeName);

                if(tx.State.Root.ReadVersion(_graphMetadataKey) == 0)
                    tx.State.Root.Add(_graphMetadataKey, (new JObject()).ToStream());

                tx.Commit();
            }
        }

        public void CreateCommandAndQueryInstances()
        {
            AdminQueries = new GraphAdminQueries();
            Queries = new GraphQueries();
            Commands = new GraphCommands(Queries, Conventions);
        }

        public void Dispose()
        {
        }
    }
}
