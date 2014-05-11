﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voron.Impl;

namespace Voron.Graph
{
    public class GraphEnvironment
    {
        private readonly StorageEnvironment _storageEnvironment;
        private readonly string _nodeTreeName;
        private readonly string _edgeTreeName;
        private readonly string _disconnectedNodesTreeName;

        public GraphEnvironment(string graphName, StorageEnvironment storageEnvironment)
        {
            if (String.IsNullOrWhiteSpace(graphName)) throw new ArgumentNullException("graphName");
            if (storageEnvironment == null) throw new ArgumentNullException("storageEnvironment");
            _nodeTreeName = graphName + Constants.NodeTreeNameSuffix;
            _edgeTreeName = graphName + Constants.EdgeTreeNameSuffix;
            _disconnectedNodesTreeName = graphName + Constants.DisconnectedNodesTreeName;
            _storageEnvironment = storageEnvironment;

            CreateSchema();
        }

        public ISession OpenSession()
        {
            return new Session(_storageEnvironment.CreateSnapshot(),
                _nodeTreeName,
                _edgeTreeName,
                _disconnectedNodesTreeName,
                wb => _storageEnvironment.Writer.Write(wb));
        }

        private void CreateSchema()
        {
            using (var tx = _storageEnvironment.NewTransaction(TransactionFlags.ReadWrite))
            {
                _storageEnvironment.CreateTree(tx, _nodeTreeName);
                _storageEnvironment.CreateTree(tx, _edgeTreeName);
                _storageEnvironment.CreateTree(tx, _disconnectedNodesTreeName);
                tx.Commit();
            }
        }
    }
}