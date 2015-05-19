# Escc.DatabaseFileControls

A library for working with files stored in databases in ASP.NET WebForms applications.

Applications using this library need to develop a custom control which inherits from `MultiFileAttachmentBaseControl`, and then deliver the file through the browser using an HTTP handler which inherits from `BaseFileAttachmentHandler`.

The behaviour is driven through values set in `web.config` and these are set for the entire application scope. If you have two applications which use this library, they must not share the same application scope in IIS or you will encounter a race condition where the application that is loaded first has its settings applied to both applications.

Example `web.config`:

	<configuration>
	  <configSections>
	    <sectionGroup name="Escc.DatabaseFileControls">
	      <section name="FileAttachmentSettings" type="System.Configuration.NameValueSectionHandler, System, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"/>
	      <section name="ImageSettings" type="System.Configuration.NameValueSectionHandler, System, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"/>
	    </sectionGroup>
	  </configSections>

      <Escc.DatabaseFileControls>
	    <FileAttachmentSettings>
	      <add key="FileAttachmentHandlerUrl" value="/{0}/FileAttachmentHandler.ashx?fileid={1}"/>
	      <add key="MaximumFileSize" value="1048576" />
	      <add key="FileEditPrompt" value="Attach file (eg flyer)&lt;br /&gt;Maximum size: 1MB&lt;br /&gt;Maximum files: six" />
	      <add key="ErrorUploadFileAttachmentSize" value="Your file attachment is too big. It must be less than 1MB." />
	      <add key="ErrorUploadFileAttachmentCount" value="A maximum of &lt;strong&gt;{0}&lt;/strong&gt; {1} can be attached to an event. You have already attached {0} {1}." />
	      <add key="ErrorUploadFileAttachmentFormat" value="Your file attachment is the wrong type - it must be one of the following: Word, Excel or PowerPoint Office document; PDF or RTF document; JPEG, GIF or PNG image." />
	    </FileAttachmentSettings>
	    <ImageSettings>
	      <add key="ImageHandlerUrl" value="/{0}/ImageHandler.ashx?imageid={1}"/>
	      <add key="ImageExampleUrl" value="/example/path/your-image-here.gif" />
	      <add key="ImageMaximumWidth" value="150" />
	      <add key="ImageMaximumHeight" value="150" />
	      <add key="ImageMaximumFileSize" value="20480" />
	
	      <add key="ErrorUploadImageSize" value="Your picture is too big. It must be 150 pixels wide by 150 pixels high, or smaller." />
	      <add key="ErrorUploadSize" value="Your picture is too big. It must be less than 20K." />
	      <add key="ErrorUploadFormat" value="Your picture is the wrong type - it must be a JPEG, GIF or PNG" />
	      <add key="ErrorImageDescLength" value="Your picture description must be 250 characters or fewer" />
	      <add key="ErrorImageDescriptionRequired" value="Please add a short description of the image and select your picture again" />
	      <add key="ImageEditPrompt" value="Picture" />
	      <add key="ImageAdvicePixels" value="Your picture or photo can be up to 150 pixels wide by 150 pixels high." />
	      <add key="ImageAdviceBytes" value="Your picture file size must be under 20K." />
	      <add key="ImageDescEditPrompt" value="Description of picture" />
	      <add key="ImageExampleAlt" value="Your picture could go here (maximum size 150 x 150 pixels)" />
	    </ImageSettings>
	  </Escc.DatabaseFileControls>
	
	</configuration>

It is published as a NuGet package using [NuBuild](https://github.com/bspell1/NuBuild).