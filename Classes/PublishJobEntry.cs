using Sitecore;
using Sitecore.Jobs;
using Sitecore.Security.Accounts;

namespace Sitecore.SharedSource.Publishing.Classes
{
   /// <summary>
   /// Class is intended to simplify job properties of a job instance.
   /// </summary>
   public class PublishJobEntry
   {
      public PublishJobEntry(Handle jobHandle, string jobName, string category, JobStatus jobStatus, User jobOwner)
      {
         JobHandle = jobHandle.ToString();
         Name = jobName;
         Status = jobStatus;
         Owner = jobOwner;
         Category = category;
      }

      /// <summary>
      /// String value of a job handle property.
      /// </summary>
      public string JobHandle { get; set; }
      /// <summary>
      /// Job name.
      /// </summary>
      public string Name { get; set; }
      /// <summary>
      /// Job status object.
      /// </summary>
      public JobStatus Status { get; set; }
      /// <summary>
      /// String value of a job state property.
      /// </summary>
      public string State
      {
         get { return Status != null ? Status.State.ToString() : "Unknown"; }
      }
      /// <summary>
      /// User account that owns the job.
      /// </summary>
      public Account Owner { get; set; }
      /// <summary>
      /// A name of user account.
      /// </summary>
      public string OwnerName { get { return Owner != null ? Owner.Name : "Unknown"; }}
      /// <summary>
      /// Job category.
      /// </summary>
      public string Category { get; set; }
   }
}
