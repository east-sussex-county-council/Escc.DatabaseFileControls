#region Using Directives
using System;
#endregion

namespace EsccWebTeam.DatabaseFileControls
{
	/// <summary>
	/// Specifies the various file attachment types we need to differentiate.
	/// </summary>
	public enum MultiFileAttachmentType
	{
		/// <summary>
		/// The files being attached are image files
		/// </summary>
		Image,
		
		/// <summary>
		/// The files being attached are documents
		/// </summary>
		Document
	}
}
