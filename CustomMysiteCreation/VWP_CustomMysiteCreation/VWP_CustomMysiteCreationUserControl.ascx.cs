using System;
using System.Web;
using System.Collections;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.Office.Server.UserProfiles;
using Microsoft.SharePoint.Administration;

namespace CustomMysiteCreation.VisualWebPart1
{
    public partial class VisualWebPart1UserControl : UserControl
    {
        UserProfileManager profileManager;
        UserProfile profile;
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {

                SPSecurity.RunWithElevatedPrivileges(delegate
                {
                    try
                    {
                        SPWeb currentWeb = SPContext.Current.Web;
                        SPUser currentUser = SPContext.Current.Web.CurrentUser;
                        SPServiceContext serverContext = SPServiceContext.GetContext(currentWeb.Site);
                        profileManager = new UserProfileManager(serverContext);
                        profile = profileManager.GetUserProfile(currentUser.LoginName);
                    }
                    catch (Exception ex)
                    {
                        if(ex.Message == "An error was encountered while retrieving the user profile.")
                        {
                            btnCreate.Visible = false;
                        }
                    }
                    try
                    {
                        string mySite = profile["PersonalSpace"].ToString();
                        if(mySite != "")
                        {
                            btnCreate.Visible = false;
                        }
                    }
                    catch
                    {
                        
                    }
                });
            }
        }

        protected void btnCreate_Click(object sender, EventArgs e)
        {
            btnCreate.Visible = false;
            string url = "";
            string emailAddress= "";
            SPList list;
            string listdomain = "";
            string listurl = "";
            string listcontentdb = "";
            ArrayList createmysiteinfo = null;
            SPUser currentUser = null;
            SPServiceContext serviceContext = null;
            HttpContext httpContext = null;
            UserProfileManager upm = null;
            SPSecurity.RunWithElevatedPrivileges(delegate ()
            {
                try
                {
                    httpContext = HttpContext.Current;
                    serviceContext = SPServiceContext.GetContext(httpContext);
                    upm = new UserProfileManager(serviceContext);
                    currentUser = SPContext.Current.Web.CurrentUser;
                    profile = upm.GetUserProfile(currentUser.LoginName);
                }
                catch (Exception ex)
                {
                    lblInfo.Text += "profileManager error:" + ex.Message + " - ";
                }
                
                
                try
                {
                    // 1. list site collection, 2. mysite host, 3. db server name
                    createmysiteinfo = new ArrayList(System.Configuration.ConfigurationManager.AppSettings["CreateMySite"].Split(";".ToCharArray()));
                }
                catch(Exception ex)
                {
                    lblInfo.Text += "web.config error:" + ex.Message + " - ";
                }
                try
                {
                    using (SPSite site = new SPSite((string)createmysiteinfo[0]))
                    {
                        using (SPWeb web = site.RootWeb)
                        {
                            try
                            {
                                emailAddress = profile["WorkEmail"].ToString().ToLower();
                                list = web.Lists["CreateMySite"];
                                foreach (SPListItem item in list.Items)
                                {
                                    if (emailAddress.Split("@".ToCharArray())[1].ToLower() == item["Domain"].ToString().ToLower())
                                    {
                                        listdomain = item["Domain"].ToString().ToLower();
                                        listurl = item["Url"].ToString().ToLower();
                                        listcontentdb = item["ContentDatabase"].ToString().ToLower();
                                    }
                                }
                                url = listurl + @"/" + emailAddress.Split("@".ToCharArray())[0].ToLower();
                            }
                            catch (Exception ex)
                            {
                                lblInfo.Text += "List reference error: " + ex.Message + "\r\n";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    lblInfo.Text += "spsite list error: " + listcontentdb + " - " + ex.Message + "\r\n";
                }
                try
                {
                    using (SPSite site = new SPSite(createmysiteinfo[1].ToString().ToLower()))
                    {
                        SPContentDatabase db = null;
                        SPWebApplication webapp = null;
                        try
                        {
                            webapp = site.WebApplication;
                        }
                        catch (Exception ex)
                        {
                            lblInfo.Text += "webapp: " + ex.Message + " - ";
                        }
                        
                        try
                        {
                            foreach (SPContentDatabase curdb in webapp.ContentDatabases)
                            {
                                if (curdb.Name.ToLower() == listcontentdb.ToLower())
                                {
                                    db = curdb;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            lblInfo.Text += "Unable to find receiving content database: " + ex.Message + " - ";
                        }
                        try
                        {
                            SPSite newSite = db.Sites.Add(url,
                                                        profile.DisplayName,
                                                        "My Site",
                                                        1033,
                                                        "SPSPERS#2",
                                                        profile.AccountName,
                                                        profile.DisplayName,
                                                        emailAddress
                                                        );
                            newSite.RootWeb.Update();
                        }
                        catch (Exception ex)
                        {
                            lblInfo.Text += "Error creating MySite: " + ex.Message + " - ";
                        }
                    }
                }
                catch (Exception ex)
                {
                    lblInfo.Text += "Error getting mysite handle: " + listcontentdb + " - " + ex.Message + "\r\n";
                }

                try
                {
                    System.Uri uri = new Uri(createmysiteinfo[1] + url);
                    if (SPSite.Exists(uri))
                    {
                        HttpContext.Current = null; // Hack to let you edit an admin only property
                        profile["PersonalSpace"].Value = url;
                        profile.Commit();
                        HttpContext.Current = httpContext;
                        Microsoft.SharePoint.Utilities.SPUtility.Redirect(createmysiteinfo[1] + url, Microsoft.SharePoint.Utilities.SPRedirectFlags.Default, httpContext, null);
                    }
                    else
                    {
                        lblInfo.Text += "There was an issue creating your My Site. Please contact your admnistrator.";
                    }
                }
                catch(Exception ex)
                {
                    lblInfo.Text += "Error updating PersonalSpace property: " + ex.Message + "\r\n";
                }
                finally
                {
                    //restore old context
                    HttpContext.Current = httpContext;
                }
            });
        }
    }
}
