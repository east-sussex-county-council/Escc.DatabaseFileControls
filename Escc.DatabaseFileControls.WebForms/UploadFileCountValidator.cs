#region Using Directives

using Escc.FormControls.WebForms.Validators;

#endregion

namespace Escc.DatabaseFileControls.WebForms
{
    /// <summary>
    /// Validates the count of files being attached to a MultiFileAttachmentBaseControl control
    /// </summary>
	public class UploadFileCountValidator : EsccCustomValidator
	{
		#region Private fields
		private int maxFiles;
		#endregion

		#region Public properties

		/// <summary>
		/// Gets or sets the maximum file size in bytes. Use 0 for unlimited.
		/// </summary>
		/// <value>The maximum file size in bytes.</value>
		public int MaximumFiles
		{
			get { return maxFiles; }
			set { maxFiles = value; }
		}
		#endregion

		#region Constructors

		/// <summary>
        /// Initializes a new instance of the <see cref="T:UploadFileCountValidator"/> class.
		/// </summary>
		/// <param name="controlToValidateId">The id of the control to validate.</param>
		/// <param name="errorMessage">The error message.</param>
		public UploadFileCountValidator(string controlToValidateId, string errorMessage)
			: base(controlToValidateId, errorMessage)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:UploadFileCountValidator"/> class.
		/// </summary>
		/// <param name="controlToValidateId">The id of the control to validate.</param>
		/// <param name="errorMessage">The error message.</param>
		/// <param name="maximumFiles">The maximum number of files that can be uploaded. Use 0 as unlimited.</param>
        public UploadFileCountValidator(string controlToValidateId, string errorMessage, int maximumFiles)
			: base(controlToValidateId, errorMessage)
		{
			this.maxFiles = maximumFiles;
		}

		#endregion Constructors

		#region Validation

		/// <summary>
		/// Overrides the <see cref="M:System.Web.UI.BaseValidator.EvaluateIsValid"></see> method.
		/// </summary>
		/// <returns>
		/// true if the value in the input control is valid; otherwise, false.
		/// </returns>
		protected override bool EvaluateIsValid()
		{
            // The 'control to validate' will actually be the same as the 'parent' control. Normally we would use 'this.parent.FindControl()'
			// to find the relevant control. That won't work here. But we can just simply access it via 'this.Parent' anyway.
            MultiFileAttachmentBaseControl fileAttacher = (this.Parent) as MultiFileAttachmentBaseControl;

			// If the control doesn't exist, that's an error
            if (fileAttacher == null) return false;

			// If max files, check it
			if (this.maxFiles > 0)
			{
                if (!fileAttacher.FreeFileDataSlotExists())
                {
                    return false;
                }
			}

			return true;
		}

		#endregion Validation

	}

}
