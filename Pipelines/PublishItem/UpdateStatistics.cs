using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Publishing;
using Sitecore.Publishing.Pipelines.Publish;
using Sitecore.Publishing.Pipelines.PublishItem;

namespace Sitecore.SharedSource.Publishing.Pipelines.PublishItem
{
   /// <summary>
   /// UpdateJobStatus class
   /// </summary>
   public class UpdateStatistics : PublishItemProcessor
   {
      #region Fields

      bool _traceToLog;

      #endregion

      #region Public properties

      /// <summary>
      /// Gets or sets a value indicating whether to trace publishing information for every item to the log.
      /// </summary>
      /// <value><c>true</c> if trace to log; otherwise, <c>false</c>.</value>
      public virtual bool TraceToLog
      {
         get
         {
            return _traceToLog;
         }
         set
         {
            _traceToLog = value;
         }
      }

      #endregion

      #region Public methods

      /// <summary>
      /// Processes the specified args.
      /// </summary>
      /// <param name="context">The context.</param>
      public override void Process([NotNull] PublishItemContext context)
      {
         Assert.ArgumentNotNull(context, "context");

         UpdateContextStatistics(context);
         UpdateJobStatistics(context);
         TraceInformation(context);
      }

      /// <summary>
      /// Traces the information.
      /// </summary>
      /// <param name="context">The context.</param>
      void TraceInformation([NotNull] PublishItemContext context)
      {
         Debug.ArgumentNotNull(context, "context");

         if (!TraceToLog)
         {
            return;
         }

         PublishItemResult result = context.Result;
         Item item = context.PublishHelper.GetItemToPublish(context.ItemId);

         string itemName = (item != null ? item.Name : "(null)");
         string itemOperation = (result != null ? result.Operation.ToString() : "(null)");
         string childAction = (result != null ? result.ChildAction.ToString() : "(null)");
         string explanation = (result != null && result.Explanation.Length > 0 ? result.Explanation : "(none)");

         Log.Info("##Publish Item:         " + itemName + " - " + context.ItemId, this);
         Log.Info("##Publish Operation:    " + itemOperation, this);
         Log.Info("##Publish Child Action: " + childAction, this);
         Log.Info("##Explanation:          " + explanation, this);
      }

      #endregion

      #region Private methods

      /// <summary>
      /// Updates the publish context.
      /// </summary>
      /// <param name="context">The context.</param>
      void UpdateContextStatistics([NotNull] PublishItemContext context)
      {
         Debug.ArgumentNotNull(context, "context");

         PublishItemResult result = context.Result;
         PublishContext publishContext = context.PublishContext;

         if (result == null || publishContext == null)
         {
            return;
         }

         switch (result.Operation)
         {
            case PublishOperation.None:
            case PublishOperation.Skipped:
               lock (publishContext)
               {
                  publishContext.Statistics.Skipped++;
               }
               break;

            case PublishOperation.Created:
               lock (publishContext)
               {
                  publishContext.Statistics.Created++;
               }
               break;

            case PublishOperation.Updated:
               lock (publishContext)
               {
                  publishContext.Statistics.Updated++;
               }
               break;

            case PublishOperation.Deleted:
               lock (publishContext)
               {
                  publishContext.Statistics.Deleted++;
               }
               break;
         }
      }

      /// <summary>
      /// Updates the job statistics.
      /// </summary>
      /// <param name="context">The context.</param>
      void UpdateJobStatistics([NotNull] PublishItemContext context)
      {
         Assert.ArgumentNotNull(context, "context");

         PublishItemResult result = context.Result;

         if (result == null || result.Operation == PublishOperation.None)
         {
            return;
         }

         Job job = context.Job;

         if (job == null)
         {
            return;
         }

         lock (job)
         {
            job.Status.Processed++;
         }
      }

      #endregion
   }
}