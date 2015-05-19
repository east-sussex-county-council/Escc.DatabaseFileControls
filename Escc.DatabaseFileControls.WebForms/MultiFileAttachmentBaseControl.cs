#region Using Directives

using System;
using System.ComponentModel;
using System.Configuration;
using System.Collections.Specialized;
using System.Globalization;
using System.Security.Principal;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Escc.FormControls.WebForms;
using Escc.Data.Ado;
using Escc.FormControls.WebForms.Validators;
using EsccWebTeam.NavigationControls;

#endregion

// --------------------------------------------------------------------------------------------
// Order of method calls and an overview of what takes place:
//
// Initialise:
//   * Just initialises the 'field data control arrays'.
//
// CreateChildControls:
//   (Viewstate data is NOT yet applied)
//   * Configures the 'field data control arrays', including various div tags for css styling.
//   * Adds all the 'field data control arrays' to the main control.
//
// If not postback:
// Parent IHas????Item method Set????Item calls SetFileAttachments:
//   (Viewstate data is NOT yet applied)
//   * Updates the data in the hidden fields with file id and name from the parent's ????Item.FileAttachments collection.
//
// Otherwise if postback:
//   * Viewstate data gets loaded into the hidden controls. 
//
// Button event handlers:
//   (Viewstate data is now applied)
//   * Updates the data in the hidden controls only.
//
// OnPreRender:
//   (Viewstate data is now applied)
//   * Transfers the hidden control data to the file display controls.
//   * Hides or shows the div tag control that surrounds the display of the file.
//
// --------------------------------------------------------------------------------------------
namespace Escc.DatabaseFileControls.WebForms
{
    /// <summary>
    /// A control that allows the user to browse for files one at a time and build up a list. The control has buttons for adding a file to the list
    /// and removing files from the list.
    /// </summary>
    [DefaultProperty("FilePaths"),
    ToolboxData("<{0}:MultiFileAttachmentBaseControl runat=server></{0}:MultiFileAttachmentBaseControl>")]
    public abstract class MultiFileAttachmentBaseControl : WebControl, INamingContainer
    {
        #region Declarations
        public const int FileDescMaxLength = 255;
        /// <summary>
        /// Optional TextBox control for passing a file description when a file is stored to the database.
        /// </summary>
        private TextBox fileDescriptionBox;

        private MultiFileAttachmentType attachmentType;
        protected HtmlGenericControl fileBrowserPart;
        protected HtmlGenericControl fileBrowserLabel;
        protected HtmlInputFile fileBrowserBox;
        protected FormPart addButtonPart;
        protected HtmlGenericControl fieldsetPart;
        protected HtmlGenericControl fieldsetLabel;
        /// <summary>
        /// A place holder for inheriting controls to add additional fields.
        /// e.g the ImageAttachmentControl will use this to add the image description field just below the file selection control
        /// </summary>
        protected PlaceHolder placePostFileBrowserParts;
        protected HtmlGenericControl spanFields;
        private EsccButton addFile;
        private string attachmentReference;

        // Validation declarations
        private EsccValidationSummary vUploadSummary;
        protected UploadSizeValidator vUploadSize;
        protected UploadFormatValidator vUploadFormat;
        protected UploadFileCountValidator vUploadFileCount;
        private string validationGroup;

        // The following are a series of 'field data control arrays'.
        // There is one of each control for each of the files that can be attached (up to 'maxFiles' in size).
        protected int maxFiles;
        protected HtmlInputHidden[] fileIdArray;
        protected HtmlInputHidden[] fileNameArray;
        private HtmlGenericControl[] spanFilenameArray;
        private HtmlGenericControl[] divFileArray;

        /// <summary>
        /// This is the name of the .NET project that is storing files in a database.
        /// </summary>
        /// <remarks>The name will be used to form the url for the project folder in the IIS web application,
        /// e.g. 'EsccWebTeam.Czone.EventCalendar' will be used to locate an image handler at '/EsccWebTeam.Czone.EventCalendar/ImageHandler.ashx' etc.</remarks>
        protected string dotnetProjectName = string.Empty;
        #endregion

        #region Configuration

        private static NameValueCollection fileConfig = FileConfig();
        private static NameValueCollection imageConfig = ImageConfig();

        /// <summary>
        /// Gets the configuration settings from the FileAttachmentSettings section
        /// </summary>
        /// <returns></returns>
        protected static NameValueCollection FileConfig()
        {
            var config = ConfigurationManager.GetSection("Escc.DatabaseFileControls/FileAttachmentSettings") as NameValueCollection;
            if (config == null) config = ConfigurationManager.GetSection("EsccWebTeam.DatabaseFileControls/FileAttachmentSettings") as NameValueCollection;
            return config;
        }

        /// <summary>
        /// Gets the configuration settings from the ImageSettings section
        /// </summary>
        /// <returns></returns>
        protected static NameValueCollection ImageConfig()
        {
            var config = ConfigurationManager.GetSection("Escc.DatabaseFileControls/ImageSettings") as NameValueCollection;
            if (config == null) config = ConfigurationManager.GetSection("EsccWebTeam.DatabaseFileControls/ImageSettings") as NameValueCollection;
            return config;
        }

        #endregion


        #region Properties
        /// <summary>
        /// Gets or sets the file attachment reference used particularly in feedback error messages to the user.
        /// E.g. image or document.
        /// </summary>
        public string AttachmentReference
        {
            get { return attachmentReference; }
            set { attachmentReference = value; }
        }

        /// <summary>
        /// The control for selecting the file to upload.
        /// </summary>
        public HtmlInputFile FileBrowserBox
        {
            get { return this.fileBrowserBox; }
        }

        /// <summary>
        /// Optional TextBox control for passing a file description when a file is stored to the database.
        /// </summary>
        public TextBox FileDescriptionBox
        {
            get { return fileDescriptionBox; }
            set { fileDescriptionBox = value; }
        }

        /// <summary>
        /// Gets or sets the validation group used by this control
        /// </summary>
        public string ValidationGroup
        {
            get { return validationGroup; }
            set { validationGroup = value; }
        }

        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiFileAttachmentBaseControl"/> class.
        /// </summary>
        /// <param name="attachmentType">Defines the type of file attachment this control supports</param>
        /// <param name="maxFiles">The maximum files that will be supported by this control</param>
        /// <param name="validationGroup">The validation group used by this control</param>
        /// <param name="attachmentReference">The attachment reference used by this control (particularly in feedback error messages)</param>
        public MultiFileAttachmentBaseControl(
            string dotnetProjectName,
            MultiFileAttachmentType attachmentType,
            int maxFiles,
            string validationGroup,
            string attachmentReference)
            : base(HtmlTextWriterTag.Fieldset)
        {
            // Set the .NET project name specific for file retrieval from the database.
            this.dotnetProjectName = dotnetProjectName;

            // Set the file attachment type that will be supported by this control
            this.attachmentType = attachmentType;

            // Set the maximum files that will be supported by this control
            this.maxFiles = maxFiles;

            // Set the validation group used by this control
            this.validationGroup = validationGroup;

            // Set the attachment reference used by this control
            this.attachmentReference = attachmentReference;

            // Create the validation summary control
            this.vUploadSummary = new EsccValidationSummary();
            this.vUploadSummary.ValidationGroup = this.validationGroup;

            // Create the 'form part' for the file browser
            this.fileBrowserBox = new HtmlInputFile();
            this.fileBrowserPart = new HtmlGenericControl("div");
            this.fileBrowserPart.Attributes.Add("class", "formPart");
            this.placePostFileBrowserParts = new PlaceHolder();

            // Create the 'form part' for the 'Add' button control. Enclose the button in a div tag that has the class 'formControl' so
            // that the button will appear a normal button size.
            HtmlGenericControl divAddButton = new HtmlGenericControl("div");
            this.addFile = new EsccButton();
            this.addFile.Text = "Add";
            this.addFile.Click += new EventHandler(addFile_Click);
            this.addFile.ValidationGroup = this.ValidationGroup;
            divAddButton.Controls.Add(this.addFile);
            this.addButtonPart = new FormPart(string.Empty, divAddButton);

            // Create the 'form part' for the fieldset control
            this.fieldsetPart = new HtmlGenericControl("fieldset");
            this.fieldsetPart.Attributes.Add("class", "formPart");

            // Initialise the file data arrays from page request to page request.
            this.fileIdArray = new HtmlInputHidden[maxFiles];
            this.fileNameArray = new HtmlInputHidden[maxFiles];
            this.spanFilenameArray = new HtmlGenericControl[maxFiles];
            this.divFileArray = new HtmlGenericControl[maxFiles];
            for (int index = 0; index < maxFiles; index++)
            {
                this.fileIdArray[index] = new HtmlInputHidden();
                this.fileNameArray[index] = new HtmlInputHidden();
                this.spanFilenameArray[index] = new HtmlGenericControl("span");
                this.divFileArray[index] = new HtmlGenericControl("div");
            }
        }
        #endregion

        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation 
        /// to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            // Place the validation summary control at the top of this control
            this.Controls.Add(this.vUploadSummary);

            // Add the form parts
            this.Controls.Add(this.fileBrowserPart);
            this.Controls.Add(this.placePostFileBrowserParts);
            this.Controls.Add(this.addButtonPart);
            this.Controls.Add(this.fieldsetPart);

            // Best to use the Label control here because it handles html in the label text whereas 'HtmlGenericControl("label")' does not.
            this.fileBrowserLabel = new HtmlGenericControl("label");
            this.fileBrowserLabel.Attributes.Add("class", "formLabel");
            this.fileBrowserLabel.InnerHtml = fileConfig["FileEditPrompt"];
            this.fileBrowserPart.Controls.Add(this.fileBrowserLabel);

            // Set up the file browsing control and add it to the relevant 'form part'.
            this.fileBrowserBox.ID = "fileUpload";
            this.fileBrowserBox.Attributes["class"] = "formControl fileBrowserControl";
            this.fileBrowserPart.Controls.Add(this.fileBrowserBox);

            // Best to use the Label control here because it handles html in the label text whereas 'HtmlGenericControl("label")' does not.
            this.fieldsetLabel = new HtmlGenericControl("legend");
            this.fieldsetLabel.Attributes.Add("class", "formLabel");
            this.fieldsetLabel.InnerHtml = "";
            this.fieldsetPart.Controls.Add(this.fieldsetLabel);

            this.spanFields = new HtmlGenericControl("div");
            this.spanFields.Attributes["class"] = "formControl";
            this.fieldsetPart.Controls.Add(this.spanFields);

            // Add the file attachment id hidden controls and the filename labels
            for (int index = 0; index < maxFiles; index++)
            {
                HtmlInputHidden idBox = this.fileIdArray[index];
                HtmlInputHidden nameBox = this.fileNameArray[index];
                HtmlGenericControl spanFilename = this.spanFilenameArray[index];
                HtmlGenericControl divFile = this.divFileArray[index];
                // Give the file data array controls a unique id for adding to the control collection.
                idBox.ID = "fileId_" + index.ToString(CultureInfo.CurrentCulture);
                nameBox.ID = "fileName_" + index.ToString(CultureInfo.CurrentCulture);
                spanFilename.ID = "fileLabel_" + index.ToString(CultureInfo.CurrentCulture);
                divFile.ID = "fileSpace_" + index.ToString(CultureInfo.CurrentCulture);

                // Add the hidden controls to the main control
                this.fieldsetPart.Controls.Add(idBox);
                this.fieldsetPart.Controls.Add(nameBox);

                // Create a dedicated delete button for the file
                EsccButton removeFile = new EsccButton();
                removeFile.ID = "removeFile_" + index.ToString(CultureInfo.CurrentCulture);
                removeFile.Text = "Delete";
                removeFile.Click += new EventHandler(removeFile_Click);
                removeFile.CausesValidation = false;
                removeFile.ValidationGroup = this.ValidationGroup;

                // Create the div tags for displaying the file and its associated delete button
                HtmlGenericControl spanFileButton = new HtmlGenericControl("span");
                divFile.Attributes["class"] = "attachedFile lowerDottedBorder";
                if (index == 0) divFile.Attributes["class"] += " upperDottedBorder";
                spanFilename.Attributes["class"] = "attachedFileName";
                spanFileButton.Attributes["class"] = "attachedFileButton";

                // Add the controls to each other
                spanFileButton.Controls.Add(removeFile);
                divFile.Controls.Add(spanFilename);
                divFile.Controls.Add(spanFileButton);
                this.spanFields.Controls.Add(divFile);
            }

            this.fileBrowserLabel.Attributes["for"] = this.fileBrowserBox.UniqueID.Replace("$", "_"); // Wait for this, because box doesn't get its id until it's added to the control tree
        }

        #region Validation
        /// <summary>
        /// Adds the upload size validator.
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <param name="maximumBytes"></param>
        protected void AddUploadSizeValidator(
            string errorMessage,
            int maximumBytes)
        {
            this.vUploadSize = new UploadSizeValidator(this.fileBrowserBox.ID, errorMessage, maximumBytes);
            if (!string.IsNullOrEmpty(this.ValidationGroup)) this.vUploadSize.ValidationGroup = this.ValidationGroup;
            this.Controls.Add(this.vUploadSize);
        }

        /// <summary>
        /// Adds the upload format validator.
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <param name="allowedFormats"></param>
        protected void AddUploadFormatValidator(
            string errorMessage,
            string allowedFormats)
        {
            this.vUploadFormat = new UploadFormatValidator(this.fileBrowserBox.ID, errorMessage, allowedFormats);
            if (!string.IsNullOrEmpty(this.ValidationGroup)) this.vUploadFormat.ValidationGroup = this.ValidationGroup;
            this.Controls.Add(this.vUploadFormat);
        }

        /// <summary>
        /// Adds the upload file count validator.
        /// </summary>
        protected void AddUploadFileCountValidator()
        {
            // Create a message. It gets added to the page control specified.
            string argMaxFiles = null;
            string argAttachmentReference = this.attachmentReference;
            // Check if we need to make the reference plural or not
            if (this.maxFiles > 1) argAttachmentReference += "s";
            // All because they want numbers as words ...
            switch (this.maxFiles)
            {
                case 1:
                    argMaxFiles = "one";
                    break;
                case 6:
                    argMaxFiles = "six";
                    break;
                default:
                    argMaxFiles = this.maxFiles.ToString();
                    break;
            }
            string msg = string.Format(fileConfig["ErrorUploadFileAttachmentCount"], argMaxFiles, argAttachmentReference);

            // Create a validator to monitor the file count.
            // Note: the 'control to validate' here will be this the parent control!
            this.vUploadFileCount = new UploadFileCountValidator(this.ID, msg, this.maxFiles);
            if (!string.IsNullOrEmpty(this.ValidationGroup)) this.vUploadFileCount.ValidationGroup = this.ValidationGroup;
            this.Controls.Add(this.vUploadFileCount);
        }

        /// <summary>
        /// This is the place where all validation is performed before a file is saved. It will
        /// be different for file attachment saves and image attachment saves.
        /// </summary>
        /// <returns>Returns true if the file save request is a valid one, otherwise returns false if file size is too big, etc. </returns>
        protected virtual bool ValidFileSaveRequest()
        {
            bool IsValid = false;

            // Perform the generic file attachment validation.
            this.vUploadSize.Validate();
            this.vUploadFormat.Validate();
            this.vUploadFileCount.Validate();
            IsValid = this.vUploadFormat.IsValid && this.vUploadSize.IsValid && this.vUploadFileCount.IsValid;

            return IsValid;
        }
        #endregion

        #region File data storage
        // The reason file ids and names are stored like this is because the event id may not yet be known. The user may be creating an event for the first time.
        // We need to store the file attachments and keep a note of all file ids ready for the first submit point.

        /// <summary>
        /// Adds a file id and name to the file data arrays. It first checks if the id has been stored before.
        /// </summary>
        /// <param name="fileId">The file id to store</param>
        /// <param name="fileName">The file name to show</param>
        protected void AddFileDataToArrays(int fileId, string fileName)
        {
            // First check if this Id has already been stored before. Don't store it twice!
            // Cycle through each file id in the field data array
            bool IdExists = false;
            foreach (HtmlInputHidden idBox in this.fileIdArray)
            {
                if (!string.IsNullOrEmpty(idBox.Value))
                {
                    if (Convert.ToInt32(idBox.Value) == fileId) IdExists = true;
                    break;
                }
            }

            if (!IdExists)
            {
                // Find the next unused storage location
                int indexFreeSlot = this.FindNextFreeFileDataSlot();

                // If all slots are occupied then 'indexFreeSlot' will be -1.
                if (indexFreeSlot > -1)
                {
                    // Add the file id to the file data array at the free index location found
                    HtmlInputHidden idBox = this.fileIdArray[indexFreeSlot];
                    idBox.Value = fileId.ToString(CultureInfo.CurrentCulture);
                    // Add the file name to the file data array at the free index location found
                    HtmlInputHidden nameBox = this.fileNameArray[indexFreeSlot];
                    nameBox.Value = fileName;
                }
            }

        }

        /// <summary>
        /// Removes file data from file data storage by first locating the index of the file id.
        /// </summary>
        /// <param name="fileId">The file id to store</param>
        private void RemoveFileDataFromArrays(int fileId)
        {
            // Cycle through each file data index value.
            for (int index = 0; index < maxFiles; index++)
            {
                HtmlInputHidden idBox = this.fileIdArray[index];
                HtmlInputHidden nameBox = this.fileNameArray[index];
                // Do we have an id value to compare with?
                if (!string.IsNullOrEmpty(idBox.Value))
                {
                    if (Convert.ToInt32(idBox.Value) == fileId)
                    {
                        // We have a file id match, so remove the hidden file data at this index point
                        idBox.Value = string.Empty;
                        nameBox.Value = string.Empty;
                    }
                }
            }
        }

        /// <summary>
        /// Finds the index of first unused 'slot' in the file data storage array
        /// </summary>
        /// <returns>Returns -1 if no free slot exists; otherwise returns the index of the first free slot.</returns>
        private int FindNextFreeFileDataSlot()
        {
            // Find the next unused storage location
            int index = -1;
            int indexFreeSlot = -1;
            foreach (HtmlInputHidden idBox in this.fileIdArray)
            {
                index++;
                if (string.IsNullOrEmpty(idBox.Value))
                {
                    // We have found a free slot so mark it
                    indexFreeSlot = index;
                    break;
                }
            }

            return indexFreeSlot;
        }

        /// <summary>
        /// Simply checks to see if a free 'slot' exists in the file data storage array
        /// </summary>
        /// <returns></returns>
        public bool FreeFileDataSlotExists()
        {
            return this.FindNextFreeFileDataSlot() != -1;
        }
        #endregion

        #region Event handlers
        /// <summary>
        /// Removes the selected file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void removeFile_Click(object sender, EventArgs e)
        {
            // Locate the selected file index
            if (sender != null)
            {
                EsccButton removeFile = (EsccButton)sender;

                // Extract the integer index value from the button's string id
                int index = -1;
                try
                {
                    index = Convert.ToInt32(removeFile.ID.Replace("removeFile_", ""));
                }
                catch { };

                // Did we get a successful index value?
                if ((index >= 0) && (index < maxFiles))
                {
                    // Use the index to get the actual file id from the respective hidden control.
                    HtmlInputHidden idBox = this.fileIdArray[index];
                    int fileDataId = Convert.ToInt32(idBox.Value);
                    // Get the linked item id from the querystring if it exists
                    int LinkedItemId = 0;
                    try
                    {
                        string paramName = QueryStringParameterNameForLinkedItemID();
                        LinkedItemId = Convert.ToInt32(this.Context.Request.QueryString[paramName]);
                    }
                    catch { }
                    // Delete the file from the database
                    this.DeleteFileInDB(fileDataId, LinkedItemId);
                    // Remove all trace of this file from the file data arrays too.
                    this.RemoveFileDataFromArrays(fileDataId);
                }
                else
                {
                    throw new Exception("Could not identify the file to remove from the button pressed on the page.");
                }
            }
        }

        /// <summary>
        /// Saves the uploaded file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addFile_Click(object sender, EventArgs e)
        {
            if (this.FreeFileDataSlotExists())
            {
                bool FileSaveRequestIsValid = this.ValidFileSaveRequest();
                if (this.fileBrowserBox.PostedFile != null && this.fileBrowserBox.PostedFile.ContentLength > 0 && FileSaveRequestIsValid)
                {
                    // Get the user requesting the delete operation
                    AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
                    string modifiedBy = System.Threading.Thread.CurrentPrincipal.Identity.Name;

                    // Read the file details and data into a file object
                    string fileDescription = null;
                    if (this.fileDescriptionBox != null) fileDescription = this.fileDescriptionBox.Text;
                    DatabaseFileData fileData = new DatabaseFileData(this.fileBrowserBox.PostedFile, fileDescription);

                    // Store the file in the database.
                    int recordId = this.SaveFileInDB(fileData, modifiedBy);
                    if (recordId > 0)
                    {
                        // We now need to store this new id and the filename in the file data arrays.
                        this.AddFileDataToArrays(recordId, fileData.FileOriginalName);
                    }
                }
            }
        }

        #endregion

        #region Rendering methods
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.PreRender"/> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> object that contains the event data.</param>
        protected override void OnPreRender(EventArgs e)
        {
            // In postbacks, the controls are updated from the viewstate at this point.

            // Cycle through each file id in the field data array. If a file id exists then show the file details on screen.
            for (int index = 0; index < maxFiles; index++)
            {
                HtmlInputHidden idBox = this.fileIdArray[index];
                HtmlInputHidden nameBox = this.fileNameArray[index];
                HtmlGenericControl spanFilename = this.spanFilenameArray[index];
                HtmlGenericControl divFile = this.divFileArray[index];

                if (!string.IsNullOrEmpty(idBox.Value))
                {
                    int FileDataID = Convert.ToInt32(idBox.Value);

                    // Get just the file details without the attachment body
                    DatabaseFileData fileData = GetFileFromDB(FileDataID, false);

                    // Create a file link control
                    FileLinkControl fileLink = new FileLinkControl();
                    // Feed it a file handler url, but manually override the file extension detail
                    switch (this.attachmentType)
                    {
                        case MultiFileAttachmentType.Document:
                            fileLink.NavigateUrl = GetFileAttachmentUrl(FileDataID, this.dotnetProjectName);
                            break;
                        case MultiFileAttachmentType.Image:
                            fileLink.NavigateUrl = GetImageUrl(FileDataID, this.dotnetProjectName);
                            break;
                    }
                    fileLink.FileExtension = System.IO.Path.GetExtension(fileData.FileOriginalName);
                    fileLink.FileSize = fileData.FileSize;
                    // Note: the filename in hidden storage (nameBox.Value) and the filename extracted from the database will be the same (fileData.FileOriginalName).
                    fileLink.InnerText = nameBox.Value;
                    // Now add the file link control
                    spanFilename.Controls.Add(fileLink);
                }

                // Show or hide the file display
                divFile.Visible = !string.IsNullOrEmpty(idBox.Value);
            }

            base.OnPreRender(e);
        }

        /// <summary>
        /// Don't render a begin tag - just render the internal <c>Controls</c> collection
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderBeginTag(HtmlTextWriter writer)
        {
            // don't render it
        }

        /// <summary>
        /// Don't render an end tag - just render the internal <c>Controls</c> collection
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderEndTag(HtmlTextWriter writer)
        {
            // don't render it
        }
        #endregion

        #region Abstract methods

        /// <summary>
        /// Gets a file attachment from the database
        /// </summary>
        /// <param name="fileId">The record id for the stored file in the database.</param>
        /// <param name="IncludeBLOBData">If false, a special stored procedure is called that gets just the file details without the actual BLOB data.</param>
        /// <returns>The file data for the stored file</returns>
        /// <remarks>This has to be abstract to enable this control to be generic.</remarks>
        public abstract DatabaseFileData GetFileFromDB(int fileId, bool IncludeBLOBData);

        /// <summary>
        /// Saves the uploaded file to the database
        /// </summary>
        /// <param name="fileData"></param>
        /// <param name="modifiedBy"></param>
        /// <returns>The record id for the stored file</returns>
        /// <remarks>This has to be abstract to enable this control to be generic.</remarks>
        protected abstract int SaveFileInDB(DatabaseFileData fileData, string modifiedBy);

        /// <summary>
        /// Deletes the file attachment from the database
        /// </summary>
        /// <param name="fileId">The id of the file to delete</param>
        /// <param name="LinkedItemId">The id of a linked item if known</param>
        /// <remarks>This has to be abstract to enable this control to be generic.</remarks>
        protected abstract void DeleteFileInDB(int fileId, int LinkedItemId);

        /// <summary>
        /// Sets the control's file attachments
        /// </summary>
        /// <param name="fileCollection">The file attachment collection</param>
        /// <remarks>This has to be abstract to enable this control to be generic.</remarks>
        public abstract void SetFileAttachments(object fileCollection);

        /// <summary>
        /// Gets the control's file attachments
        /// </summary>
        /// <remarks>This has to be abstract to enable this control to be generic.</remarks>
        public abstract object GetFileAttachments();

        /// <summary>
        /// Each class that inherits from this class must specify the querystring parameter used to identify the file link id.
        /// When a file is added to a database it is usually liked to something (e.g. EventCalendar event or VSB article or whatever).
        /// Here is where we specify the name of the parameter to look out for.
        /// </summary>
        /// <returns></returns>
        /// <remarks>This has to be abstract to enable this control to be generic.</remarks>
        public abstract string QueryStringParameterNameForLinkedItemID();

        #endregion

        #region File attachment urls
        /// <summary>
        /// Gets a file attachment URL.
        /// </summary>
        /// <param name="fileId">The database record id of the file</param>
        /// <returns>Virtual URL of the file attachment, or null if no file</returns>
        public static Uri GetFileAttachmentUrl(int fileId, string projectName)
        {
            if ((fileId > 0) && (fileConfig["FileAttachmentHandlerUrl"] != null))
            {
                // Defaults
                string scheme = Uri.UriSchemeHttp;
                string host = "localhost";

                // Try to override defaults
                if (HttpContext.Current != null)
                {
                    scheme = HttpContext.Current.Request.Url.Scheme;
                    host = HttpContext.Current.Request.Url.Host;
                }

                string fileUrl = string.Format(fileConfig["FileAttachmentHandlerUrl"], projectName, fileId);

                // Build a URL
                if (fileUrl.StartsWith("http://") || fileUrl.StartsWith("https://"))
                {
                    return new Uri(fileUrl);
                }
                else
                {
                    return new Uri(scheme + "://" + host + (fileUrl.StartsWith("/") ? "" : "/") + fileUrl);
                }
            }
            else return null;
        }

        /// <summary>
        /// Gets the image URL.
        /// </summary>
        /// <param name="imageId">The database record id of the image</param>
        /// <returns>Virtual URL of image, or null if no image</returns>
        public static Uri GetImageUrl(int imageId, string projectName)
        {
            if ((imageId > 0) && (imageConfig["ImageHandlerUrl"] != null))
            {
                // Defaults
                string scheme = Uri.UriSchemeHttp;
                string host = "localhost";

                // Try to override defaults
                if (HttpContext.Current != null)
                {
                    scheme = HttpContext.Current.Request.Url.Scheme;
                    host = HttpContext.Current.Request.Url.Host;
                }

                string imageUrl = string.Format(imageConfig["ImageHandlerUrl"], projectName, imageId);

                // Build a URL
                if (imageUrl.StartsWith("http://") || imageUrl.StartsWith("https://"))
                {
                    return new Uri(imageUrl);
                }
                else
                {
                    return new Uri(scheme + "://" + host + (imageUrl.StartsWith("/") ? "" : "/") + imageUrl);
                }
            }
            else return null;
        }
        #endregion

    }
}
