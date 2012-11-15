// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PublishForm.cs" company="Sitecore">
//   Sitecore Shared Source license applies to this file.
// </copyright>
// <summary>
//   Represents a PublishForm.cs file.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sitecore.SharedSource.Publishing
{
   using System;
   using System.Linq;

   using Sitecore.Data;
   using Sitecore.Data.Items;
   using Sitecore.Data.Managers;
   using Sitecore.Diagnostics;
   using Sitecore.Globalization;
   using Sitecore.Jobs;
   using Sitecore.SecurityModel;
   using Sitecore.SharedSource.Publishing.Classes;
   using Sitecore.Web.UI.HtmlControls;
   using Sitecore.Web.UI.Sheer;

   /// <summary>
   /// Class extends standard PublishForm type.
   /// </summary>
   public class PublishForm : Sitecore.Shell.Applications.Dialogs.Publish.PublishForm
   {
      #region Fields

      /// <summary>
      /// Variables representing UI controls on the wizrd form.
      /// </summary>
      protected Border SmartPublishPane;
      protected Border RepublishPane;
      protected Border PublishStatusManagerLink;

      #endregion Fields

      /// <summary>
      /// Page load event.
      /// </summary>
      /// <param name="e">
      /// Event argument of EventArgs compatible type.
      /// </param>
      protected override void OnLoad(EventArgs e)
      {
         base.OnLoad(e);
         if (!Context.ClientPage.IsEvent)
         {
            IncrementalPublishPane.Visible = IncrementalPublishPane.Visible
                                                ? this.IsHidden(Settings.IncrementalSettingId)
                                                : IncrementalPublishPane.Visible;
            IncrementalPublish.Disabled = IsDisabled(Settings.IncrementalSettingId);
            if (!IncrementalPublishPane.Visible || IncrementalPublish.Disabled)
            {
               IncrementalPublish.Checked = false;
            }

            this.SmartPublishPane.Visible = this.IsHidden(Settings.SmartSettingId);
            SmartPublish.Disabled = IsDisabled(Settings.SmartSettingId);
            if (!this.SmartPublishPane.Visible || this.SmartPublish.Disabled)
            {
               SmartPublish.Checked = false;
            }

            this.RepublishPane.Visible = this.IsHidden(Settings.RepublishSettingId);
            Republish.Disabled = IsDisabled(Settings.RepublishSettingId);
            if (!this.RepublishPane.Visible || this.Republish.Disabled)
            {
               Republish.Checked = false;
            }

            PublishChildrenPane.Visible = PublishChildrenPane.Visible
                                             ? this.IsHidden(Settings.PublishChildrenSettingId)
                                             : PublishChildrenPane.Visible;
            PublishChildren.Disabled = IsDisabled(Settings.PublishChildrenSettingId);
            if (!PublishChildrenPane.Visible || PublishChildren.Disabled)
            {
               PublishChildren.Checked = false;
            }

            // Show Publish Status Manager link
            if (Security.SecurityHelper.CanRunApplication("Publish Status Manager"))
            {
               this.PublishStatusManagerLink.Visible = true;
            }
         }
      }

      /// <summary>
      /// Action to run when wizard page is changed.
      /// </summary>
      /// <param name="page">
      /// Wizard page step name.
      /// </param>
      /// <param name="oldPage">
      /// Wizard page previous step name.
      /// </param>
      protected override void ActivePageChanged(string page, string oldPage)
      {
         base.ActivePageChanged(page, oldPage);
         if (page == "Publishing")
         {
            // Enable cancel button
            this.CancelButton.Disabled = this.DisableCancelButton(Settings.CancelButtonSettingId);
         }
      }

      /// <summary>
      /// Action to run when wizard page is changing.
      /// </summary>
      /// <param name="page">
      /// Current wizard page name.
      /// </param>
      /// <param name="newpage">
      /// New wizard page name.
      /// </param>
      /// <returns>
      /// Indicates if page is changed.
      /// </returns>
      protected override bool ActivePageChanging(string page, ref string newpage)
      {
         if (newpage == "Publishing")
         {
            if (!CheckedPublishOption())
            {
               SheerResponse.Alert(Translate.Text("You must pick a publish mode option."), new string[0]);
               return false;
            }
         }

         return base.ActivePageChanging(page, ref newpage);
      }

      /// <summary>
      /// Action to take when Next button is clicked.
      /// </summary>
      /// <param name="sender">
      /// The event sender.
      /// </param>
      /// <param name="formEventArgs">
      /// The form event args.
      /// </param>
      protected override void OnNext(object sender, EventArgs formEventArgs)
      {
         // If PublishChildren checkbox is selected, run confirmation dialog.
         if (this.PublishChildren.Checked && this.ServerProperties["active"].Equals("Settings") && ConfirmPublishChildren(this.ItemID)
             && !string.IsNullOrEmpty(this.ItemID))
         {
            Context.ClientPage.Start(this, "ConfirmPublish");
         }
         else
         {
            base.OnNext(sender, formEventArgs);
         }
      }

      /// <summary>
      /// Opens Publish Status Manager application as a modal dialog.
      /// </summary>
      protected void OpenPublishStatusManager()
      {
         // Close parental PublishDialog window.
         EndWizard();

         // Open Publish Status Manager as an app.
         Sitecore.Shell.Framework.Windows.RunApplication("Publish Status Manager");
      }

      /// <summary>
      /// Cancels the dialog and publishing job(s) created by a context user.
      /// </summary>
      /// <param name="sender">
      /// The event sender.
      /// </param>
      /// <param name="formEventArgs">
      /// The form event args.
      /// </param>
      protected override void OnCancel(object sender, EventArgs formEventArgs)
      {
         // Close the dialog if the job is finished.
         if (Active == "Settings" || Active == "Retry" || Active == "FirstPage" || Active == "LastPage")
         {
            base.OnCancel(sender, formEventArgs);
         }
         else
         {
            Context.ClientPage.Start(this, "Cancel");
         }
      }

      /// <summary>
      /// Cancels all publishing jobs owned by the context user.
      /// </summary>
      protected virtual void CancelJob()
      {
         Job[] jobs = JobManager.GetJobs();
         var publishJobs = jobs.Where(j => j.Category.StartsWith("publish") && j.Options.ContextUser == Context.User);
         if (publishJobs.Count() > 0)
         {
            foreach (var job in publishJobs)
            {
               PublishJobHelper.CancelJob(this, job);
            }
         }
      }

      /// <summary>
      /// Indicates if a publish option should be hidden.
      /// </summary>
      /// <param name="itemId">Publish option setting item ID.</param>
      /// <returns>Boolean value</returns>
      protected bool IsHidden(ID itemId)
      {
         Item settingItem = GetItem(itemId);
         return settingItem != null;
      }

      /// <summary>
      /// Indicates if Cancel button should be disabled
      /// </summary>
      /// <param name="settingId">Cancel button setting item ID.</param>
      /// <returns>Boolean value</returns>
      protected virtual bool DisableCancelButton(ID settingId)
      {
         Item settingItem = GetItem(settingId);
         if (settingItem == null)
         {
            return true;
         }

         return !(settingItem["Enabled"] == "1" || Context.User.IsAdministrator);
      }

      /// <summary>
      /// Indicates if confirmation dialog on publish subitems option is enabled.
      /// </summary>
      /// <returns>
      /// Returns confirmation dialog result.
      /// </returns>
      private static bool ConfirmationEnabled()
      {
         Item settingItem = GetItem(Settings.ConfirmationSettingId);
         if (settingItem != null)
         {
            return settingItem["Enabled"] == "1";
         }

         return false;
      }

      /// <summary>
      /// Check whether verification dialog should pop-up.
      /// </summary>
      /// <param name="itemId">
      /// Selected item id.
      /// </param>
      /// <returns>
      /// Returns <c>true</c> if item has children and confirmation setting is enabled, <c>false</c> otherwise.
      /// </returns>
      private static bool ConfirmPublishChildren(string itemId)
      {
         Assert.IsNotNullOrEmpty(itemId, "itemId");
         if (ID.IsID(itemId))
         {
            Item item = GetItem(ID.Parse(itemId));

            // As publishing security is disabled by default it's better to rise the dialog even if a publisher cannot see item children.
            return ItemManager.HasChildren(item, SecurityCheck.Disable) && ConfirmationEnabled();
         }

         return ConfirmationEnabled();
      }

      /// <summary>
      /// Returns a Sitecore item.
      /// </summary>
      /// <param name="itemId">item ID.</param>
      /// <returns>Boolean value</returns>
      private static Item GetItem(ID itemId)
      {
         Item item = Context.ContentDatabase.GetItem(itemId);
         return item;
      }

      /// <summary>
      /// Indicates if a publish option is selected.
      /// </summary>
      /// <returns>Boolean value</returns>
      private static bool CheckedPublishOption()
      {
         return !string.IsNullOrEmpty(Context.ClientPage.ClientRequest.Form.Get("PublishMode"));
      }

      /// <summary>
      /// Indicates whether a publishing option shoudl be disabled based on a security configured for a publishing option item. 
      /// </summary>
      /// <param name="itemId">Publishing option item ID.</param>
      /// <returns>Boolean value</returns>
      private static bool IsDisabled(ID itemId)
      {
         Item settingItem = GetItem(itemId);
         if (settingItem != null)
         {
            return settingItem["Disabled"] == "1";
         }

         return false;
      }

      /// <summary>
      /// Confirmation dialog for next wizard button.
      /// </summary>
      /// <param name="args">
      /// Pipeline arguments.
      /// </param>
      private void ConfirmPublish(ClientPipelineArgs args)
      {
         Assert.ArgumentNotNull(args, "args");
         if (args.Result == "yes")
         {
            // Call this method to move to next page in the Wizard form.
            Next();
         }
         else if (args.Result == "no")
         {
            args.AbortPipeline();
         }
         else
         {
            Context.ClientPage.ClientResponse.Confirm(Translate.Text("Are you sure you want to publish subitems too?"));
         }

         args.WaitForPostBack();
      }

      /// <summary>
      /// This method gets called as a pipeline from OnCancel method.
      /// </summary>
      /// <param name="args">
      /// Pipeline arguments.
      /// </param>
      private void Cancel(ClientPipelineArgs args)
      {
         Assert.ArgumentNotNull(args, "args");
         if (args.Result == "yes")
         {
            this.CancelJob();
         }
         else if (args.Result == "no")
         {
            args.AbortPipeline();
         }
         else
         {
            Context.ClientPage.ClientResponse.Confirm(
               Translate.Text("Are you sure you want to cancel this publishing job?"));
         }

         args.WaitForPostBack();
      }
   }
}