using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Jobs;
using Sitecore.Web.UI.Sheer;
using Sitecore.SharedSource.Publishing.Classes;
using Sitecore.Shell.Framework.Commands;

namespace Sitecore.SharedSource.Publishing.Commands
{
   /// <summary>
   /// Class represents a publish cancel command.
   /// </summary>
   public class PublishCancel : Command
   {
      public override void Execute([NotNull] CommandContext context)
      {
         Assert.IsNotNull(context, "context");
         Context.ClientPage.Start(this, "Run", context.Parameters);
      }

      /// <summary>
      /// Method gets called in the pipeline to allow an interaction with a user.
      /// </summary>
      /// <param name="args"></param>
      public void Run(ClientPipelineArgs args)
      {
         Assert.ArgumentNotNull(args, "args");
         string cancelAll = args.Parameters["cancelAll"];
         if (args.Result == "yes")
         {
            if (cancelAll == "yes")
            {
               CancelAll();
            }
            else
            {
               CancelJob();
            }
         }
         else if (args.Result == "no")
         {
            args.AbortPipeline();
         }
         else
         {
            if (cancelAll == "yes")
            {
               var jobs = PublishJobHelper.GetJobs(JobState.Running);
               if (jobs.GetEnumerator().MoveNext())
               {
                  SheerResponse.Confirm(Translate.Text("Are you sure you want to cancel all current publishing jobs?"));
               }
               else
               {
                  SheerResponse.Alert(Translate.Text("There are no publishing jobs to cancel."));
               }
            }
            else
            {
               if (PublishJobHelper.GetSelectedJob("JobList") != null)
               {
                  SheerResponse.Confirm(Translate.Text("Are you sure you want to cancel selected publishing job?"));
               }
               else
               {
                  SheerResponse.Alert(Translate.Text("Please select a job from the list to cancel."));
               }
            }
         }
         args.WaitForPostBack();
      }

      /// <summary>
      /// Cancels all publishing jobs currently running or in queued state.
      /// </summary>
      public void CancelAll()
      {
         int canceledJobsCount = 0;
         foreach (var job in PublishJobHelper.GetJobs())
         {
            if (job != null && job.Status.State != JobState.Finished)
            {
               PublishJobHelper.CancelJob(this, job.JobHandle);
               canceledJobsCount++;
            }
         }
         Log.Info(string.Format("Publish cancel: {0} publishing related jobs were canceled.", canceledJobsCount), this);
      }

      /// <summary>
      /// Cancels a selected publishing job in the 
      /// </summary>
      protected void CancelJob()
      {
         Job job = PublishJobHelper.GetSelectedJob("JobList");
         if (job != null)
         {
            if (job.Status.State == JobState.Finished)
            {
               SheerResponse.Alert(Translate.Text("This job has already been completed."));
            }
            else
            {
               PublishJobHelper.CancelJob(this, job);
            }
         }
      }
   }
}