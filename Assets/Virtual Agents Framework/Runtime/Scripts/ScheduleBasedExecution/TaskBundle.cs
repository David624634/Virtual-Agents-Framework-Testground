using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using i5.VirtualAgents.AgentTasks;
using UnityEngine;

namespace i5.VirtualAgents
{
    /// <summary>
    /// A task which consists of multiple subtasks.
    /// It allows for checking of preconditions and then executing a sequence of tasks.
    /// </summary>
    public class TaskBundle : AgentBaseTask
    {
        /// <summary>
        /// List of tasks to be part of the bundle
        /// </summary>
        public List<AgentBaseTask> TaskQueue { get; private set; }

        /// <summary>
        /// List of conditions to be met before execution of tasks
        /// </summary>
        public List<Func<bool>> Preconditions { get; private set; }

        private bool TaskStartedAndPreconditionsAreChecked = false;

        private Agent executingAgent;

        /// <summary>
        /// Creates an empty TaskBundle
        /// </summary>
        public TaskBundle()
        {
            State = TaskState.Waiting;
            TaskQueue = new List<AgentBaseTask>();
            Preconditions = new List<Func<bool>>();
        }

        /// <summary>
        /// Creates a TaskBundle with a list of subtasks
        /// </summary>
        /// <param name="tasks"></param>
        public TaskBundle(List<AgentBaseTask> tasks)
        {
            State = TaskState.Waiting;
            TaskQueue = tasks;
            Preconditions = new List<Func<bool>>();
        }

        /// <summary>
        /// Creates a TaskBundle with a list of tasks and a list of preconditions
        /// </summary>
        /// <param name="tasks"></param>
        /// <param name="preconditions"></param>
        public TaskBundle(List<AgentBaseTask> tasks, List<Func <bool>> preconditions)
        {
            State = TaskState.Waiting;
            TaskQueue = tasks;
            Preconditions = preconditions;
        }

        /// <summary>
        /// Adds a task to the task queue after initialisation
        /// </summary>
        /// <param name="task">The task to be added to the task queue</param>
        public void AddTask(AgentBaseTask task)
        {
            TaskQueue.Add(task);
        }

        /// <summary>
        /// Adds a precondition to the list of preconditions after initialisation
        /// Note: Preconditions are only checked before the tasks are executed,
        /// a precondition added after the execution has started will not be checked
        /// </summary>
        /// <param name="precondition">The precondition to be added to the list of preconditions</param>
        public void AddPrecondition(Func<bool> precondition)
        {
            if(TaskState.Waiting != State)
            {
                Debug.LogWarning("Preconditions can only be checked before the TaskBundle is started");
            }
            Preconditions.Add(precondition);
        }

        /// <summary>
        /// Check for preconditions and start the execution of all subtasks in sequence
        /// </summary>
        /// <param name="executingAgent"></param>
        public override void StartExecution(Agent executingAgent)
        {
            State = TaskState.Running;
            this.executingAgent = executingAgent;
            if (CheckPreconditions())
            {
                TaskStartedAndPreconditionsAreChecked = true;

                for (var i = 1; i < TaskQueue.Count; i++)
                {
                        for (int j = 0; j < i; j++)
                        {
                            var task = TaskQueue[i];
                            // Reset tasks in case they were already executed
                            // TODO: Reset method is added in BehaviourTree Branch, uncomment after merge
                            //task.Reset();
                            // Adding dependencies to tasks in case later implementations rely on that
                            // Each task depends on all previous tasks
                            task.DependsOnTasks.Add(TaskQueue[j]);

                        }
                }
            }
            else
            {
                Debug.Log("Preconditions of TaskBundle not met");
                StopAsFailed();
                TaskStartedAndPreconditionsAreChecked = false;
            }
        }

        /// <summary>
        /// Execute all tasks in the task queue. If a task fails, the whole bundle fails. Note, that checking of preconditions is not part of this method.
        /// </summary>
        /// <param name="executingAgent"></param>
        public override TaskState EvaluateTaskState()
        {
            if (!TaskStartedAndPreconditionsAreChecked) return State;
            // Check if any task failed
            var failedTask = TaskQueue.FirstOrDefault(task => task.State == TaskState.Failure);
            if (failedTask != null)
            {
                Debug.LogWarning("Task: " + failedTask + " failed");
                StopAsFailed();
                TaskStartedAndPreconditionsAreChecked = false;
                return State;
            }
            // Find the first task that is either running or waiting
            ITask currentTask = TaskQueue.FirstOrDefault(task => task.State is TaskState.Waiting or TaskState.Running);

            // If no task is running, waiting or failed, the bundle is finished
            if(currentTask == null)
            {
                StopAsSucceeded();
                return State;
            }
            currentTask.Tick(executingAgent);
            return State;
        }

        /// <summary>
        /// Check if all preconditions are met.
        /// </summary>
        /// <returns> True if all preconditions evaluate to true, otherwise false. </returns>
        private bool CheckPreconditions()
        {
            bool res = true;
            foreach (Func<bool> condition in Preconditions)
            {
                res = res && condition();
            }
            return res;
        }
    }
}