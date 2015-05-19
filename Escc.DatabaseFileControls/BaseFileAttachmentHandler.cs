#region Using Directives
using System;
using System.Web;
using EsccWebTeam.Data.Ado;
#endregion

namespace EsccWebTeam.DatabaseFileControls
{
    /// <summary>
    /// Enables a file attachment to be displayed on a web page directly from database storage (whether an image or a document).
    /// </summary>
    public abstract class BaseFileAttachmentHandler : IHttpHandler
    {
        #region Declarations
        private HttpContext Context = null;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the record id for the file
        /// </summary>
        /// <value>The record id.</value>
        protected int FileDataID
        {
            get
            {
                // Get the parameter name we need to use.
                string paramName = this.QueryStringParameterNameForFileID();
                int id = 0;
                if ((this.Context != null) && (this.Context.Request != null))
                {
                    if (this.Context.Request.QueryString[paramName] != null)
                    {
                        try
                        {
                            // Try and convert the querystring parameter to an integer
                            id = Convert.ToInt32(this.Context.Request.QueryString[paramName]);
                        }
                        catch { }
                    }
                }
                return id;
            }
        }
        #endregion

        #region Abstract methods
        /// <summary>
        /// Gets a file attachment from the database.
        /// </summary>
        /// <param name="fileDataId">The record id for the stored file attachment</param>
        /// <param name="includeBlobData">If false, a special stored procedure is called that gets just the file details without the actual BLOB data.
        /// If true, the normal file retrieval stored procedure is called.</param>
        /// <returns>The file data for the stored file attachment</returns>
        public abstract DatabaseFileData GetFileAttachment(int fileDataId, bool includeBlobData);

        /// <summary>
        /// Each class that inherits from this class must specify the querystring parameter used to identify the file to fetch from the database.
        /// </summary>
        /// <returns></returns>
        public abstract string QueryStringParameterNameForFileID();
        #endregion

        #region IHttpHandler implementation
        /// <summary>
        /// Enables processing of HTTP Web requests by a custom HttpHandler that implements the <see cref="T:System.Web.IHttpHandler"/> interface.
        /// </summary>
        /// <param name="context">An <see cref="T:System.Web.HttpContext"/> object that provides references to the intrinsic server objects (for example, Request, Response, Session, and Server) used to service HTTP requests.</param>
        public void ProcessRequest(HttpContext context)
        {
            this.Context = context;

            int fileDataID = this.FileDataID;
            if (fileDataID > 0)
            {
                // We have a file we need to retrieve from the database
                DatabaseFileData fileData = GetFileAttachment(fileDataID, true);

                // Tell the response object the real name for the file
                this.Context.Response.AddHeader("Content-Disposition", "attachment;filename=" + fileData.FileOriginalName);
                // Tell the response object the content type for the file
                this.Context.Response.ContentType = fileData.FileContentType;
                // Now write the file's binary data out to the response object
                this.Context.Response.BinaryWrite(fileData.FileBLOBData);
            }
        }

        /// <summary>
        /// Gets a value indicating whether another request can use the <see cref="T:System.Web.IHttpHandler"/> instance.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Web.IHttpHandler"/> instance is reusable; otherwise, false.
        /// </returns>
        public bool IsReusable
        {
            get
            {
                return true;
            }
        }
        #endregion
    }
}
