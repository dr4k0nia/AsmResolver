using System.Collections.Generic;

namespace AsmResolver.Workspaces
{
    /// <summary>
    /// Provides a context in which a workspace analyzer is situated in.
    /// </summary>
    public class AnalysisContext
    {
        /// <summary>
        /// Creates a new instance of the <see cref="AnalysisContext"/> class.
        /// </summary>
        /// <param name="workspace">The parent workspace.</param>
        public AnalysisContext(Workspace workspace)
        {
            Workspace = workspace;
        }

        /// <summary>
        /// Gets the parent workspace that this analysis context is associated with.
        /// </summary>
        public Workspace Workspace
        {
            get;
        }

        /// <summary>
        /// Gets a queue of objects that are scheduled for analysis.
        /// </summary>
        public Queue<object> Agenda
        {
            get;
        } = new();

        /// <summary>
        /// Gets a collection of objects that were already analysed.
        /// </summary>
        public ISet<object> TraversedObjects
        {
            get;
        } = new HashSet<object>();

        /// <summary>
        /// Schedules the provided object if it was not scheduled before.
        /// </summary>
        /// <param name="subject">The object to analyse.</param>
        public void SchedulaForAnalysis(object subject)
        {
            if (TraversedObjects.Add(subject))
                Agenda.Enqueue(subject);
        }
    }
}