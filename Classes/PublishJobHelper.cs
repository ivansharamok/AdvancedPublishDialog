using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sitecore;
using Sitecore.Collections;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Publishing;

namespace Sitecore.SharedSource.Publishing.Classes
{
   public static class PublishJobHelper
   {
      /// <summary>
      /// Returns publish related jobs.
      /// </summary>
      /// <returns></returns>
      public static IEnumerable<PublishJobEntry> GetJobs()
      {
         var jobs = JobManager.GetJobs();
         var publishJobs =
            jobs.Where(job => job.Category.StartsWith("publish", StringComparison.InvariantCultureIgnoreCase)).Select(
               job => new PublishJobEntry(job.Handle, job.Name, job.Category, job.Status, job.Options.ContextUser));
         List<PublishJobEntry> jobList = new List<PublishJobEntry>(publishJobs);
         return jobList;
      }

      /// <summary>
      /// Returns publish related jobs in a specified state.
      /// </summary>
      /// <param name="state"></param>
      /// <returns></returns>
      public static IEnumerable<PublishJobEntry> GetJobs(JobState state)
      {
         var jobs = JobManager.GetJobs();
         var publishJobs =
            jobs.Where(job => job.Category.Equals("publish", StringComparison.InvariantCultureIgnoreCase) && job.Status.State == state).Select(
               job => new PublishJobEntry(job.Handle, job.Name, job.Category, job.Status, job.Options.ContextUser));
         List<PublishJobEntry> jobList = new List<PublishJobEntry>(publishJobs);
         return jobList;
      }

      /// <summary>
      /// Returns a selected publish job from the Listview control.
      /// </summary>
      /// <param name="jobContainerId">The Id of the Listview control.</param>
      /// <returns></returns>
      public static Job GetSelectedJob(string jobContainerId)
      {
         Sitecore.Web.UI.HtmlControls.Listview jobList =
            Context.ClientPage.FindSubControl(jobContainerId) as Sitecore.Web.UI.HtmlControls.Listview;
         if (jobList != null && jobList.SelectedItems.Length > 0)
         {
            string jobHandle = jobList.SelectedItems[0].ID;
            return JobManager.GetJob(Handle.Parse(jobHandle));
         }
         return null;
      }

      /// <summary>
      /// Cancels a job.
      /// </summary>
      /// <param name="cancelOwner">An object that calls the method.</param>
      /// <param name="job">Job instance.</param>
      public static void CancelJob(object cancelOwner, Job job)
      {
         if (job != null)
         {
            // Expire the job now
            job.Status.Expiry = DateTime.Now;
            PublishStatus ps = job.Options.CustomData as PublishStatus;
            if (ps != null)
            {
               ps.Messages.Add(string.Format(Sitecore.Globalization.Translate.Text("Publishing job was forced to finish by \"{0}\" user"),
                                             Context.GetUserName()));
            }
            if (job.Status.State == JobState.Queued)
            {
               FinishJob(job);
            }
            else
            {
               if (ps != null)
               {
                  // Set job state to Finish to force it quit.
                  ps.SetState(JobState.Finished);
               }
            }
            Log.Audit(cancelOwner, "Publish cancel: Publishing job \"{1}/{2}\" was forced to finish by \"{0}\" user",
                      new[] { Context.GetUserName(), job.Name, job.Handle.ToString() });
         }
         else
         {
            Log.SingleWarn("Publish cancel: Failed to cancel a publishing job. The job was not found.", cancelOwner);
         }

      }

      /// <summary>
      /// Cancels a job.
      /// </summary>
      /// <param name="cancelOwner">An object that calls the method.</param>
      /// <param name="jobHandle">A job handle.</param>
      public static void CancelJob(object cancelOwner, string jobHandle)
      {
         Job job = JobManager.GetJob(Handle.Parse(jobHandle));
         CancelJob(cancelOwner, job);
      }

      /// <summary>
      /// Finishes queued job.
      /// </summary>
      /// <param name="job">Job instance to finish.</param>
      private static void FinishJob(Job job)
      {
         Type type = typeof(JobManager);
         // Use _queuedJobs collection to remove the job from the queue.
         FieldInfo queuedFI = type.GetField("_queuedJobs",
                                             BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
         // Use private method of JobManager class to finish the job.
         MethodInfo finishJobMI = type.GetMethod("FinishJob", BindingFlags.Static | BindingFlags.NonPublic);
         if (queuedFI != null && finishJobMI != null)
         {
            JobCollection queuedJobs = queuedFI.GetValue(null) as JobCollection;
            if (queuedJobs != null && finishJobMI != null)
            {
               // Remove the job from queuedJobs collection.
               queuedJobs.Remove(job);
               queuedFI.SetValue(null, queuedJobs);
               // Move the job to finishedJobs collection.
               finishJobMI.Invoke(null, new object[] {job});
            }
         }
      }
   }
}
