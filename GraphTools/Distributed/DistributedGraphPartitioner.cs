﻿using GraphTools.Distributed.Machines;
using GraphTools.Distributed.Messages;
using GraphTools.Graph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GraphTools.Distributed
{
    class DistributedGraphPartitioner<TNode, TLabel>
    {
        /// <summary>
        /// Graph to partition.
        /// </summary>
        private MultiDirectedGraph<TNode, TLabel> graph;

        /// <summary>
        /// Number of machines.
        /// </summary>
        private int m;

        /// <summary>
        /// Measures total running time of simulation.
        /// </summary>
        private Stopwatch stopwatch = new Stopwatch();

        /// <summary>
        /// Gets the number of milliseconds it took for the partition computation to complete.
        /// </summary>
        public long ElapsedMilliseconds
        {
            get
            {
                return stopwatch.ElapsedMilliseconds;
            }
        }

        /// <summary>
        /// Number of messages sent across all machines.
        /// </summary>
        private int visitTimes = 0;

        /// <summary>
        /// Gets the number of messages sent across all machines.
        /// </summary>
        public int VisitTimes
        {
            get
            {
                return visitTimes;
            }
        }

        /// <summary>
        /// Total size of all the messages sent.
        /// </summary>
        private int dataShipment = 0;

        /// <summary>
        /// Gets the total size of all the messages sent.
        /// </summary>
        public int DataShipment
        {
            get
            {
                return dataShipment;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="m">Number of machines.</param>
        /// <param name="graph"></param>
        public DistributedGraphPartitioner(int m, MultiDirectedGraph<TNode, TLabel> graph)
        {
            this.graph = graph;
            this.m = m;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IDictionary<TNode, int> ExactBisimulationReduction(Func<MultiDirectedGraph<TNode, TLabel>, int, Dictionary<TNode, int>> splitter)
        {
            IDictionary<TNode, int> distributedPartition = null;
            var segments = splitter(graph, m);
            var workers = ExactBisimulationWorker<TNode, TLabel>.CreateWorkers(graph, segments);
            ExactBisimulationCoordinator<TNode, TLabel> coordinator = null;
            coordinator = new ExactBisimulationCoordinator<TNode, TLabel>((k_max, foundPartition) =>
            {
                // Console.WriteLine("k_max=" + k_max);
                distributedPartition = foundPartition;

                coordinator.Stop();
                foreach (var worker in workers)
                {
                    worker.Stop();
                }
            });

            // Start bisimulation partition computation
            coordinator.SendMe(new CoordinatorMessage(null, workers));

            // Create tasks
            int r = workers.Length;
            var tasks = new Task[r + 1];
            for (int i = 0; i < r; i++)
            {
                tasks[i] = new Task(workers[i].Run);
            }
            tasks[r] = new Task(coordinator.Run);

            stopwatch.Reset();
            stopwatch.Start();
            {
                // Run each task
                foreach (var task in tasks)
                {
                    task.Start();
                }

                // Wait for each task to finish
                foreach (var task in tasks)
                {
                    task.Wait();
                }
            }
            stopwatch.Stop();

            visitTimes = workers.Sum(worker => worker.VisitTimes) + coordinator.VisitTimes;
            dataShipment = workers.Sum(worker => worker.DataShipment) + coordinator.DataShipment;

            return distributedPartition;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IDictionary<TNode, int> EstimateBisimulationReduction(Func<MultiDirectedGraph<TNode,TLabel>, int, Dictionary<TNode, int>> splitter)
        {
            IDictionary<TNode, int> distributedPartition = null;
            var segments = splitter(graph, m);
            var workers = EstimateBisimulationWorker<TNode, TLabel>.CreateWorkers(graph, segments);
            EstimateBisimulationCoordinator<TNode> coordinator = null;
            coordinator = new EstimateBisimulationCoordinator<TNode>((k_max, foundPartition) =>
            {
                // Console.WriteLine("k_max=" + k_max);
                distributedPartition = foundPartition;

                coordinator.Stop();
                foreach (var worker in workers)
                {
                    worker.Stop();
                }
            });

            // Start bisimulation partition computation
            coordinator.SendMe(new CoordinatorMessage(null, workers));

            // Create tasks
            int r = workers.Length;
            var tasks = new Task[r + 1];
            for (int i = 0; i < r; i++)
            {
                tasks[i] = new Task(workers[i].Run);
            }
            tasks[r] = new Task(coordinator.Run);

            stopwatch.Reset();
            stopwatch.Start();
            {
                // Run each task
                foreach (var task in tasks)
                {
                    task.Start();
                }

                // Wait for each task to finish
                foreach (var task in tasks)
                {
                    task.Wait();
                }
            }
            stopwatch.Stop();

            visitTimes = workers.Sum(worker => worker.VisitTimes) + coordinator.VisitTimes;
            dataShipment = workers.Sum(worker => worker.DataShipment) + coordinator.DataShipment;

            return distributedPartition;
        }
    }
}
